using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// Low-overhead Quest release telemetry. Requests the release refresh rate,
    /// then emits one compact adb-logcat evidence line every reporting interval.
    /// </summary>
    public sealed class FrameTimeTelemetry : MonoBehaviour
    {
        private const float TargetRefreshHz = 72f;
        private const float TargetFrameMs = 1000f / TargetRefreshHz;

        [SerializeField, Min(2f)] private float reportEverySeconds = 10f;
        [SerializeField] private VistaDirector director;

        private readonly FrameTiming[] timings = new FrameTiming[1];
        private readonly List<XRDisplaySubsystem> displays = new();
        private XRDisplaySubsystem display;
        private float windowStartedAt;
        private float sessionStartedAt;
        private float cpuTotal;
        private float gpuTotal;
        private float frameTotal;
        private float worstFrame;
        private int samples;
        private int cpuSamples, gpuSamples;
        private ulong lastTimingTimestamp;
        private readonly float[] frameSamples = new float[4096];
        private int storedFrames;
        private string windowVista;
        private LifeMode windowLife;
        private MotionMode windowMotion;
        private bool windowTransition;
        private bool discardResumeFrame;
        private bool paused;
        private bool focused = true;
        private int stateWarmupFrames;
        private int overBudgetFrames;

        public void Configure(VistaDirector vistaDirector)
        {
            director = vistaDirector;
        }

        private void OnEnable()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.RoundToInt(TargetRefreshHz);
            sessionStartedAt = Time.unscaledTime;
            windowStartedAt = Time.unscaledTime;
            ResetWindow();
            Debug.Log($"QUIET_WATCH_BUILD version={Application.version} build_guid={Application.buildGUID} api={SystemInfo.graphicsDeviceType} timing_enabled={FrameTimingManager.IsFeatureEnabled()}");
            FrameTimingManager.CaptureFrameTimings();
        }

        private IEnumerator Start()
        {
            const float maxWaitSeconds = 5f;
            var waited = 0f;
            while (waited < maxWaitSeconds)
            {
                displays.Clear();
                SubsystemManager.GetSubsystems(displays);
                for (var i = 0; i < displays.Count; i++)
                {
                    if (displays[i] == null || !displays[i].running)
                    {
                        continue;
                    }

                    display = displays[i];
                    var requested = display.TryRequestDisplayRefreshRate(TargetRefreshHz);
                    // The runtime applies an accepted request on a later frame.
                    yield return null;
                    var actual = display.TryGetDisplayRefreshRate(out var actualHz)
                        ? actualHz.ToString("F1")
                        : "unavailable";
                    Debug.Log($"QUIET_WATCH_DISPLAY requested_hz={TargetRefreshHz:F0} accepted={requested} actual_hz={actual}");
                    yield break;
                }

                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning("QUIET_WATCH_DISPLAY requested_hz=72 accepted=false actual_hz=unavailable reason=no_running_display");
        }

        private void OnApplicationPause(bool value) { paused = value; discardResumeFrame = true; ResetWindow(); }
        private void OnApplicationFocus(bool value) { focused = value; discardResumeFrame = true; ResetWindow(); }

        private void LateUpdate()
        {
            if (paused || !focused) return;
            if (discardResumeFrame)
            {
                discardResumeFrame = false;
                FrameTimingManager.CaptureFrameTimings();
                return;
            }
            var vista = director?.ActiveVista?.VistaId ?? "none";
            if (vista != windowVista || (director != null &&
                (director.Life != windowLife || director.Motion != windowMotion || director.IsTransitioning != windowTransition)))
            {
                if (samples > 0) Report();
                ResetWindow();
            }
            var frameMs = Time.unscaledDeltaTime * 1000f;
            frameTotal += frameMs;
            worstFrame = Mathf.Max(worstFrame, frameMs);
            samples++;
            if (storedFrames < frameSamples.Length) frameSamples[storedFrames++] = frameMs;
            if (frameMs > TargetFrameMs * 1.05f)
            {
                overBudgetFrames++;
            }

            FrameTimingManager.CaptureFrameTimings();
            // Timings arrive asynchronously. Do not label the previous vista's
            // trailing GPU sample as this one, or count the same sample twice.
            if (stateWarmupFrames > 0) stateWarmupFrames--;
            else if (FrameTimingManager.IsFeatureEnabled() && FrameTimingManager.GetLatestTimings(1, timings) > 0
                && timings[0].frameStartTimestamp > lastTimingTimestamp)
            {
                lastTimingTimestamp = timings[0].frameStartTimestamp;
                var cpu = (float)timings[0].cpuFrameTime;
                var gpu = (float)timings[0].gpuFrameTime;
                if (Valid(cpu)) { cpuTotal += cpu; cpuSamples++; }
                if (Valid(gpu)) { gpuTotal += gpu; gpuSamples++; }
            }

            if (Time.unscaledTime - windowStartedAt >= reportEverySeconds)
            {
                Report();
                ResetWindow();
            }
        }

        private void Report()
        {
            var text = new StringBuilder(256);
            text.Append("QUIET_WATCH_PERF session_s=")
                .Append((Time.unscaledTime - sessionStartedAt).ToString("F0"))
                .Append(" vista=").Append(windowVista)
                .Append(" life=").Append(windowLife)
                .Append(" motion=").Append(windowMotion)
                .Append(" transition=").Append(windowTransition)
                .Append(" samples=").Append(samples)
                .Append(" frame_avg_ms=").Append((samples > 0 ? frameTotal / samples : 0f).ToString("F2"))
                .Append(" frame_worst_ms=").Append(worstFrame.ToString("F2"))
                .Append(" over_budget_pct=").Append((samples > 0 ? overBudgetFrames * 100f / samples : 0f).ToString("F2"));

            text.Append(" cpu_avg_ms=").Append(cpuSamples > 0 ? (cpuTotal / cpuSamples).ToString("F2") : "unavailable")
                .Append(" gpu_avg_ms=").Append(gpuSamples > 0 ? (gpuTotal / gpuSamples).ToString("F2") : "unavailable")
                .Append(" cpu_samples=").Append(cpuSamples).Append(" gpu_samples=").Append(gpuSamples);
            Array.Sort(frameSamples, 0, storedFrames);
            text.Append(" app_frame_p95_ms=").Append(Percentile(0.95f).ToString("F2"))
                .Append(" app_frame_p99_ms=").Append(Percentile(0.99f).ToString("F2"))
                .Append(" percentile_samples=").Append(storedFrames)
                .Append(" lateness_source=application_delta_not_compositor");

            if (display != null && display.running && display.TryGetDisplayRefreshRate(out var refreshHz))
            {
                text.Append(" refresh_hz=").Append(refreshHz.ToString("F1"));
            }
            else
            {
                text.Append(" refresh_hz=unavailable");
            }

            text.Append(" allocated_mb=")
                .Append((Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f)).ToString("F1"));

            ReadAndroidThermals(out var batteryTempC, out var thermalStatus);
            text.Append(" battery_c=")
                .Append(batteryTempC >= 0f ? batteryTempC.ToString("F1") : "unavailable")
                .Append(" thermal_status=")
                .Append(thermalStatus >= 0 ? thermalStatus.ToString() : "unavailable");

            Debug.Log(text.ToString());
        }

        private static void ReadAndroidThermals(out float batteryTempC, out int thermalStatus)
        {
            batteryTempC = -1f;
            thermalStatus = -1;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var filter = new AndroidJavaObject(
                    "android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED");
                using var battery = activity.Call<AndroidJavaObject>(
                    "registerReceiver", new object[] { null, filter });
                if (battery != null)
                {
                    var tenthsC = battery.Call<int>("getIntExtra", "temperature", -1);
                    if (tenthsC >= 0)
                    {
                        batteryTempC = tenthsC / 10f;
                    }
                }

                using var power = activity.Call<AndroidJavaObject>("getSystemService", "power");
                if (power != null)
                {
                    thermalStatus = power.Call<int>("getCurrentThermalStatus");
                }
            }
            catch (System.Exception)
            {
                // Some Android runtimes omit thermal APIs. The evidence line
                // reports unavailable without generating recurring log noise.
            }
#endif
        }

        private static bool Valid(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        private float Percentile(float percentile) => storedFrames > 0
            ? frameSamples[Mathf.Clamp(Mathf.CeilToInt(storedFrames * percentile) - 1, 0, storedFrames - 1)] : 0f;

        private void ResetWindow()
        {
            windowVista = director?.ActiveVista?.VistaId ?? "none";
            windowLife = director != null ? director.Life : LifeMode.Quiet;
            windowMotion = director != null ? director.Motion : MotionMode.Still;
            windowTransition = director != null && director.IsTransitioning;
            storedFrames = 0;
            stateWarmupFrames = 4;
            windowStartedAt = Time.unscaledTime;
            cpuTotal = 0f;
            gpuTotal = 0f;
            frameTotal = 0f;
            worstFrame = 0f;
            samples = 0;
            cpuSamples = gpuSamples = 0;
            overBudgetFrames = 0;
        }
    }
}

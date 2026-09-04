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
        private int timingSamples;
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

        private void LateUpdate()
        {
            var frameMs = Time.unscaledDeltaTime * 1000f;
            frameTotal += frameMs;
            worstFrame = Mathf.Max(worstFrame, frameMs);
            samples++;
            if (frameMs > TargetFrameMs * 1.05f)
            {
                overBudgetFrames++;
            }

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                cpuTotal += (float)timings[0].cpuFrameTime;
                gpuTotal += (float)timings[0].gpuFrameTime;
                timingSamples++;
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
                .Append(" vista=").Append(director?.ActiveVista?.VistaId ?? "none")
                .Append(" life=").Append(director != null ? director.Life.ToString() : "unknown")
                .Append(" motion=").Append(director != null ? director.Motion.ToString() : "unknown")
                .Append(" samples=").Append(samples)
                .Append(" frame_avg_ms=").Append((samples > 0 ? frameTotal / samples : 0f).ToString("F2"))
                .Append(" frame_worst_ms=").Append(worstFrame.ToString("F2"))
                .Append(" over_budget_pct=").Append((samples > 0 ? overBudgetFrames * 100f / samples : 0f).ToString("F2"));

            if (timingSamples > 0)
            {
                text.Append(" cpu_avg_ms=").Append((cpuTotal / timingSamples).ToString("F2"))
                    .Append(" gpu_avg_ms=").Append((gpuTotal / timingSamples).ToString("F2"));
            }
            else
            {
                text.Append(" cpu_avg_ms=unavailable gpu_avg_ms=unavailable");
            }

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

        private void ResetWindow()
        {
            windowStartedAt = Time.unscaledTime;
            cpuTotal = 0f;
            gpuTotal = 0f;
            frameTotal = 0f;
            worstFrame = 0f;
            samples = 0;
            timingSamples = 0;
            overBudgetFrames = 0;
        }
    }
}

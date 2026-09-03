using System.Text;
using UnityEngine;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// Allocation-free sampling window for Quest CPU/GPU frame evidence.
    /// Emits one compact adb-logcat line every reporting interval.
    /// </summary>
    public sealed class FrameTimeTelemetry : MonoBehaviour
    {
        [SerializeField, Min(2f)] private float reportEverySeconds = 10f;

        private readonly FrameTiming[] timings = new FrameTiming[1];
        private float windowStartedAt;
        private float cpuTotal;
        private float gpuTotal;
        private float frameTotal;
        private float worstFrame;
        private int samples;
        private int timingSamples;

        private void OnEnable()
        {
            windowStartedAt = Time.unscaledTime;
            FrameTimingManager.CaptureFrameTimings();
        }

        private void LateUpdate()
        {
            var frameMs = Time.unscaledDeltaTime * 1000f;
            frameTotal += frameMs;
            worstFrame = Mathf.Max(worstFrame, frameMs);
            samples++;

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
            var text = new StringBuilder(128);
            text.Append("QUIET_WATCH_PERF samples=").Append(samples)
                .Append(" frame_avg_ms=").Append((samples > 0 ? frameTotal / samples : 0f).ToString("F2"))
                .Append(" frame_worst_ms=").Append(worstFrame.ToString("F2"));

            if (timingSamples > 0)
            {
                text.Append(" cpu_avg_ms=").Append((cpuTotal / timingSamples).ToString("F2"))
                    .Append(" gpu_avg_ms=").Append((gpuTotal / timingSamples).ToString("F2"));
            }
            else
            {
                text.Append(" cpu_avg_ms=unavailable gpu_avg_ms=unavailable");
            }

            Debug.Log(text.ToString());
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
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace StarshipCabin
{
    /// <summary>
    /// Milestone 8: fixed foveated rendering on Quest, to buy back the frame
    /// budget that HDR + bloom spends. This is fixed foveation, not eye
    /// tracked, so it reads no gaze data and needs no extra permissions.
    ///
    /// The XR display subsystem usually is not running on the first frame, so
    /// this component polls briefly until it can set the level once.
    /// </summary>
    public class FoveationController : MonoBehaviour
    {
        [Range(0f, 1f)] public float foveationLevel = 0.66f;
        public float maxWaitSeconds = 5f;

        private IEnumerator Start()
        {
            var elapsed = 0f;
            var displays = new List<XRDisplaySubsystem>();

            while (elapsed < maxWaitSeconds)
            {
                SubsystemManager.GetSubsystems(displays);

                foreach (var display in displays)
                {
                    if (display != null && display.running)
                    {
                        ApplyFoveation(display);
                        yield break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning("Starship Cabin: no running XRDisplaySubsystem found; foveation not applied.");
        }

        private void ApplyFoveation(XRDisplaySubsystem display)
        {
            display.foveatedRenderingLevel = Mathf.Clamp01(foveationLevel);
            display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;
            Debug.Log($"Starship Cabin: fixed foveated rendering set to {foveationLevel:0.00}.");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace StarshipCabin
{
    [System.Serializable]
    public struct SeatAnchor
    {
        public string anchorName;
        /// <summary>World-space target eye position (y is the target eye height).</summary>
        public Vector3 eyePoint;
        /// <summary>World yaw (degrees) the user faces after the hop, regardless of physical orientation.</summary>
        public float yawDegrees;
    }

    /// <summary>
    /// Milestone 3: comfort-first movement between seat anchors.
    ///
    /// Real head-tracked walking is untouched (the camera stays device-driven).
    /// A short grip press hops to the next anchor: fade to black (0.25 s),
    /// move/rotate the XR origin so the user's *current* head pose lands exactly
    /// on the anchor's eye point facing the anchor's yaw, haptic tick, fade in.
    /// No translation or rotation is ever visible — the no-artificial-motion
    /// comfort rule holds. Holding grip (>= 1 s) re-centers on the current
    /// anchor (re-syncs after physical drift or posture change).
    ///
    /// Eye height is anchor-locked: sitting on a real chair, the couch anchor
    /// puts the eye at couch-seated height and the bed-lie anchor at lying
    /// height, so each anchor is a genuinely different perspective.
    /// </summary>
    public class SeatAnchorController : MonoBehaviour
    {
        public Transform cameraTransform;
        public Renderer fadeRenderer;
        public SeatAnchor[] anchors;
        public float fadeSeconds = 0.25f;
        public float holdRecenterSeconds = 1.0f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<XRInputDevice> devices = new();
        private MaterialPropertyBlock block;
        private int currentIndex;
        private bool transitioning;
        private bool initialAligned;
        private bool gripWasPressed;
        private float gripPressedAt;
        private bool holdHandled;

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            if (fadeRenderer != null)
            {
                SetFadeAlpha(0f);
                fadeRenderer.enabled = false;
            }
        }

        private void Start()
        {
            if (anchors != null && anchors.Length > 0)
            {
                ApplyAnchor(0);
            }
        }

        private void Update()
        {
            if (anchors == null || anchors.Length == 0 || cameraTransform == null)
            {
                return;
            }

            // The HMD reports (0,0,0) until tracking starts; re-align once the
            // first real head pose arrives so the initial view sits on anchor 0.
            if (!initialAligned && cameraTransform.localPosition.y > 0.25f)
            {
                initialAligned = true;
                ApplyAnchor(currentIndex);
            }

            var pressed = IsGripPressed();

            if (pressed && !gripWasPressed)
            {
                gripPressedAt = Time.time;
                holdHandled = false;
            }

            if (pressed && !holdHandled && Time.time - gripPressedAt >= holdRecenterSeconds)
            {
                holdHandled = true;
                StartHop(currentIndex); // recenter on the current anchor
            }

            if (!pressed && gripWasPressed && !holdHandled)
            {
                StartHop((currentIndex + 1) % anchors.Length);
            }

            gripWasPressed = pressed;
        }

        public void StartHop(int index)
        {
            if (transitioning || anchors == null || anchors.Length == 0)
            {
                return;
            }

            StartCoroutine(HopRoutine(Mathf.Clamp(index, 0, anchors.Length - 1)));
        }

        private IEnumerator HopRoutine(int index)
        {
            transitioning = true;

            yield return Fade(0f, 1f);
            ApplyAnchor(index);
            SendHapticTick();
            yield return Fade(1f, 0f);

            transitioning = false;
        }

        /// <summary>
        /// Places the XR origin so the user's current head pose lands on the
        /// anchor: current gaze yaw maps to the anchor yaw, and current head
        /// position maps to the anchor eye point (including height).
        /// </summary>
        private void ApplyAnchor(int index)
        {
            currentIndex = index;
            var anchor = anchors[index];
            var headLocal = cameraTransform.localPosition;
            var headYawLocal = cameraTransform.localEulerAngles.y;

            transform.rotation = Quaternion.Euler(0f, anchor.yawDegrees - headYawLocal, 0f);

            var rotatedHead = transform.rotation * headLocal;
            transform.position = new Vector3(
                anchor.eyePoint.x - rotatedHead.x,
                anchor.eyePoint.y - headLocal.y,
                anchor.eyePoint.z - rotatedHead.z);

            Debug.Log($"Starship Cabin seat anchor: {anchor.anchorName}");
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeRenderer == null)
            {
                yield break;
            }

            fadeRenderer.enabled = true;

            var elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeSeconds)));
                yield return null;
            }

            SetFadeAlpha(to);
            fadeRenderer.enabled = to > 0.001f;
        }

        private void SetFadeAlpha(float alpha)
        {
            fadeRenderer.GetPropertyBlock(block);
            block.SetColor(ColorId, new Color(0f, 0f, 0f, alpha));
            fadeRenderer.SetPropertyBlock(block);
        }

        // ------------------------------------------------------------------
        // Input: grip on either controller (XR devices + Input System fallback,
        // mirroring XRAmbienceInputController's dual-path approach).
        // ------------------------------------------------------------------

        private bool IsGripPressed()
        {
            foreach (var node in new[] { XRNode.RightHand, XRNode.LeftHand })
            {
                devices.Clear();
                InputDevices.GetDevicesAtXRNode(node, devices);

                foreach (var device in devices)
                {
                    if (device.isValid &&
                        device.TryGetFeatureValue(XRCommonUsages.gripButton, out var gripped) &&
                        gripped)
                    {
                        return true;
                    }
                }
            }

            foreach (var device in InputSystem.devices)
            {
                if (IsInputSystemButtonPressed(device, "gripPressed") ||
                    IsInputSystemButtonPressed(device, "gripButton"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInputSystemButtonPressed(UnityEngine.InputSystem.InputDevice device, string controlName)
        {
            return device.TryGetChildControl<ButtonControl>(controlName)?.isPressed == true;
        }

        private void SendHapticTick()
        {
            foreach (var node in new[] { XRNode.RightHand, XRNode.LeftHand })
            {
                devices.Clear();
                InputDevices.GetDevicesAtXRNode(node, devices);

                foreach (var device in devices)
                {
                    if (device.isValid &&
                        device.TryGetHapticCapabilities(out var capabilities) &&
                        capabilities.supportsImpulse)
                    {
                        device.SendHapticImpulse(0u, 0.25f, 0.08f);
                    }
                }
            }
        }
    }
}

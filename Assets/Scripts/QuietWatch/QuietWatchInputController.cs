using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace StarshipCabin.QuietWatch
{
    /// <summary>
    /// Controller mapping for the diegetic selector: primary advances vista,
    /// secondary toggles Quiet/Living, thumbstick click toggles Still/Drift.
    /// </summary>
    public sealed class QuietWatchInputController : MonoBehaviour
    {
        private static readonly XRNode[] ControllerNodes = { XRNode.RightHand, XRNode.LeftHand };

        [SerializeField] private VistaDirector director;

        private readonly List<XRInputDevice> devices = new();
        private bool primaryWasPressed;
        private bool secondaryWasPressed;
        private bool stickWasPressed;

        public void Configure(VistaDirector vistaDirector)
        {
            director = vistaDirector;
        }

        private void Update()
        {
            ReadButtons(out var primary, out var secondary, out var stick, out var source);

            if (primary && !primaryWasPressed)
            {
                director?.SelectNextVista();
                Pulse(source);
            }

            if (secondary && !secondaryWasPressed)
            {
                director?.ToggleLifeMode();
                Pulse(source);
            }

            if (stick && !stickWasPressed)
            {
                director?.ToggleMotionMode();
                Pulse(source);
            }

            primaryWasPressed = primary;
            secondaryWasPressed = secondary;
            stickWasPressed = stick;
        }

        private void ReadButtons(out bool primary, out bool secondary, out bool stick, out XRInputDevice source)
        {
            primary = false;
            secondary = false;
            stick = false;
            source = default;

            foreach (var node in ControllerNodes)
            {
                devices.Clear();
                InputDevices.GetDevicesAtXRNode(node, devices);

                foreach (var device in devices)
                {
                    if (!device.isValid)
                    {
                        continue;
                    }

                    var any = false;
                    if (device.TryGetFeatureValue(XRCommonUsages.primaryButton, out var p) && p)
                    {
                        primary = true;
                        any = true;
                    }
                    if (device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out var s) && s)
                    {
                        secondary = true;
                        any = true;
                    }
                    if (device.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out var c) && c)
                    {
                        stick = true;
                        any = true;
                    }

                    if (any)
                    {
                        source = device;
                    }
                }
            }
        }

        private static void Pulse(XRInputDevice device)
        {
            if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, 0.18f, 0.055f);
            }
        }
    }
}

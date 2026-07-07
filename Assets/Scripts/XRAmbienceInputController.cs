using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace StarshipCabin
{
    public class XRAmbienceInputController : MonoBehaviour
    {
        [SerializeField] private CabinExperienceController cabinController;
        [SerializeField] private XRNode[] controllerNodes = { XRNode.RightHand, XRNode.LeftHand };
        [SerializeField] private bool diagnosticLogging = false;
        [SerializeField] private float logIntervalSeconds = 2f;

        private readonly List<XRInputDevice> devices = new();
        private bool cycleWasPressed;
        private float nextDiagnosticLogAt;

        private void Awake()
        {
            if (cabinController == null)
            {
                cabinController = FindAnyObjectByType<CabinExperienceController>();
            }

            Debug.Log("Starship Cabin XR ambience input ready.");
        }

        private void Update()
        {
            var cyclePressed = IsAnyCycleControlPressed(out var sourceDevice);
            if (cyclePressed && !cycleWasPressed)
            {
                cabinController?.CycleMode();
                SendHapticPulse(sourceDevice);
                Debug.Log($"Starship Cabin cycle input from {sourceDevice}");
            }

            cycleWasPressed = cyclePressed;

            if (diagnosticLogging && Time.time >= nextDiagnosticLogAt)
            {
                nextDiagnosticLogAt = Time.time + logIntervalSeconds;
                LogInputDiagnostics();
            }
        }

        private bool IsAnyCycleControlPressed(out XRInputDevice sourceDevice)
        {
            foreach (var node in controllerNodes)
            {
                devices.Clear();
                InputDevices.GetDevicesAtXRNode(node, devices);

                foreach (var device in devices)
                {
                    if (IsCycleControlPressed(device))
                    {
                        sourceDevice = device;
                        return true;
                    }
                }
            }

            foreach (var device in InputSystem.devices)
            {
                if (IsInputSystemCycleControlPressed(device))
                {
                    sourceDevice = default;
                    return true;
                }
            }

            sourceDevice = default;
            return false;
        }

        private static bool IsCycleControlPressed(XRInputDevice device)
        {
            if (!device.isValid)
            {
                return false;
            }

            return IsPressed(device, XRCommonUsages.primaryButton)
                || IsPressed(device, XRCommonUsages.secondaryButton);
        }

        private static bool IsPressed(XRInputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.TryGetFeatureValue(usage, out var pressed) && pressed;
        }

        private static bool IsInputSystemCycleControlPressed(UnityEngine.InputSystem.InputDevice device)
        {
            return IsInputSystemButtonPressed(device, "primaryButton")
                || IsInputSystemButtonPressed(device, "secondaryButton");
        }

        private static bool IsInputSystemButtonPressed(UnityEngine.InputSystem.InputDevice device, string controlName)
        {
            return device.TryGetChildControl<ButtonControl>(controlName)?.isPressed == true;
        }

        private static void SendHapticPulse(XRInputDevice device)
        {
            if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, 0.22f, 0.08f);
            }
        }

        private void LogInputDiagnostics()
        {
            foreach (var node in controllerNodes)
            {
                devices.Clear();
                InputDevices.GetDevicesAtXRNode(node, devices);
                Debug.Log($"Starship Cabin XR node {node}: {devices.Count} device(s)");
            }

            foreach (var device in InputSystem.devices)
            {
                if (device.displayName.Contains("Controller") ||
                    device.layout.Contains("XR"))
                {
                    Debug.Log($"Starship Cabin InputSystem device: {device.displayName} layout={device.layout}");
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Video;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace StarshipCabin
{
    /// <summary>
    /// Milestone 5: in-room media wall. Plays local video files (.mp4 / .webm)
    /// dropped into the app's persistent data folder on the headset:
    ///
    ///   /sdcard/Android/data/jp.openclaw.starshipcabin/files/Videos/
    ///   (push with: adb push movie.mp4 "/sdcard/Android/data/jp.openclaw.starshipcabin/files/Videos/")
    ///
    /// Controls (trigger, either controller — grip is anchors, A/B/X/Y is ambience):
    ///   short press  = play / pause
    ///   hold >= 1 s  = next video (rescans the folder, so new files appear
    ///                  without restarting)
    ///
    /// The screen stays black until a video plays. Audio is spatialized at the
    /// screen so it belongs to the room.
    /// </summary>
    public class MediaScreenController : MonoBehaviour
    {
        public Renderer screenRenderer;
        public AudioSource audioSource;
        public string videoSubfolder = "Videos";
        public float holdNextSeconds = 1.0f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<XRInputDevice> devices = new();
        private readonly List<string> playlist = new();
        private VideoPlayer player;
        private Material screenMaterial;
        private int currentIndex = -1;
        private bool triggerWasPressed;
        private float triggerPressedAt;
        private bool holdHandled;

        private string VideoFolder => Path.Combine(Application.persistentDataPath, videoSubfolder);

        private void Awake()
        {
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.renderMode = VideoRenderMode.MaterialOverride;
            player.targetMaterialRenderer = screenRenderer;
            player.targetMaterialProperty = "_BaseMap";
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioSource);
            player.errorReceived += (_, message) => Debug.LogWarning($"Starship Cabin media: {message}");
            player.prepareCompleted += _ => SetScreenLit(true);

            if (screenRenderer != null)
            {
                // Runtime material instance: black until video light hits it.
                screenMaterial = screenRenderer.material;
                SetScreenLit(false);
            }

            Directory.CreateDirectory(VideoFolder);
            RefreshPlaylist();
        }

        private void Update()
        {
            var pressed = IsTriggerPressed();

            if (pressed && !triggerWasPressed)
            {
                triggerPressedAt = Time.time;
                holdHandled = false;
            }

            if (pressed && !holdHandled && Time.time - triggerPressedAt >= holdNextSeconds)
            {
                holdHandled = true;
                NextVideo();
            }

            if (!pressed && triggerWasPressed && !holdHandled)
            {
                TogglePlayPause();
            }

            triggerWasPressed = pressed;
        }

        public void TogglePlayPause()
        {
            if (player.isPlaying)
            {
                player.Pause();
                return;
            }

            if (currentIndex < 0)
            {
                NextVideo();
                return;
            }

            player.Play();
        }

        public void NextVideo()
        {
            RefreshPlaylist();

            if (playlist.Count == 0)
            {
                Debug.Log($"Starship Cabin media: no videos found in {VideoFolder}");
                SetScreenLit(false);
                return;
            }

            currentIndex = (currentIndex + 1) % playlist.Count;
            player.url = playlist[currentIndex];
            player.Play();
            Debug.Log($"Starship Cabin media: playing {Path.GetFileName(playlist[currentIndex])}");
        }

        private void RefreshPlaylist()
        {
            playlist.Clear();

            if (!Directory.Exists(VideoFolder))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(VideoFolder))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension == ".mp4" || extension == ".webm")
                {
                    playlist.Add(file);
                }
            }

            playlist.Sort();
        }

        private void SetScreenLit(bool lit)
        {
            if (screenMaterial != null)
            {
                screenMaterial.SetColor(BaseColorId, lit ? Color.white : Color.black);
            }
        }

        private bool IsTriggerPressed()
        {
            foreach (var node in new[] { XRNode.RightHand, XRNode.LeftHand })
            {
                devices.Clear();
                InputDevices.GetDevicesAtXRNode(node, devices);

                foreach (var device in devices)
                {
                    if (device.isValid &&
                        device.TryGetFeatureValue(XRCommonUsages.triggerButton, out var pressed) &&
                        pressed)
                    {
                        return true;
                    }
                }
            }

            foreach (var device in InputSystem.devices)
            {
                if (IsInputSystemButtonPressed(device, "triggerPressed") ||
                    IsInputSystemButtonPressed(device, "triggerButton"))
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
    }
}

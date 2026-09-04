using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StarshipCabin.QuietWatch;
using System.Linq;

namespace StarshipCabin.EditorTools
{
    /// <summary>Produces deterministic desktop reference captures from every seat.</summary>
    public static class QuietWatchCaptureTool
    {
        private const string ScenePath = "Assets/Scenes/Cabin_Quarters_V2.unity";
        private const string OutputFolder = "Builds/Captures";

        [MenuItem("Starship Cabin/Quiet Watch/Capture Fixed Seats")]
        public static void CaptureAll()
        {
            if (!File.Exists(ScenePath))
            {
                QuartersSceneSetup.SetupQuartersScene();
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            var camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Quiet Watch capture requires the generated Main Camera.");
            }

            var points = UnityEngine.Object.FindObjectsByType<VistaCapturePoint>();
            if (points.Length == 0)
            {
                throw new InvalidOperationException("Quiet Watch capture points are missing; regenerate the scene.");
            }

            Directory.CreateDirectory(OutputFolder);
            var previousParent = camera.transform.parent;
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousTarget = camera.targetTexture;
            var target = new RenderTexture(1536, 1024, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);

            var vistas = UnityEngine.Object.FindObjectsByType<VistaEnvironment>(FindObjectsInactive.Include)
                .OrderBy(vista => vista.VistaId)
                .ToArray();

            try
            {
                camera.transform.SetParent(null, true);
                camera.targetTexture = target;

                foreach (var vista in vistas)
                {
                    foreach (var candidate in vistas)
                    {
                        candidate.gameObject.SetActive(candidate == vista);
                    }
                    vista.Enter(LifeMode.Quiet, MotionMode.Still);

                    foreach (var point in points)
                    {
                        camera.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        pixels.Apply(false);

                        var path = Path.Combine(OutputFolder, $"{Slug(vista.VistaId)}-{Slug(point.CaptureName)}.png");
                        File.WriteAllBytes(path, pixels.EncodeToPNG());
                        Debug.Log("Quiet Watch capture: " + Path.GetFullPath(path));
                    }

                    vista.Exit();
                }

                // Three deterministic Living-mode review frames prove that
                // choreography and grace-note compositions remain inside the
                // observation window before the headset build is installed.
                var couch = points.FirstOrDefault(point => point.CaptureName == "Couch");
                if (couch != null)
                {
                    foreach (var authored in vistas.OfType<AuthoredVista>())
                    {
                        if (authored.VistaId != "harbour"
                            && authored.VistaId != "great-weather"
                            && authored.VistaId != "long-formation")
                        {
                            continue;
                        }

                        foreach (var candidate in vistas)
                        {
                            candidate.gameObject.SetActive(candidate == authored);
                        }
                        authored.Enter(LifeMode.Living, MotionMode.Still);
                        authored.PreviewAt(60f, LifeMode.Living, MotionMode.Still);
                        camera.transform.SetPositionAndRotation(couch.transform.position, couch.transform.rotation);
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        pixels.Apply(false);

                        var path = Path.Combine(OutputFolder, $"{Slug(authored.VistaId)}-living-event-couch.png");
                        File.WriteAllBytes(path, pixels.EncodeToPNG());
                        Debug.Log("Quiet Watch Living capture: " + Path.GetFullPath(path));
                        authored.Exit();
                    }
                }
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = previousTarget;
                camera.transform.SetParent(previousParent, true);
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
                UnityEngine.Object.DestroyImmediate(pixels);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static string Slug(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "unnamed"
                : value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("(", "").Replace(")", "");
        }
    }
}

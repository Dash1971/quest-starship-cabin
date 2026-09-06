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
        private static string OutputFolder;
        [Serializable] private sealed class CaptureManifest
        {
            public string sourceHash, unityVersion, createdUtc;
            public bool baked;
            public string[] files;
        }

        [Serializable] private sealed class ChessLightingEvidence
        {
            public string sourceHash;
            public int bakedProbeCount;
            public ChessRendererEvidence[] pieces;
        }
        [Serializable] private sealed class ChessRendererEvidence
        {
            public string name, shader, receiveGI, lightProbeUsage, reflectionProbeUsage, probeAnchor;
            public string[] keywords;
            public int lightmapIndex;
            public Color baseColor;
        }

        public static void RegenerateAndCaptureAll()
        {
            QuartersSceneSetup.SetupQuartersScene();
            CaptureAll();
        }

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

            var generation = QuietWatchBuildValidation.RequireCurrentScene(false);
            QuietWatchTrafficValidation.ValidateOpenScene();
            OutputFolder = Path.Combine("Builds/Captures", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));

            var camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Quiet Watch capture requires the generated Main Camera.");
            }

            var points = UnityEngine.Object.FindObjectsByType<VistaCapturePoint>(FindObjectsSortMode.None);
            if (points.Length == 0)
            {
                throw new InvalidOperationException("Quiet Watch capture points are missing; regenerate the scene.");
            }

            Directory.CreateDirectory(OutputFolder);
            var previousParent = camera.transform.parent;
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1536, 1024, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);

            var vistas = UnityEngine.Object.FindObjectsByType<VistaEnvironment>(FindObjectsInactive.Include, FindObjectsSortMode.None)
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
                    if (vista is AuthoredVista fixedAuthored) fixedAuthored.PreviewAt(0, LifeMode.Quiet, MotionMode.Still);
                    if (vista is FirstQuestionVista fixedStars) fixedStars.PreviewAt(0, LifeMode.Quiet, MotionMode.Still);

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

                // Deterministic Living frames support composition review;
                // they do not establish stereo comfort or headset performance.
                var couch = points.FirstOrDefault(point => point.CaptureName == "Couch");
                if (couch != null)
                {
                    foreach (var authored in vistas.OfType<AuthoredVista>())
                    {
                        if (authored.VistaId != "harbour"
                            && authored.VistaId != "blue-morning"
                            && authored.VistaId != "great-weather"
                            && authored.VistaId != "long-formation")
                        {
                            continue;
                        }

                        foreach (var candidate in vistas)
                        {
                            candidate.gameObject.SetActive(candidate == authored);
                        }
                        var eventPreview = authored.GraceNoteAtSeconds + authored.GraceDurationSeconds * 0.68f;
                        var previewTimes = authored.VistaId == "harbour"
                            ? new[] { 28f, 52f, eventPreview }
                            : authored.VistaId == "long-formation"
                                ? new[] { 0f, 10f, 45f, eventPreview }
                                : authored.VistaId == "great-weather"
                                    ? new[] { authored.GraceNoteAtSeconds, authored.GraceNoteAtSeconds + authored.GraceDurationSeconds * 0.5f, authored.GraceNoteAtSeconds + authored.GraceDurationSeconds }
                                    : new[] { eventPreview };
                        foreach (var previewAt in previewTimes)
                        {
                            var motion = authored.VistaId == "long-formation" ? MotionMode.Drift : MotionMode.Still;
                            authored.Enter(LifeMode.Living, motion);
                            authored.PreviewAt(previewAt, LifeMode.Living, motion);
                            camera.transform.SetPositionAndRotation(couch.transform.position, couch.transform.rotation);
                            camera.Render();
                            RenderTexture.active = target;
                            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                            pixels.Apply(false);

                            var beforeEvent = previewAt < authored.GraceNoteAtSeconds;
                            var suffix = authored.VistaId == "harbour" || authored.VistaId == "great-weather"
                                ? $"living-{previewAt:000}s-couch"
                                : authored.VistaId == "long-formation" && beforeEvent
                                    ? $"living-drift-cruise-{previewAt:000}s-couch"
                                    : "living-event-couch";
                            var path = Path.Combine(OutputFolder, $"{Slug(authored.VistaId)}-{suffix}.png");
                            File.WriteAllBytes(path, pixels.EncodeToPNG());
                            Debug.Log("Quiet Watch Living capture: " + Path.GetFullPath(path));
                        }
                        authored.Exit();
                    }
                    var firstQuestion = vistas.OfType<FirstQuestionVista>().Single();
                    firstQuestion.gameObject.SetActive(true);
                    firstQuestion.Enter(LifeMode.Living, MotionMode.Still);
                    foreach (var previewAt in new[] { 782f, 786f })
                    {
                        firstQuestion.PreviewAt(previewAt, LifeMode.Living, MotionMode.Still);
                        camera.transform.SetPositionAndRotation(couch.transform.position, couch.transform.rotation);
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                        pixels.Apply(false);
                        File.WriteAllBytes(Path.Combine(OutputFolder, $"first-question-living-{previewAt:000}s-couch.png"), pixels.EncodeToPNG());
                    }
                    firstQuestion.Exit();
                }
                var chessVista = vistas.OfType<AuthoredVista>().Single(v => v.VistaId == "long-formation");
                foreach (var candidate in vistas) candidate.gameObject.SetActive(candidate == chessVista);
                chessVista.Enter(LifeMode.Quiet, MotionMode.Still);
                chessVista.PreviewAt(0, LifeMode.Quiet, MotionMode.Still);
                var chess = GameObject.Find("Chess Board").transform.position;
                // Keep the original comparison view and add +/-12 cm head offsets.
                foreach (var offset in new[] { 0f, -0.12f, 0.12f })
                {
                    camera.transform.position = chess + new Vector3(-0.25f + offset, 0.55f, -0.5f);
                    camera.transform.rotation = Quaternion.LookRotation(chess + Vector3.up * 0.04f - camera.transform.position);
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply(false);
                    var name = offset == 0f ? "chess-matte-material-review" : offset < 0f ? "chess-head-left" : "chess-head-right";
                    File.WriteAllBytes(Path.Combine(OutputFolder, name + ".png"), pixels.EncodeToPNG());
                }
                var chessEvidence = new ChessLightingEvidence
                {
                    sourceHash = generation.SourceHash,
                    bakedProbeCount = LightmapSettings.lightProbes != null ? LightmapSettings.lightProbes.count : 0,
                    pieces = new[] { "Chess Pieces White", "Chess Pieces Black" }.Select(name =>
                    {
                        var renderer = GameObject.Find(name).GetComponent<MeshRenderer>();
                        return new ChessRendererEvidence
                        {
                            name = name, shader = renderer.sharedMaterial.shader.name,
                            keywords = renderer.sharedMaterial.shaderKeywords,
                            receiveGI = renderer.receiveGI.ToString(), lightProbeUsage = renderer.lightProbeUsage.ToString(),
                            reflectionProbeUsage = renderer.reflectionProbeUsage.ToString(),
                            probeAnchor = renderer.probeAnchor != null ? renderer.probeAnchor.name : "missing",
                            lightmapIndex = renderer.lightmapIndex, baseColor = renderer.sharedMaterial.GetColor("_BaseColor")
                        };
                    }).ToArray()
                };
                File.WriteAllText(Path.Combine(OutputFolder, "chess-lighting.json"), JsonUtility.ToJson(chessEvidence, true));
                chessVista.Exit();
                var manifest = new CaptureManifest
                {
                    sourceHash = generation.SourceHash, unityVersion = Application.unityVersion,
                    createdUtc = DateTime.UtcNow.ToString("O"),
                    baked = generation.BakedSourceHash == generation.SourceHash && LightmapSettings.lightmaps.Length > 0,
                    files = Directory.GetFiles(OutputFolder, "*.png").Select(Path.GetFileName).OrderBy(name => name).ToArray()
                };
                File.WriteAllText(Path.Combine(OutputFolder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                camera.transform.SetParent(previousParent, true);
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
                UnityEngine.Object.DestroyImmediate(pixels);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                // Capture hooks alter transforms, shared lights and property
                // blocks. Restore the saved scene, never save preview poses.
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        private static string Slug(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "unnamed"
                : value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("(", "").Replace(")", "");
        }
    }

    /// <summary>
    /// Deterministic route audit used before captures and Android builds. The
    /// runtime never needs physics: sampled spline clearance is proven here.
    /// </summary>
    internal static class QuietWatchTrafficValidation
    {
        private const string OutputFolder = "Builds/Validation";
        private const float GraceStartSeconds = 720f;
        private const float GraceDurationSeconds = 72f;

        public static void ValidateOpenScene()
        {
            // Resources.FindObjectsOfTypeAll is intentional: clearance markers
            // are hidden from the Hierarchy and the harbour vista is inactive
            // until selected, both of which must still participate in audit.
            var routes = Resources.FindObjectsOfTypeAll<HarbourTrafficRoute>()
                .Where(route => route.gameObject.scene.IsValid())
                .OrderBy(route => route.name)
                .ToArray();
            var volumes = Resources.FindObjectsOfTypeAll<HarbourClearanceVolume>()
                .Where(volume => volume.gameObject.scene.IsValid())
                .OrderBy(volume => volume.VolumeLabel)
                .ToArray();
            var errors = new System.Collections.Generic.List<string>();
            var report = new System.Collections.Generic.List<string>
            {
                "Quiet Watch harbour traffic clearance audit",
                $"Routes: {routes.Length}",
                $"Station clearance volumes: {volumes.Length}",
                "Route sampling: 401 positions per corridor",
                "Traffic separation sampling: 0.5 seconds across 1200 seconds",
                string.Empty
            };

            if (routes.Length != 3)
            {
                errors.Add($"Expected exactly three harbour routes, found {routes.Length}.");
            }
            if (volumes.Length < 30)
            {
                errors.Add($"Expected at least 30 station clearance volumes, found {volumes.Length}.");
            }

            foreach (var route in routes)
            {
                if (route.Points == null || route.Points.Length < 4)
                {
                    errors.Add($"{route.name}: route requires at least four authored points.");
                    continue;
                }

                var nearest = float.PositiveInfinity;
                var nearestLabel = string.Empty;
                for (var sample = 0; sample <= 400; sample++)
                {
                    var phase = sample / 400f;
                    route.Evaluate(phase, out var localPosition, out _, out _);
                    var worldPosition = route.transform.parent.TransformPoint(localPosition);
                    foreach (var volume in volumes)
                    {
                        var clearance = Vector3.Distance(worldPosition, volume.transform.position)
                            - route.ClearanceRadius * Mathf.Abs(route.transform.parent.lossyScale.x)
                            - volume.Radius * Mathf.Abs(volume.transform.lossyScale.x);
                        if (clearance < nearest)
                        {
                            nearest = clearance;
                            nearestLabel = volume.VolumeLabel;
                        }
                    }
                }

                report.Add($"{route.name}: closest station clearance {nearest:0.00} m ({nearestLabel})");
                if (nearest < 0f)
                {
                    errors.Add($"{route.name}: penetrates {nearestLabel} by {-nearest:0.00} m.");
                }
            }

            for (var first = 0; first < routes.Length; first++)
            {
                for (var second = first + 1; second < routes.Length; second++)
                {
                    var nearest = float.PositiveInfinity;
                    var nearestAt = 0f;
                    for (var step = 0; step <= 2400; step++)
                    {
                        var elapsed = step * 0.5f;
                        var phaseA = TrafficPhase(routes[first], elapsed);
                        var phaseB = TrafficPhase(routes[second], elapsed);
                        routes[first].Evaluate(phaseA, out var localA, out _, out _);
                        routes[second].Evaluate(phaseB, out var localB, out _, out _);
                        var worldA = routes[first].transform.parent.TransformPoint(localA);
                        var worldB = routes[second].transform.parent.TransformPoint(localB);
                        var clearance = Vector3.Distance(worldA, worldB)
                            - routes[first].ClearanceRadius * Mathf.Abs(routes[first].transform.parent.lossyScale.x)
                            - routes[second].ClearanceRadius * Mathf.Abs(routes[second].transform.parent.lossyScale.x);
                        if (clearance < nearest)
                        {
                            nearest = clearance;
                            nearestAt = elapsed;
                        }
                    }

                    report.Add($"{routes[first].name} / {routes[second].name}: closest traffic separation {nearest:0.00} m at {nearestAt:0.0} s");
                    if (nearest < 0f)
                    {
                        errors.Add($"{routes[first].name} and {routes[second].name}: overlap by {-nearest:0.00} m at {nearestAt:0.0} s.");
                    }
                }
            }

            report.Add(string.Empty);
            report.Add(errors.Count == 0 ? "PASS" : "FAIL");
            report.AddRange(errors);
            Directory.CreateDirectory(OutputFolder);
            var reportPath = Path.Combine(OutputFolder, "harbour-traffic-clearance.txt");
            File.WriteAllLines(reportPath, report);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Quiet Watch traffic validation failed:\n" + string.Join("\n", errors));
            }

            Debug.Log("Quiet Watch traffic validation PASS: " + Path.GetFullPath(reportPath));
        }

        private static float TrafficPhase(HarbourTrafficRoute route, float elapsed)
        {
            if (!route.IsGraceRoute)
            {
                return route.PhaseAt(elapsed, true);
            }

            return Smooth01((elapsed - GraceStartSeconds) / GraceDurationSeconds);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}

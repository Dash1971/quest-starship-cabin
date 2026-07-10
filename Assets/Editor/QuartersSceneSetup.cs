using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using UnityEngine.XR.OpenXR.Features.Interactions;
using StarshipCabin;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Concept V2 "Crew Quarters" scene generator — Milestone 1: shell + glazing.
    ///
    /// Room frame: X = width (±3.2), Y = up (0..2.5), outboard (glazed hull
    /// slope) toward -Z, inner/entry wall at +Z. The hull face rises from a
    /// 0.75 m sill at ~55° carrying four windows: three rounded-trapezoid
    /// lounge panes and one wide sleep-alcove pane. Stars render on a single
    /// shader quad 6 m behind the slope so head-tracked parallax reads as
    /// distance. No colliders, everything static, flat-shaded meshes saved as
    /// assets with generated lightmap UVs (baking happens in Milestone 4).
    /// </summary>
    public static class QuartersSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Cabin_Quarters_V2.unity";
        private const string MeshFolder = "Assets/Meshes/Quarters";

        // Room dimensions (metres).
        private const float HalfWidth = 3.2f;
        private const float InnerZ = 2.6f;
        private const float OuterZ = -2.6f;
        private const float Height = 2.5f;
        private const float SillHeight = 0.75f;
        private const float SlopeTopZ = -1.4f;

        // Slope plane frame.
        private static readonly Vector3 SlopeOrigin = new(0f, SillHeight, OuterZ);
        private static readonly Vector3 SlopeTop = new(0f, Height, SlopeTopZ);
        private static Vector3 SlopeUp => (SlopeTop - SlopeOrigin).normalized;
        private static float SlopeLength => Vector3.Distance(SlopeOrigin, SlopeTop);
        private static Vector3 SlopeNormal => Vector3.Cross(Vector3.right, SlopeUp).normalized; // points into the room

        // Window rectangles in slope space (u along +X, v up the slope).
        private const float WindowV0 = 0.28f;
        private const float WindowV1 = 1.95f;
        private static readonly (float u0, float u1)[] WindowSpans =
        {
            (-2.95f, -1.85f), // lounge 1
            (-1.63f, -0.53f), // lounge 2
            (-0.31f, 0.79f),  // lounge 3
            (1.35f, 2.85f)    // sleep alcove
        };

        private const float FrameFace = 0.07f;
        private const float FrameTopInset = 0.10f;
        private const float FrameCornerRadius = 0.14f;
        private const int CornerSegments = 5;

        [MenuItem("Starship Cabin/Setup Quarters Scene (V2)")]
        public static void SetupQuartersScene()
        {
            EnsureFolders();
            ConfigureAndroidPlayer();
            ConfigureOpenXrForQuest();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.385f, 0.355f);
            RenderSettings.fog = false;

            var materials = CreateQuartersMaterials();

            var root = new GameObject("Quarters Shell").transform;
            BuildQuartersShell(root, materials);
            var starSurface = BuildGlazing(root, materials);
            BuildInterimLights();
            AddXrRig();
            AddControllers(starSurface);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("Starship Cabin: Quarters V2 scene generated at " + ScenePath);
        }

        [MenuItem("Starship Cabin/Build Quarters APK")]
        public static void BuildQuartersApk()
        {
            SetupQuartersScene();

            Directory.CreateDirectory("Builds");
            var buildPath = "Builds/StarshipCabin-Quarters.apk";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = buildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Console.WriteLine($"Build result: {summary.result}");
            Console.WriteLine($"Build output: {Path.GetFullPath(buildPath)}");
            Console.WriteLine($"Build size: {summary.totalSize}");
            Console.WriteLine($"Build time: {summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {summary.result}");
            }
        }

        // ------------------------------------------------------------------
        // Shell
        // ------------------------------------------------------------------

        private struct QuartersMaterials
        {
            public Material Cream;
            public Material PanelWhite;
            public Material Deck;
            public Material Carpet;
            public Material Trim;
            public Material Frame;
            public Material Cove;
            public Material Glass;
            public Material Stars;
        }

        private static QuartersMaterials CreateQuartersMaterials()
        {
            return new QuartersMaterials
            {
                Cream = CreateMaterial("Quarters Cream", new Color(0.851f, 0.824f, 0.769f)),
                PanelWhite = CreateMaterial("Soft Panel White", new Color(0.902f, 0.878f, 0.831f)),
                Deck = CreateMaterial("Deck Warm Grey", new Color(0.462f, 0.443f, 0.405f)),
                Carpet = CreateMaterial("Muted Plum Carpet", new Color(0.427f, 0.290f, 0.322f)),
                Trim = CreateMaterial("Warm Grey Trim", new Color(0.608f, 0.588f, 0.549f)),
                Frame = CreateMaterial("Graphite Frame", new Color(0.106f, 0.114f, 0.125f)),
                Cove = CreateEmissiveMaterial("Warm White Cove", new Color(1.0f, 0.910f, 0.769f), new Color(1.0f, 0.882f, 0.702f), 2.1f),
                Glass = CreateGlassMaterial("Quarters Observation Glass", new Color(0.03f, 0.06f, 0.12f, 0.16f), new Color(0.02f, 0.05f, 0.10f), 0.4f),
                Stars = CreateStarMaterial()
            };
        }

        private static void BuildQuartersShell(Transform root, QuartersMaterials mats)
        {
            // Floor deck.
            var floor = new MeshDraft();
            floor.AddQuadOriented(
                new Vector3(-HalfWidth, 0f, OuterZ),
                new Vector3(HalfWidth, 0f, OuterZ),
                new Vector3(HalfWidth, 0f, InnerZ),
                new Vector3(-HalfWidth, 0f, InnerZ),
                Vector3.up);
            MeshObject(root, "Floor Deck", floor.ToMesh("Quarters Floor"), mats.Deck);

            // Lounge carpet: rounded rectangle, slightly raised.
            var carpetContour = QuartersMeshes.RoundedQuadContour(
                new Vector2(-3.0f, -1.55f), new Vector2(0.7f, -1.55f),
                new Vector2(0.7f, 1.95f), new Vector2(-3.0f, 1.95f),
                0.25f, CornerSegments);
            var carpetTop = QuartersMeshes.MapToPlane(carpetContour, Vector3.zero, Vector3.right, Vector3.forward, Vector3.up, 0.012f);
            var carpetBase = QuartersMeshes.MapToPlane(carpetContour, Vector3.zero, Vector3.right, Vector3.forward, Vector3.up, 0f);
            var carpet = new MeshDraft();
            carpet.AddConvexPolygon(carpetTop, Vector3.up);
            carpet.AddSkirt(carpetTop, carpetBase, faceOutward: true);
            MeshObject(root, "Lounge Carpet", carpet.ToMesh("Quarters Carpet"), mats.Carpet);

            // Ceiling.
            var ceiling = new MeshDraft();
            ceiling.AddQuadOriented(
                new Vector3(-HalfWidth, Height, SlopeTopZ),
                new Vector3(HalfWidth, Height, SlopeTopZ),
                new Vector3(HalfWidth, Height, InnerZ),
                new Vector3(-HalfWidth, Height, InnerZ),
                Vector3.down);
            MeshObject(root, "Ceiling", ceiling.ToMesh("Quarters Ceiling"), mats.PanelWhite);

            // Inner (entry) wall.
            var innerWall = new MeshDraft();
            innerWall.AddQuadOriented(
                new Vector3(-HalfWidth, 0f, InnerZ),
                new Vector3(HalfWidth, 0f, InnerZ),
                new Vector3(HalfWidth, Height, InnerZ),
                new Vector3(-HalfWidth, Height, InnerZ),
                Vector3.back);
            MeshObject(root, "Inner Wall", innerWall.ToMesh("Quarters Inner Wall"), mats.Cream);

            // Outboard sill wall (below the glazing).
            var sillWall = new MeshDraft();
            sillWall.AddQuadOriented(
                new Vector3(-HalfWidth, 0f, OuterZ),
                new Vector3(HalfWidth, 0f, OuterZ),
                new Vector3(HalfWidth, SillHeight, OuterZ),
                new Vector3(-HalfWidth, SillHeight, OuterZ),
                Vector3.forward);
            MeshObject(root, "Sill Wall", sillWall.ToMesh("Quarters Sill Wall"), mats.Cream);

            // Side walls: pentagon matching the hull profile.
            BuildSideWall(root, mats.Cream, -HalfWidth, Vector3.right);
            BuildSideWall(root, mats.Cream, HalfWidth, Vector3.left);

            // Glazed hull slope panels (the solid parts around the window rectangles).
            var slope = new MeshDraft();
            SlopePanel(slope, -HalfWidth, HalfWidth, 0f, WindowV0);              // bottom band
            SlopePanel(slope, -HalfWidth, HalfWidth, WindowV1, SlopeLength);      // top band
            SlopePanel(slope, -HalfWidth, WindowSpans[0].u0, WindowV0, WindowV1); // left margin
            SlopePanel(slope, WindowSpans[0].u1, WindowSpans[1].u0, WindowV0, WindowV1);
            SlopePanel(slope, WindowSpans[1].u1, WindowSpans[2].u0, WindowV0, WindowV1);
            SlopePanel(slope, WindowSpans[2].u1, WindowSpans[3].u0, WindowV0, WindowV1); // lounge/alcove divider
            SlopePanel(slope, WindowSpans[3].u1, HalfWidth, WindowV0, WindowV1);  // right margin
            MeshObject(root, "Hull Slope", slope.ToMesh("Quarters Hull Slope"), mats.Cream);

            // Window sill ledge along the slope base.
            var ledge = QuartersMeshes.ChamferedBox("Quarters Sill Ledge", 2f * HalfWidth, 0.06f, 0.28f, 0.015f);
            MeshObject(root, "Sill Ledge", ledge, mats.Trim, new Vector3(0f, SillHeight - 0.03f, OuterZ + 0.16f), Quaternion.identity);

            // Sill cove strip (emissive, grazes light up the slope between panes).
            var sillCove = QuartersMeshes.ChamferedBox("Quarters Sill Cove", 5.9f, 0.03f, 0.05f, 0.008f);
            MeshObject(root, "Sill Cove", sillCove, mats.Cove, new Vector3(0f, SillHeight + 0.015f, OuterZ + 0.05f), Quaternion.identity);

            // Ceiling perimeter coves.
            var sideCove = QuartersMeshes.ChamferedBox("Quarters Side Cove", 0.05f, 0.03f, 3.9f, 0.008f);
            MeshObject(root, "Ceiling Cove Left", sideCove, mats.Cove, new Vector3(-HalfWidth + 0.09f, Height - 0.06f, 0.6f), Quaternion.identity);
            MeshObject(root, "Ceiling Cove Right", sideCove, mats.Cove, new Vector3(HalfWidth - 0.09f, Height - 0.06f, 0.6f), Quaternion.identity);

            var spanCove = QuartersMeshes.ChamferedBox("Quarters Span Cove", 6.2f, 0.03f, 0.05f, 0.008f);
            MeshObject(root, "Ceiling Cove Inner", spanCove, mats.Cove, new Vector3(0f, Height - 0.06f, InnerZ - 0.09f), Quaternion.identity);
            MeshObject(root, "Ceiling Cove Slope Return", spanCove, mats.Cove, new Vector3(0f, Height - 0.06f, SlopeTopZ + 0.09f), Quaternion.identity);

            // Rounded structural beams over the mullion lines: up the slope, then across the ceiling.
            var beamXs = new[] { -1.74f, -0.42f, 1.07f };
            var slopeBeam = QuartersMeshes.ChamferedBox("Quarters Slope Beam", 0.16f, 0.10f, SlopeLength + 0.14f, 0.03f);
            var ceilingBeam = QuartersMeshes.ChamferedBox("Quarters Ceiling Beam", 0.16f, 0.10f, InnerZ - SlopeTopZ, 0.03f);
            var slopeBeamRotation = Quaternion.LookRotation(SlopeUp, SlopeNormal);

            foreach (var x in beamXs)
            {
                var slopeCenter = SlopePoint(x, SlopeLength * 0.5f, 0.04f); // half-depth 0.05: slightly embedded, no float gap
                MeshObject(root, $"Slope Beam ({x:0.00})", slopeBeam, mats.Trim, slopeCenter, slopeBeamRotation);

                var ceilingCenter = new Vector3(x, Height - 0.05f, (SlopeTopZ + InnerZ) * 0.5f);
                MeshObject(root, $"Ceiling Beam ({x:0.00})", ceilingBeam, mats.Trim, ceilingCenter, Quaternion.identity);
            }
        }

        private static void BuildSideWall(Transform root, Material material, float x, Vector3 inwardNormal)
        {
            var points = new List<Vector3>
            {
                new(x, 0f, InnerZ),
                new(x, 0f, OuterZ),
                new(x, SillHeight, OuterZ),
                new(x, Height, SlopeTopZ),
                new(x, Height, InnerZ)
            };

            var draft = new MeshDraft();
            draft.AddConvexPolygon(points, inwardNormal);
            MeshObject(root, x < 0f ? "Left Wall" : "Right Wall", draft.ToMesh(x < 0f ? "Quarters Left Wall" : "Quarters Right Wall"), material);
        }

        private static void SlopePanel(MeshDraft draft, float u0, float u1, float v0, float v1)
        {
            draft.AddQuadOriented(
                SlopePoint(u0, v0, 0f),
                SlopePoint(u1, v0, 0f),
                SlopePoint(u1, v1, 0f),
                SlopePoint(u0, v1, 0f),
                SlopeNormal);
        }

        private static Vector3 SlopePoint(float u, float v, float offset)
        {
            return SlopeOrigin + Vector3.right * u + SlopeUp * v + SlopeNormal * offset;
        }

        // ------------------------------------------------------------------
        // Glazing
        // ------------------------------------------------------------------

        private static StarWindowSurface BuildGlazing(Transform root, QuartersMaterials mats)
        {
            var glazingRoot = new GameObject("Glazing").transform;
            glazingRoot.SetParent(root);

            for (var i = 0; i < WindowSpans.Length; i++)
            {
                var (u0, u1) = WindowSpans[i];
                BuildWindow(glazingRoot, mats, i, u0, u1);
            }

            // One big star surface 6 m behind the slope plane: head movement gives
            // near-zero parallax, so the stars read as distant. Sized to cover the
            // view frustum through every pane from anywhere in the room.
            var starMesh = QuartersMeshes.UvQuad(
                "Quarters Star Surface",
                SlopePoint(-8f, -4f, -6f),
                SlopePoint(8f, -4f, -6f),
                SlopePoint(8f, 6.5f, -6f),
                SlopePoint(-8f, 6.5f, -6f));
            var starObject = MeshObject(glazingRoot, "Star Window Surface", starMesh, mats.Stars);
            GameObjectUtility.SetStaticEditorFlags(starObject, 0); // animated shader: keep out of batching/GI
            return starObject.AddComponent<StarWindowSurface>();
        }

        private static void BuildWindow(Transform parent, QuartersMaterials mats, int index, float u0, float u1)
        {
            var outer2D = QuartersMeshes.RoundedQuadContour(
                new Vector2(u0, WindowV0), new Vector2(u1, WindowV0),
                new Vector2(u1, WindowV1), new Vector2(u0, WindowV1),
                0.006f, CornerSegments); // near-square: avoids corner slivers against the rectangular slope aperture

            var inner2D = QuartersMeshes.RoundedQuadContour(
                new Vector2(u0 + FrameFace, WindowV0 + FrameFace),
                new Vector2(u1 - FrameFace, WindowV0 + FrameFace),
                new Vector2(u1 - FrameFace - FrameTopInset, WindowV1 - FrameFace),
                new Vector2(u0 + FrameFace + FrameTopInset, WindowV1 - FrameFace),
                FrameCornerRadius, CornerSegments);

            List<Vector3> Map(IList<Vector2> contour, float offset) =>
                QuartersMeshes.MapToPlane(contour, SlopeOrigin, Vector3.right, SlopeUp, SlopeNormal, offset);

            var frame = new MeshDraft();
            frame.AddRing(Map(inner2D, 0.045f), Map(outer2D, 0.045f), SlopeNormal); // front face
            frame.AddSkirt(Map(outer2D, 0.045f), Map(outer2D, 0f), faceOutward: true); // outer skirt down to slope
            frame.AddSkirt(Map(inner2D, 0.045f), Map(inner2D, -0.05f), faceOutward: false); // reveal into the opening
            MeshObject(parent, $"Window Frame {index + 1}", frame.ToMesh($"Quarters Window Frame {index + 1}"), mats.Frame);

            var glass = new MeshDraft();
            glass.AddConvexPolygon(Map(inner2D, -0.03f), SlopeNormal);
            var glassObject = MeshObject(parent, $"Window Glass {index + 1}", glass.ToMesh($"Quarters Window Glass {index + 1}"), mats.Glass);
            GameObjectUtility.SetStaticEditorFlags(glassObject, StaticEditorFlags.BatchingStatic); // transparent: skip GI contribution
        }

        // ------------------------------------------------------------------
        // Lights, rig, controllers
        // ------------------------------------------------------------------

        private static void BuildInterimLights()
        {
            // Interim realtime lights approximating the cove scheme; replaced by
            // baked area lights in Milestone 4.
            var lightsRoot = new GameObject("Interim Lights").transform;

            PointLight(lightsRoot, "Warm Room Key", new Vector3(0f, 2.2f, 0.5f), new Color(1f, 0.88f, 0.72f), 1.7f, 9.5f);
            PointLight(lightsRoot, "Sill Cove Glow", new Vector3(0f, 1.1f, -2.2f), new Color(1f, 0.87f, 0.68f), 0.9f, 4.5f);
            PointLight(lightsRoot, "Starlight Fill", new Vector3(0f, 1.9f, -2.0f), new Color(0.5f, 0.64f, 0.95f), 0.5f, 4.5f);
            PointLight(lightsRoot, "Alcove Reading Glow", new Vector3(2.1f, 2.1f, -0.5f), new Color(1f, 0.72f, 0.42f), 0.55f, 4.0f);
        }

        private static void PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static void AddXrRig()
        {
            // Origin sits on the floor at the couch anchor, facing the glazing.
            // The tracked camera supplies real head height above it, and the
            // Milestone 3 SeatAnchorController will move this origin between anchors.
            var origin = new GameObject("XR Origin (Quarters)");
            origin.transform.position = new Vector3(-1.0f, 0f, -0.4f);
            origin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(origin.transform, worldPositionStays: false);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.012f, 0.018f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 120f;

            var trackedPose = cameraObject.AddComponent<TrackedPoseDriver>();
            trackedPose.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            trackedPose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            cameraObject.AddComponent<AudioListener>();
        }

        private static void AddControllers(StarWindowSurface starSurface)
        {
            var controller = new GameObject("CabinExperience");
            var experience = controller.AddComponent<CabinExperienceController>();
            controller.AddComponent<XRAmbienceInputController>();

            var serialized = new SerializedObject(experience);
            var starProperty = serialized.FindProperty("starWindow");
            if (starProperty != null)
            {
                starProperty.objectReferenceValue = starSurface;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var audio = new GameObject("CabinAudio");
            audio.AddComponent<AmbientAudioController>();
        }

        // ------------------------------------------------------------------
        // Materials
        // ------------------------------------------------------------------

        private static Material CreateMaterial(string name, Color color)
        {
            var path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var mat = new Material(Shader.Find("Standard"))
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateEmissiveMaterial(string name, Color color, Color emission, float intensity)
        {
            var mat = CreateMaterial(name, color);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            mat.SetColor("_EmissionColor", emission * intensity);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateGlassMaterial(string name, Color color, Color emission, float intensity)
        {
            var mat = CreateEmissiveMaterial(name, color, emission, intensity);
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateStarMaterial()
        {
            const string path = "Assets/Materials/Star Window Surface.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("StarshipCabin/StarWindow");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "StarshipCabin/StarWindow shader not found. Ensure Assets/Shaders/StarWindow.shader is imported.");
            }

            var mat = new Material(shader) { name = "Star Window Surface" };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ------------------------------------------------------------------
        // Object + asset helpers
        // ------------------------------------------------------------------

        private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            return MeshObject(parent, name, mesh, material, Vector3.zero, Quaternion.identity);
        }

        private static GameObject MeshObject(
            Transform parent, string name, Mesh mesh, Material material, Vector3 position, Quaternion rotation)
        {
            var saved = SaveMesh(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = rotation;

            go.AddComponent<MeshFilter>().sharedMesh = saved;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            GameObjectUtility.SetStaticEditorFlags(
                go,
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ReflectionProbeStatic |
                StaticEditorFlags.OccludeeStatic);

            return go;
        }

        private static Mesh SaveMesh(Mesh mesh)
        {
            var path = $"{MeshFolder}/{Sanitize(mesh.name)}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == mesh)
            {
                // Same mesh instance reused by multiple scene objects (e.g. the
                // cove strips and beams) — already saved on a previous call.
                return mesh;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            Unwrapping.GenerateSecondaryUVSet(mesh);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '-');
            }
            return name;
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Materials");
            Directory.CreateDirectory("Assets/Shaders");
            Directory.CreateDirectory(MeshFolder);
            Directory.CreateDirectory("Builds");
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // Player + OpenXR configuration (same as the V1 generator, kept local
        // so this file is a standalone drop-in).
        // ------------------------------------------------------------------

        private static void ConfigureAndroidPlayer()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.companyName = "OpenClaw";
            PlayerSettings.productName = "Starship Cabin";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "jp.openclaw.starshipcabin");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        }

        private static void ConfigureOpenXrForQuest()
        {
            var buildTargetSettings = GetOrCreateXrBuildTargetSettings();

            if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            }

            var managerSettings = buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            if (managerSettings == null)
            {
                throw new InvalidOperationException("Unable to create Android XR manager settings.");
            }

            managerSettings.automaticLoading = true;
            managerSettings.automaticRunning = true;

            var assigned = XRPackageMetadataStore.AssignLoader(
                managerSettings,
                "UnityEngine.XR.OpenXR.OpenXRLoader",
                BuildTargetGroup.Android);

            if (!assigned)
            {
                throw new InvalidOperationException("Unable to assign OpenXR loader for Android.");
            }

            var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXrSettings == null)
            {
                throw new InvalidOperationException("Unable to load Android OpenXR settings.");
            }

            EnableFeature<MetaQuestFeature>(openXrSettings);
            EnableFeature<MetaQuestTouchPlusControllerProfile>(openXrSettings);
            EnableFeature<MetaQuestTouchProControllerProfile>(openXrSettings);
            EnableFeature<OculusTouchControllerProfile>(openXrSettings);

            EditorUtility.SetDirty(openXrSettings);
            EditorUtility.SetDirty(managerSettings);
            AssetDatabase.SaveAssets();
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateXrBuildTargetSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(
                    XRGeneralSettings.k_SettingsKey,
                    out var existing) && existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory("Assets/XR");

            var guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var found = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(assetPath);
                if (found != null)
                {
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, found, true);
                    return found;
                }
            }

            var created = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(created, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, created, true);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void EnableFeature<T>(OpenXRSettings settings) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature != null)
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }
    }
}

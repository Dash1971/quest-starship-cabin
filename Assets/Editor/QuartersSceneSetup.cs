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
using UnityEngine.Rendering.Universal;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using UnityEngine.XR.OpenXR.Features.Interactions;
using StarshipCabin;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Concept V2 "Crew Quarters" scene generator.
    /// Milestone 1: shell + glazing. Milestone 2: furnishings (see
    /// QuartersFurnishings.cs). Milestone 3: seat anchors (see
    /// SeatAnchorController.cs) + star coverage fix. Milestone 4: URP
    /// migration, baked cove lighting (area lights + emissive strips, one
    /// mixed runtime light), a bake menu item, star shader V2 (colour
    /// temperature, halos, galactic band, shooting stars), and the bed
    /// anchor reworked to a reclining pose. Milestone 5: ambient audio V2
    /// (layered engine bed, brown noise, softer beeps), lateral star motion
    /// (ship reads as flying forward), dimmer desk lamp, and a media wall
    /// later retired in Milestone 8. Milestone 8: HDR + bloom trial and fixed
    /// foveated rendering. Milestone 9: Jovian Dawn planet and ring.
    ///
    /// Milestone 4 workflow: Setup Quarters Scene (V2) → Bake Quarters
    /// Lighting → Build Quarters APK. The build no longer regenerates the
    /// scene (that would discard the bake); regenerate explicitly via the
    /// setup menu, then re-bake.
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
            ConfigureUrpPipeline();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Dim flat ambient: the baked coves carry the room from Milestone 4 on.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.17f, 0.165f, 0.155f);
            RenderSettings.fog = false;

            ConfigureLightingSettings();

            var materials = CreateQuartersMaterials();

            var root = new GameObject("Quarters Shell").transform;
            BuildQuartersShell(root, materials);
            var starSurface = BuildGlazing(root, materials);
            BuildPlanet(root);
            QuartersFurnishings.BuildAll(root);
            BuildBakedLightRig();
            AddXrRig();
            AddControllers(starSurface);
            BuildPostProcessing();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("Starship Cabin: Quarters V2 scene generated at " + ScenePath);
        }

        [MenuItem("Starship Cabin/Bake Quarters Lighting")]
        public static void BakeQuartersLighting()
        {
            if (!File.Exists(ScenePath))
            {
                throw new InvalidOperationException(
                    "Quarters scene not found — run Starship Cabin/Setup Quarters Scene (V2) first.");
            }

            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            ConfigureLightingSettings();
            Lightmapping.BakeAsync();
            Debug.Log("Starship Cabin: lightmap bake started. Save the scene when it completes, then Build Quarters APK.");
        }

        [MenuItem("Starship Cabin/Build Quarters APK")]
        public static void BuildQuartersApk()
        {
            // Milestone 4: the build no longer regenerates the scene — that
            // would discard baked lightmaps. Regenerate + re-bake explicitly.
            if (!File.Exists(ScenePath))
            {
                SetupQuartersScene();
            }
            else
            {
                ConfigureAndroidPlayer();
                ConfigureOpenXrForQuest();
                ConfigureUrpPipeline();
            }

            Directory.CreateDirectory("Builds");
            var buildPath = "Builds/StarshipCabin-Quarters-Beta.apk";

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
                new Vector2(-3.0f, -2.05f), new Vector2(0.7f, -2.05f), // extended under the couch (Milestone 2)
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
            // near-zero parallax, so the stars read as distant.
            //
            // Milestone 3 coverage fix: oblique sight lines (e.g. couch →
            // alcove pane) project ~5× beyond the pane edge onto the offset
            // plane, so the quad must extend far past the slope bounds to
            // cover every pane from every seat (the old ±8 quad left the
            // alcove pane black from the couch). UVs scale with physical size
            // (u: 50/16, v: 28/10.5) so star density per metre is unchanged.
            var starMesh = QuartersMeshes.UvQuad(
                "Quarters Star Surface",
                SlopePoint(-25f, -10f, -6f),
                SlopePoint(25f, -10f, -6f),
                SlopePoint(25f, 18f, -6f),
                SlopePoint(-25f, 18f, -6f),
                3.125f, 2.667f);
            var starObject = MeshObject(glazingRoot, "Star Window Surface", starMesh, mats.Stars);
            GameObjectUtility.SetStaticEditorFlags(starObject, 0); // animated shader: keep out of batching/GI
            return starObject.AddComponent<StarWindowSurface>();
        }

        // ------------------------------------------------------------------
        // Planet (Milestone 9): "Jovian Dawn", the first hero world.
        // ------------------------------------------------------------------

        // Planet sits BETWEEN the glass and the star backdrop plane (which is at
        // slope offset -6). Opaque + normal depth composites it: it occludes the
        // stars it covers, the field fills the rest. No render-order tricks.
        // Placement is slope space (see SlopePoint): u along the window wall,
        // v up the slope, offset = metres out from the slope (0 = glass line;
        // must stay between 0 and -6). >>> TUNE u / v / offset / radius on device.
        private const float PlanetSlopeU = 0.2f;    // ~centre lounge window
        private const float PlanetSlopeV = 1.60f;   // high in the panes
        private const float PlanetOffset = -4.0f;   // out from the slope; keep > -6
        private const float PlanetRadius = 1.6f;
        private static readonly Vector3 PlanetSunDir = new(-0.55f, 0.30f, 0.78f);
        private const bool PlanetHasRing = false;   // off until placement is confirmed; re-enable later
        private const float RingInnerMul = 1.55f;
        private const float RingOuterMul = 2.35f;
        private static readonly Vector3 RingTiltEuler = new(74f, 12f, 0f);

        private static void BuildPlanet(Transform root)
        {
            var planetRoot = new GameObject("Planet (Jovian Dawn)").transform;
            planetRoot.SetParent(root);
            planetRoot.position = SlopePoint(PlanetSlopeU, PlanetSlopeV, PlanetOffset);

            var sphere = BuildUvSphere("Quarters Planet", PlanetRadius, 96, 48);
            var body = MeshObject(planetRoot, "Planet Body", sphere, CreatePlanetMaterial());
            GameObjectUtility.SetStaticEditorFlags(body, 0);
            body.AddComponent<PlanetSurface>().sunDirection = PlanetSunDir;

            if (PlanetHasRing)
            {
                var ring = BuildRingMesh("Quarters Planet Ring", PlanetRadius * RingInnerMul, PlanetRadius * RingOuterMul, 128);
                var ringObject = MeshObject(planetRoot, "Planet Ring", ring, CreateRingMaterial());
                ringObject.transform.localPosition = Vector3.zero;
                ringObject.transform.localRotation = Quaternion.Euler(RingTiltEuler);
                GameObjectUtility.SetStaticEditorFlags(ringObject, 0);
            }
        }

        private static Mesh BuildUvSphere(string name, float radius, int lon, int lat)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (var y = 0; y <= lat; y++)
            {
                var v = (float)y / lat;
                var theta = v * Mathf.PI;
                float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
                for (var x = 0; x <= lon; x++)
                {
                    var u = (float)x / lon;
                    var phi = u * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(phi) * sinT, cosT, Mathf.Sin(phi) * sinT);
                    verts.Add(dir * radius);
                    normals.Add(dir);
                    uvs.Add(new Vector2(u, v));
                }
            }

            var stride = lon + 1;
            for (var y = 0; y < lat; y++)
            {
                for (var x = 0; x < lon; x++)
                {
                    var a = y * stride + x;
                    var b = a + stride;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildRingMesh(string name, float inner, float outer, int seg)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (var i = 0; i <= seg; i++)
            {
                var ang = (float)i / seg * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                verts.Add(dir * inner);
                uvs.Add(new Vector2(0f, (float)i / seg));
                verts.Add(dir * outer);
                uvs.Add(new Vector2(1f, (float)i / seg));
            }

            for (var i = 0; i < seg; i++)
            {
                var a = i * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreatePlanetMaterial()
        {
            const string path = "Assets/Materials/Planet Jovian Dawn.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("StarshipCabin/Planet");
                if (shader == null)
                {
                    throw new InvalidOperationException("StarshipCabin/Planet shader not found (Milestone 9).");
                }
                mat = new Material(shader) { name = "Planet Jovian Dawn" };
                AssetDatabase.CreateAsset(mat, path);
            }

            // M9 fix v2: calm the exposure so bands are visible and the sunlit
            // face doesn't clip to a white blob under HDR bloom.
            mat.SetFloat("_DayBoost", 1.15f);
            mat.SetFloat("_RimStrength", 0.7f);
            mat.SetFloat("_NightLevel", 0.05f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateRingMaterial()
        {
            const string path = "Assets/Materials/Planet Ring.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("StarshipCabin/PlanetRing");
            if (shader == null)
            {
                throw new InvalidOperationException("StarshipCabin/PlanetRing shader not found.");
            }

            var mat = new Material(shader) { name = "Planet Ring" };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
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

        private static void BuildBakedLightRig()
        {
            // Milestone 4: the concept's lighting plan. Baked area lights along
            // the cove lines (plus the already-BakedEmissive cove strips), warm
            // baked points for the alcove/desk pools, and exactly ONE mixed
            // runtime light — the cool starlight fill behind the glazing.
            var lightsRoot = new GameObject("Baked Light Rig").transform;

            var warmCove = new Color(1f, 0.882f, 0.702f);
            var warmPool = new Color(1f, 0.72f, 0.42f);

            // Key: sill cove area light grazing up the glazed slope.
            AreaLight(lightsRoot, "Sill Cove Key", new Vector3(0f, 0.82f, -2.48f),
                Quaternion.LookRotation(SlopeNormal, Vector3.up), new Vector2(5.8f, 0.15f), warmCove, 2.4f, 5.5f);

            // Fill: ceiling perimeter coves, angled slightly inward.
            AreaLight(lightsRoot, "Ceiling Cove Left", new Vector3(-3.02f, 2.40f, 0.6f),
                Quaternion.LookRotation(Vector3.down + Vector3.right * 0.35f, Vector3.forward), new Vector2(3.8f, 0.10f), warmCove, 1.5f, 4.5f);
            AreaLight(lightsRoot, "Ceiling Cove Right", new Vector3(3.02f, 2.40f, 0.6f),
                Quaternion.LookRotation(Vector3.down + Vector3.left * 0.35f, Vector3.forward), new Vector2(3.8f, 0.10f), warmCove, 1.5f, 4.5f);
            AreaLight(lightsRoot, "Ceiling Cove Inner", new Vector3(0f, 2.40f, 2.48f),
                Quaternion.LookRotation(Vector3.down + Vector3.back * 0.35f, Vector3.right), new Vector2(6.0f, 0.10f), warmCove, 1.3f, 4.5f);
            AreaLight(lightsRoot, "Ceiling Cove Slope Return", new Vector3(0f, 2.40f, -1.30f),
                Quaternion.LookRotation(Vector3.down + Vector3.forward * 0.35f, Vector3.right), new Vector2(6.0f, 0.10f), warmCove, 1.3f, 4.5f);

            // Local pools.
            BakedPoint(lightsRoot, "Alcove Reading Pool", new Vector3(2.05f, 1.45f, -1.05f), warmPool, 1.0f, 3.0f);
            // Milestone 5: dimmed after headset feedback ("nice but too bright").
            BakedPoint(lightsRoot, "Desk Lamp Pool", new Vector3(-2.9f, 1.15f, 2.3f), warmPool, 0.32f, 2.2f);
            BakedPoint(lightsRoot, "Room Soft Fill", new Vector3(0f, 2.0f, 0.6f), new Color(1f, 0.88f, 0.72f), 1.1f, 8.0f);

            // The only runtime light: cool starlight through the glazing, so
            // ambience mode changes can tint the room subtly in later work.
            var starlight = new GameObject("Starlight Fill (Mixed)");
            starlight.transform.SetParent(lightsRoot);
            starlight.transform.position = new Vector3(0f, 1.9f, -2.0f);
            var mixed = starlight.AddComponent<Light>();
            mixed.type = LightType.Point;
            mixed.color = new Color(0.5f, 0.64f, 0.95f);
            mixed.intensity = 0.5f;
            mixed.range = 4.5f;
            mixed.shadows = LightShadows.None;
            mixed.lightmapBakeType = LightmapBakeType.Mixed;
        }

        private static void AreaLight(
            Transform parent, string name, Vector3 position, Quaternion rotation,
            Vector2 size, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = rotation;
            var light = go.AddComponent<Light>();
            light.type = LightType.Rectangle; // baked-only: emits along local +Z
            light.areaSize = size;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = LightmapBakeType.Baked;
        }

        private static void BakedPoint(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = LightmapBakeType.Baked;
        }

        // ------------------------------------------------------------------
        // URP pipeline + lighting settings
        // ------------------------------------------------------------------

        private static void ConfigureUrpPipeline()
        {
            Directory.CreateDirectory("Assets/Settings");

            const string rendererPath = "Assets/Settings/Quarters Renderer.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            const string pipelinePath = "Assets/Settings/Quarters URP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            // Quest defaults: MSAA 4x. HDR is ON for the Milestone 8 trial so
            // emissive coves and bright stars can exceed 1.0 and bloom; fixed
            // foveated rendering offsets the frame cost.
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = true;
            pipeline.shadowDistance = 8f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;

            // Assign to every quality level so device tiers can't fall back to Built-in.
            var originalLevel = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(originalLevel, applyExpensiveChanges: false);

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureLightingSettings()
        {
            const string path = "Assets/Settings/Quarters Lighting.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings { name = "Quarters Lighting" };
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            settings.lightmapMaxSize = 1024;
            settings.lightmapResolution = 16f; // texels/m — small room, crisp coves
            settings.lightmapPadding = 2;
            settings.ao = true;
            settings.aoMaxDistance = 0.6f;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            EditorUtility.SetDirty(settings);

            Lightmapping.lightingSettings = settings;
        }

        // ------------------------------------------------------------------
        // Post-processing (Milestone 8): a single global bloom volume so the
        // emissive coves and brightest stars glow under HDR.
        //
        // Threshold is below 1.0 on purpose: StarWindow.shader tone-maps its
        // own output to <= 1.0 (1 - exp(-col)), so a >=1.0 threshold would not
        // catch the stars. No Tonemapping override is added; the shader already
        // tone-maps, and adding another pass would double it.
        // ------------------------------------------------------------------

        private static void BuildPostProcessing()
        {
            var profile = CreatePostFxProfile();

            var volumeObject = new GameObject("Post Process Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static VolumeProfile CreatePostFxProfile()
        {
            const string path = "Assets/Settings/Quarters PostFX.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null)
            {
                return existing;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(overrides: true);
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.62f);

            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void AddXrRig()
        {
            // Milestone 3: the SeatAnchorController owns the origin transform at
            // runtime (it applies the couch anchor on Start, fixing the old
            // "sitting on the low table" spawn). The editor-time pose below is
            // only a preview approximation of anchor 1.
            var origin = new GameObject("XR Origin (Quarters)");
            origin.transform.position = new Vector3(-1.6f, 0f, -1.45f);
            origin.transform.rotation = Quaternion.identity;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(origin.transform, worldPositionStays: false);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.012f, 0.018f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 120f;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.None;

            var trackedPose = cameraObject.AddComponent<TrackedPoseDriver>();
            trackedPose.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            trackedPose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<FoveationController>();

            AddSeatAnchors(origin, cameraObject.transform);
        }

        private static void AddSeatAnchors(GameObject origin, Transform cameraTransform)
        {
            // Comfort fade overlay: small quad in front of the camera, alpha
            // driven by SeatAnchorController. Renders in the Overlay queue with
            // ZTest Always, so it covers everything during hops.
            var fadeMesh = QuartersMeshes.UvQuad(
                "Quarters Fade Quad",
                new Vector3(-1.2f, -1.2f, 0f),
                new Vector3(1.2f, -1.2f, 0f),
                new Vector3(1.2f, 1.2f, 0f),
                new Vector3(-1.2f, 1.2f, 0f));
            var fadeObject = MeshObject(cameraTransform, "Fade Overlay", fadeMesh, CreateFadeMaterial());
            fadeObject.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            fadeObject.transform.localRotation = Quaternion.identity;
            GameObjectUtility.SetStaticEditorFlags(fadeObject, 0); // follows the camera

            var controller = origin.AddComponent<SeatAnchorController>();
            controller.cameraTransform = cameraTransform;
            controller.fadeRenderer = fadeObject.GetComponent<MeshRenderer>();
            controller.anchors = new[]
            {
                // Eye points are world-space targets; y is the eye height at
                // that seat, so each anchor is a distinct perspective even if
                // the user stays physically seated throughout.
                new SeatAnchor { anchorName = "Couch", eyePoint = new Vector3(-1.6f, 1.10f, -1.42f), yawDegrees = 0f },
                new SeatAnchor { anchorName = "Bed (sitting)", eyePoint = new Vector3(1.42f, 1.22f, -0.10f), yawDegrees = 225f },
                // Reworked from "lying" after headset feedback: eye at 0.78 m
                // felt like sitting *inside* the mattress when physically
                // seated. Reclining against the headboard reads naturally in a
                // seated posture and keeps the up-through-the-glass view.
                // (For true flat-on-your-back: set y back to ~0.78.)
                new SeatAnchor { anchorName = "Bed (reclining)", eyePoint = new Vector3(2.05f, 0.95f, -1.00f), yawDegrees = 0f },
                new SeatAnchor { anchorName = "Desk", eyePoint = new Vector3(-2.2f, 1.18f, 2.0f), yawDegrees = 270f }
            };

            EditorUtility.SetDirty(controller);
        }

        private static Material CreateFadeMaterial()
        {
            const string path = "Assets/Materials/Fade Overlay.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("StarshipCabin/FadeOverlay");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "StarshipCabin/FadeOverlay shader not found. Ensure Assets/Shaders/FadeOverlay.shader is imported.");
            }

            var mat = new Material(shader) { name = "Fade Overlay" };
            mat.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
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

        internal static Material CreateMaterial(string name, Color color)
        {
            var path = $"Assets/Materials/{name}.mat";
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                throw new InvalidOperationException(
                    "URP Lit shader not found. Ensure com.unity.render-pipelines.universal is installed (Milestone 4).");
            }

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                // Milestone 4: upgrade any material generated under Built-in RP.
                if (existing.shader != urpLit && existing.shader.name == "Standard")
                {
                    existing.shader = urpLit;
                    existing.SetColor("_BaseColor", color);
                    existing.SetFloat("_Smoothness", 0.25f);
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }

            var mat = new Material(urpLit) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.25f); // matte interior surfaces
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        internal static Material CreateEmissiveMaterial(string name, Color color, Color emission, float intensity)
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
            // URP Lit transparent surface (Milestone 4).
            var mat = CreateEmissiveMaterial(name, color, emission, intensity);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f); // 0 = opaque, 1 = transparent
            mat.SetFloat("_Blend", 0f);   // alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_Smoothness", 0.6f); // softened after HDR made the alcove reflection hot
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateStarMaterial()
        {
            const string path = "Assets/Materials/Star Window Surface.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("StarshipCabin/StarWindow");
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "StarshipCabin/StarWindow shader not found. Ensure Assets/Shaders/StarWindow.shader is imported.");
                }

                mat = new Material(shader) { name = "Star Window Surface" };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetFloat("_Twinkle", 0.10f);
            mat.renderQueue = -1; // follow the shader (Geometry)
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------
        // Object + asset helpers
        // ------------------------------------------------------------------

        internal static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            return MeshObject(parent, name, mesh, material, Vector3.zero, Quaternion.identity);
        }

        internal static GameObject MeshObject(
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
            PlayerSettings.productName = "Starship Cabin Beta";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "jp.openclaw.starshipcabin.beta");
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

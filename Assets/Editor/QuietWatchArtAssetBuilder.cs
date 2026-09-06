using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Imports and instantiates the Blender-authored Quiet Watch benchmark.
    /// The source FBXs contain deliberate material slots; this class replaces
    /// those embedded previews with Quest-tuned URP materials and builds the
    /// runtime LOD groups used by Harbour and Long Formation.
    /// </summary>
    internal static class QuietWatchArtAssetBuilder
    {
        private const string ModelRoot = "Assets/Art/QuietWatch/Models";
        private const string TextureRoot = "Assets/Art/QuietWatch/Textures";
        private const string MaterialRoot = "Assets/Materials/Quiet Watch Benchmark";
        internal const int ExteriorLayer = 8;

        private sealed class MaterialSet
        {
            public Material Hull;
            public Material Armour;
            public Material Machinery;
            public Material Blue;
            public Material Amber;
            public Material Glass;
        }

        private static MaterialSet materials;

        public static void PrepareAssets()
        {
            Directory.CreateDirectory(MaterialRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTexture("QW_Hull_BaseColor.png", false, true);
            ConfigureTexture("QW_Hull_MetallicSmoothness.png", false, false);
            ConfigureTexture("QW_Hull_Normal.png", true, false);
            ConfigureTexture("QW_Hull_Occlusion.png", false, false);
            ConfigureTexture("QW_Hull_Emission.png", false, false);

            foreach (var family in new[] { "CommandShip", "EscortSpear", "EscortWing", "HarbourSector" })
            {
                for (var lod = 0; lod < 3; lod++)
                {
                    ConfigureModel($"QW_{family}_LOD{lod}.fbx");
                }
            }

            materials = BuildMaterials();
            AssetDatabase.SaveAssets();
        }

        public static Transform InstantiateLod(
            Transform parent, string family, string name, Vector3 position,
            Quaternion rotation, float scale, bool castShadows = true)
        {
            if (materials == null)
            {
                PrepareAssets();
            }

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = rotation;
            root.localScale = Vector3.one * scale;
            root.gameObject.layer = ExteriorLayer;
            GameObjectUtility.SetStaticEditorFlags(root.gameObject, 0);

            var lods = new List<LOD>();
            var thresholds = new[] { 0.38f, 0.14f, 0.025f };
            for (var index = 0; index < 3; index++)
            {
                var path = $"{ModelRoot}/QW_{family}_LOD{index}.fbx";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null)
                {
                    throw new InvalidOperationException($"Quiet Watch model was not imported: {path}");
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                instance.name = $"{name} LOD{index}";
                instance.transform.SetParent(root, false);
                instance.transform.localPosition = Vector3.zero;
                // Blender assets use X width, Y fore/aft, Z up. Rotate once at
                // the import root so fore/aft becomes Unity Z and up becomes Y.
                instance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                instance.transform.localScale = Vector3.one;
                SetLayerRecursively(instance.transform, ExteriorLayer);

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    ReplaceMaterials(renderer);
                    renderer.shadowCastingMode = castShadows
                        ? ShadowCastingMode.On
                        : ShadowCastingMode.Off;
                    renderer.receiveShadows = castShadows;
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    renderer.allowOcclusionWhenDynamic = true;
                }
                lods.Add(new LOD(thresholds[index], renderers));
            }

            var group = root.gameObject.AddComponent<LODGroup>();
            group.animateCrossFading = false;
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();

            if (family != "HarbourSector")
            {
                // Engine apertures are already emissive geometry in every imported LOD.
                // Do not add generic aft spheres: they float outside the real nozzles.
                if (family == "CommandShip")
                {
                    var details = AddCommandScaleDetails(root);
                    for (var i = 0; i < 2; i++)
                        lods[i] = new LOD(thresholds[i], lods[i].renderers.Concat(details).ToArray());
                    group.SetLODs(lods.ToArray());
                    group.RecalculateBounds();
                }
            }
            return root;
        }

        private static Renderer[] AddCommandScaleDetails(Transform ship)
        {
            var windows = new MeshDraft();
            var marks = new MeshDraft();
            // Original import orientation is retained. Recessed cabin decks sit
            // under the mission shoulder, with room-sized rather than hangar-
            // sized apertures once the assembly is scaled to fleet metres.
            for (var side = -1; side <= 1; side += 2)
            {
                for (var deck = 0; deck < 3; deck++)
                {
                    for (var bay = 0; bay < 36; bay++)
                    {
                        if ((bay * 13 + deck * 7) % 11 < 3) continue;
                        var z = -3.6f + bay * 0.16f;
                        var x = side * 1.91f;
                        var y = -1.23f - deck * 0.14f;
                        var a = new Vector3(x,y-0.026f,z-0.042f);
                        var b = new Vector3(x,y+0.026f,z-0.042f);
                        var c = new Vector3(x,y+0.026f,z+0.042f);
                        var d = new Vector3(x,y-0.026f,z+0.042f);
                        windows.AddQuadOriented(a,b,c,d,Vector3.right*side);
                    }
                }
                // Repeating service markings establish the length of the hull.
                for (var bay = 0; bay < 8; bay++)
                {
                    var z = -3.6f + bay * 0.75f;
                    marks.AddQuadOriented(new Vector3(side*1.2f,-1.71f,z),
                        new Vector3(side*1.65f,-1.71f,z), new Vector3(side*1.65f,-1.71f,z+0.10f),
                        new Vector3(side*1.2f,-1.71f,z+0.10f),Vector3.down);
                }
            }
            var litWindows = Emissive("Resolute Occupied Decks",new Color(0.21f,0.17f,0.12f),
                new Color(1f,0.73f,0.42f),1.25f,0f,0.1f);
            var paint = Lit("Resolute Service Markings",new Color(0.78f,0.68f,0.46f),0f,0.18f);
            var decks = QuartersSceneSetup.MeshObject(ship,"Room-scale occupied decks",windows.ToMesh("Resolute Occupied Deck Windows"),litWindows);
            var stripes = QuartersSceneSetup.MeshObject(ship,"Hull service markings",marks.ToMesh("Resolute Service Markings"),paint);
            foreach (var detail in new[] { decks, stripes })
            {
                detail.transform.localPosition = Vector3.zero;
                detail.transform.localRotation = Quaternion.identity;
                detail.transform.localScale = Vector3.one;
                detail.layer = ExteriorLayer;
                GameObjectUtility.SetStaticEditorFlags(detail,0);
                var renderer = detail.GetComponent<Renderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
            return new[] { decks.GetComponent<Renderer>(), stripes.GetComponent<Renderer>() };
        }

        public static Light AddExteriorSun(
            Transform parent, string name, Quaternion rotation, Color color,
            float intensity, bool shadows)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localRotation = rotation;
            lightObject.layer = ExteriorLayer;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << ExteriorLayer;
            light.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            light.shadowStrength = 0.58f;
            light.shadowBias = 0.075f;
            light.shadowNormalBias = 0.45f;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            return light;
        }

        private static void ConfigureTexture(string fileName, bool normal, bool srgb)
        {
            var path = $"{TextureRoot}/{fileName}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Quiet Watch texture was not imported: {path}");
            }
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb && !normal;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static void ConfigureModel(string fileName)
        {
            var path = $"{ModelRoot}/{fileName}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Quiet Watch FBX was not imported: {path}");
            }
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static MaterialSet BuildMaterials()
        {
            var baseMap = LoadTexture("QW_Hull_BaseColor.png");
            var metallic = LoadTexture("QW_Hull_MetallicSmoothness.png");
            var normal = LoadTexture("QW_Hull_Normal.png");
            var occlusion = LoadTexture("QW_Hull_Occlusion.png");
            var emission = LoadTexture("QW_Hull_Emission.png");

            return new MaterialSet
            {
                Hull = Pbr("Benchmark Hull", new Color(0.78f, 0.84f, 0.90f),
                    baseMap, metallic, normal, occlusion, emission, new Color(0.34f, 0.64f, 0.95f)),
                Armour = Pbr("Benchmark Armour", new Color(1.0f, 1.0f, 1.0f),
                    baseMap, metallic, normal, occlusion, emission, new Color(0.42f, 0.68f, 0.92f)),
                Machinery = Lit("Benchmark Machinery", new Color(0.055f, 0.068f, 0.082f), 0.82f, 0.34f),
                Blue = Emissive("Benchmark Navigation Blue", new Color(0.16f, 0.78f, 1.0f),
                    new Color(0.12f, 0.68f, 1.0f), 4.2f),
                Amber = Emissive("Benchmark Navigation Amber", new Color(1.0f, 0.34f, 0.055f),
                    new Color(1.0f, 0.26f, 0.045f), 3.5f),
                Glass = Emissive("Benchmark Bridge Glass", new Color(0.025f, 0.12f, 0.18f),
                    new Color(0.035f, 0.22f, 0.34f), 0.9f, 0.86f, 0.82f)
            };
        }

        private static Material Pbr(
            string name, Color tint, Texture baseMap, Texture metallic, Texture normal,
            Texture occlusion, Texture emission, Color emissionColor)
        {
            var mat = Lit(name, tint, 1f, 1f);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.SetTexture("_BumpMap", normal);
            mat.SetTexture("_OcclusionMap", occlusion);
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", emissionColor);
            mat.SetFloat("_BumpScale", 0.72f);
            mat.SetFloat("_OcclusionStrength", 0.82f);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_OCCLUSIONMAP");
            mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Lit(string name, Color color, float metallic, float smoothness)
        {
            var path = $"{MaterialRoot}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            }
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Emissive(
            string name, Color color, Color emission, float intensity,
            float metallic = 0.08f, float smoothness = 0.56f)
        {
            var mat = Lit(name, color, metallic, smoothness);
            mat.SetColor("_EmissionColor", emission * intensity);
            mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            var path = $"{TextureRoot}/{fileName}";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException($"Quiet Watch texture was not loaded: {path}");
            }
            return texture;
        }

        private static void ReplaceMaterials(Renderer renderer)
        {
            var slots = renderer.sharedMaterials;
            for (var index = 0; index < slots.Length; index++)
            {
                var normalized = slots[index] == null
                    ? string.Empty
                    : slots[index].name.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
                if (normalized.Contains("emissiveblue")) slots[index] = materials.Blue;
                else if (normalized.Contains("emissiveamber")) slots[index] = materials.Amber;
                else if (normalized.Contains("bridgeglass")) slots[index] = materials.Glass;
                else if (normalized.Contains("machinery")) slots[index] = materials.Machinery;
                else if (normalized.Contains("armour")) slots[index] = materials.Armour;
                else slots[index] = materials.Hull;
            }
            renderer.sharedMaterials = slots;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
            {
                SetLayerRecursively(child, layer);
            }
        }
    }
}

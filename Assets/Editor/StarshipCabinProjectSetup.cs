using System.IO;
using UnityEditor;
using UnityEditor.Build;
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
    public static class StarshipCabinProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Cabin_Seated_MVP.unity";

        [MenuItem("Starship Cabin/Setup MVP Scene")]
        public static void SetupMvpScene()
        {
            EnsureFolders();
            ConfigureAndroidPlayer();
            ConfigureOpenXrForQuest();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.31f, 0.27f);
            RenderSettings.fog = false;

            var warmWall = Material("Warm Architectural Wall", new Color(0.54f, 0.50f, 0.43f));
            var wallInset = Material("Inset Wall Panel", new Color(0.36f, 0.34f, 0.30f));
            var darkTrim = Material("Graphite Trim", new Color(0.105f, 0.11f, 0.12f));
            var floorMat = Material("Quiet Charcoal Floor", new Color(0.16f, 0.15f, 0.135f));
            var carpetMat = Material("Muted Cabin Rug", new Color(0.25f, 0.16f, 0.14f));
            var glassMat = EmissiveMaterial("Starfield Glass", new Color(0.016f, 0.028f, 0.052f), new Color(0.03f, 0.08f, 0.17f), 0.75f);
            var amberMat = EmissiveMaterial("Soft Amber Panel", new Color(1.0f, 0.60f, 0.20f), new Color(1.0f, 0.50f, 0.12f), 1.75f);
            var tealMat = EmissiveMaterial("Soft Teal Panel", new Color(0.14f, 0.78f, 0.72f), new Color(0.10f, 0.70f, 0.64f), 1.35f);
            var blueMat = EmissiveMaterial("Soft Blue Status", new Color(0.16f, 0.50f, 0.92f), new Color(0.10f, 0.40f, 0.82f), 1.1f);
            var cushionMat = Material("Deep Rest Cushion", new Color(0.18f, 0.20f, 0.24f));
            var woodMat = Material("Warm Cabin Accent", new Color(0.45f, 0.31f, 0.20f));

            var cabinRoot = new GameObject("Atmosphere Cabin Shell");
            CreateShell(cabinRoot.transform, warmWall, wallInset, darkTrim, floorMat, carpetMat);
            CreateForwardWindow(cabinRoot.transform, glassMat, darkTrim);
            CreateFurniture(cabinRoot.transform, darkTrim, cushionMat, woodMat);
            CreateControlSurfaces(cabinRoot.transform, darkTrim, amberMat, tealMat, blueMat);
            CreateLightRails(cabinRoot.transform, darkTrim, amberMat, tealMat);

            AddMainCamera();
            AddStarfield();
            AddLights();
            AddExperienceController();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Materials");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Audio");
            Directory.CreateDirectory("Builds");
        }

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

        private static Material Material(string name, Color color)
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

        private static Material EmissiveMaterial(string name, Color color, Color emission, float intensity)
        {
            var mat = Material(name, color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission * intensity);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return go;
        }

        private static GameObject ChildPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = Primitive(name, type, position, scale, material);
            go.transform.SetParent(parent);
            return go;
        }

        private static void CreateShell(Transform root, Material warmWall, Material wallInset, Material darkTrim, Material floorMat, Material carpetMat)
        {
            ChildPrimitive(root, "Floor", PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f), new Vector3(8.6f, 0.1f, 8.4f), floorMat);
            ChildPrimitive(root, "Soft Center Rug", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0.85f), new Vector3(4.7f, 0.025f, 3.9f), carpetMat);
            ChildPrimitive(root, "Rear Wall", PrimitiveType.Cube, new Vector3(0f, 1.65f, 3.70f), new Vector3(8.6f, 3.3f, 0.16f), warmWall);
            ChildPrimitive(root, "Left Wall", PrimitiveType.Cube, new Vector3(-4.3f, 1.65f, 0f), new Vector3(0.16f, 3.3f, 8.4f), warmWall);
            ChildPrimitive(root, "Right Wall", PrimitiveType.Cube, new Vector3(4.3f, 1.65f, 0f), new Vector3(0.16f, 3.3f, 8.4f), warmWall);
            ChildPrimitive(root, "Ceiling", PrimitiveType.Cube, new Vector3(0f, 3.32f, 0f), new Vector3(8.6f, 0.12f, 8.4f), warmWall);

            for (var i = 0; i < 4; i++)
            {
                var z = -1.95f + i * 1.25f;
                ChildPrimitive(root, $"Left Inset Panel {i + 1}", PrimitiveType.Cube, new Vector3(-4.20f, 1.68f, z), new Vector3(0.06f, 2.2f, 0.82f), wallInset);
                ChildPrimitive(root, $"Right Inset Panel {i + 1}", PrimitiveType.Cube, new Vector3(4.20f, 1.68f, z), new Vector3(0.06f, 2.2f, 0.82f), wallInset);
            }

            ChildPrimitive(root, "Rear Horizontal Trim", PrimitiveType.Cube, new Vector3(0f, 2.66f, 3.59f), new Vector3(7.8f, 0.08f, 0.12f), darkTrim);
            ChildPrimitive(root, "Rear Lower Trim", PrimitiveType.Cube, new Vector3(0f, 0.78f, 3.59f), new Vector3(7.8f, 0.08f, 0.12f), darkTrim);
        }

        private static void CreateForwardWindow(Transform root, Material glassMat, Material darkTrim)
        {
            ChildPrimitive(root, "Forward Bulkhead Left", PrimitiveType.Cube, new Vector3(-3.62f, 1.65f, -3.70f), new Vector3(1.25f, 3.3f, 0.16f), darkTrim);
            ChildPrimitive(root, "Forward Bulkhead Right", PrimitiveType.Cube, new Vector3(3.62f, 1.65f, -3.70f), new Vector3(1.25f, 3.3f, 0.16f), darkTrim);
            ChildPrimitive(root, "Forward Bulkhead Top", PrimitiveType.Cube, new Vector3(0f, 2.86f, -3.70f), new Vector3(6.0f, 0.52f, 0.16f), darkTrim);
            ChildPrimitive(root, "Forward Bulkhead Bottom", PrimitiveType.Cube, new Vector3(0f, 0.58f, -3.70f), new Vector3(6.0f, 0.72f, 0.16f), darkTrim);
            ChildPrimitive(root, "Panoramic Observation Glass", PrimitiveType.Cube, new Vector3(0f, 1.80f, -3.75f), new Vector3(5.7f, 1.7f, 0.06f), glassMat);
            ChildPrimitive(root, "Window Top Trim", PrimitiveType.Cube, new Vector3(0f, 2.69f, -3.59f), new Vector3(6.05f, 0.12f, 0.16f), darkTrim);
            ChildPrimitive(root, "Window Bottom Trim", PrimitiveType.Cube, new Vector3(0f, 0.92f, -3.59f), new Vector3(6.05f, 0.12f, 0.16f), darkTrim);
            ChildPrimitive(root, "Window Left Trim", PrimitiveType.Cube, new Vector3(-3.02f, 1.80f, -3.59f), new Vector3(0.12f, 1.9f, 0.16f), darkTrim);
            ChildPrimitive(root, "Window Right Trim", PrimitiveType.Cube, new Vector3(3.02f, 1.80f, -3.59f), new Vector3(0.12f, 1.9f, 0.16f), darkTrim);
        }

        private static void CreateFurniture(Transform root, Material darkTrim, Material cushionMat, Material woodMat)
        {
            ChildPrimitive(root, "Low Lounge Base", PrimitiveType.Cube, new Vector3(-1.8f, 0.32f, 1.25f), new Vector3(2.0f, 0.34f, 1.15f), darkTrim);
            ChildPrimitive(root, "Low Lounge Cushion", PrimitiveType.Cube, new Vector3(-1.8f, 0.56f, 1.25f), new Vector3(1.85f, 0.18f, 1.0f), cushionMat);
            var back = ChildPrimitive(root, "Low Lounge Back", PrimitiveType.Cube, new Vector3(-1.8f, 0.95f, 1.78f), new Vector3(1.9f, 0.7f, 0.16f), cushionMat);
            back.transform.rotation = Quaternion.Euler(-8f, 0f, 0f);

            ChildPrimitive(root, "Side Table Base", PrimitiveType.Cube, new Vector3(1.65f, 0.36f, 1.22f), new Vector3(0.7f, 0.46f, 0.7f), woodMat);
            ChildPrimitive(root, "Side Table Top", PrimitiveType.Cube, new Vector3(1.65f, 0.64f, 1.22f), new Vector3(0.95f, 0.08f, 0.95f), darkTrim);

            ChildPrimitive(root, "Quiet Storage Bench", PrimitiveType.Cube, new Vector3(0f, 0.42f, 2.55f), new Vector3(3.6f, 0.44f, 0.6f), darkTrim);
            ChildPrimitive(root, "Bench Cushion", PrimitiveType.Cube, new Vector3(0f, 0.70f, 2.55f), new Vector3(3.35f, 0.16f, 0.48f), cushionMat);
        }

        private static void CreateControlSurfaces(Transform root, Material darkTrim, Material amberMat, Material tealMat, Material blueMat)
        {
            ChildPrimitive(root, "Seated Console", PrimitiveType.Cube, new Vector3(0f, 0.58f, -1.55f), new Vector3(2.25f, 0.35f, 0.65f), darkTrim);
            ChildPrimitive(root, "Console Sloped Face", PrimitiveType.Cube, new Vector3(0f, 0.82f, -1.78f), new Vector3(2.15f, 0.08f, 0.36f), darkTrim).transform.rotation = Quaternion.Euler(-16f, 0f, 0f);

            ChildPrimitive(root, "Amber Mode Strip", PrimitiveType.Cube, new Vector3(-0.62f, 0.91f, -1.89f), new Vector3(0.78f, 0.055f, 0.07f), amberMat);
            ChildPrimitive(root, "Teal Mode Strip", PrimitiveType.Cube, new Vector3(0.25f, 0.91f, -1.89f), new Vector3(0.54f, 0.055f, 0.07f), tealMat);
            ChildPrimitive(root, "Blue Status Strip", PrimitiveType.Cube, new Vector3(0.88f, 0.91f, -1.89f), new Vector3(0.34f, 0.055f, 0.07f), blueMat);

            for (var i = 0; i < 5; i++)
            {
                var x = -0.88f + i * 0.44f;
                var mat = i % 3 == 0 ? amberMat : i % 3 == 1 ? tealMat : blueMat;
                ChildPrimitive(root, $"Console Calm Indicator {i + 1}", PrimitiveType.Cube, new Vector3(x, 0.84f, -1.92f), new Vector3(0.22f, 0.035f, 0.05f), mat);
            }
        }

        private static void CreateLightRails(Transform root, Material darkTrim, Material amberMat, Material tealMat)
        {
            ChildPrimitive(root, "Ceiling Left Rail", PrimitiveType.Cube, new Vector3(-1.9f, 3.20f, 0.15f), new Vector3(0.16f, 0.08f, 6.3f), darkTrim);
            ChildPrimitive(root, "Ceiling Right Rail", PrimitiveType.Cube, new Vector3(1.9f, 3.20f, 0.15f), new Vector3(0.16f, 0.08f, 6.3f), darkTrim);
            ChildPrimitive(root, "Warm Ceiling Glow Left", PrimitiveType.Cube, new Vector3(-1.9f, 3.12f, 0.15f), new Vector3(0.08f, 0.04f, 5.4f), amberMat);
            ChildPrimitive(root, "Warm Ceiling Glow Right", PrimitiveType.Cube, new Vector3(1.9f, 3.12f, 0.15f), new Vector3(0.08f, 0.04f, 5.4f), amberMat);
            ChildPrimitive(root, "Cool Window Header Glow", PrimitiveType.Cube, new Vector3(0f, 2.75f, -3.48f), new Vector3(4.4f, 0.06f, 0.08f), tealMat);
        }

        private static void AddMainCamera()
        {
            var cameraObject = new GameObject("Seated Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.22f, 1.08f);
            cameraObject.transform.rotation = Quaternion.Euler(4f, 180f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.012f, 0.018f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 100f;

            var trackedPose = cameraObject.AddComponent<TrackedPoseDriver>();
            trackedPose.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            trackedPose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            cameraObject.AddComponent<AudioListener>();
        }

        private static void AddStarfield()
        {
            var starObject = new GameObject("Procedural Starfield");
            starObject.transform.position = new Vector3(0f, 1.85f, -8.4f);
            starObject.transform.localScale = Vector3.one;

            var particles = starObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 120f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.82f, 1f), Color.white);
            main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            particles.Emit(1200);

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10.5f, 5.2f, 3.6f);

            starObject.AddComponent<StarfieldWindow>();
        }

        private static void AddLights()
        {
            var ambient = new GameObject("Warm Cabin Key Light");
            ambient.transform.position = new Vector3(0f, 2.95f, 1.0f);
            var key = ambient.AddComponent<Light>();
            key.type = LightType.Point;
            key.color = new Color(1f, 0.78f, 0.54f);
            key.intensity = 3.2f;
            key.range = 10.5f;

            var windowGlow = new GameObject("Window Cool Fill");
            windowGlow.transform.position = new Vector3(0f, 2.05f, -3.1f);
            var fill = windowGlow.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.36f, 0.62f, 0.95f);
            fill.intensity = 1.45f;
            fill.range = 6.3f;

            var sideGlow = new GameObject("Amber Side Panel Glow");
            sideGlow.transform.position = new Vector3(-2.75f, 1.8f, -0.4f);
            var side = sideGlow.AddComponent<Light>();
            side.type = LightType.Point;
            side.color = new Color(1f, 0.46f, 0.18f);
            side.intensity = 0.80f;
            side.range = 5.2f;

            var oppositeSideGlow = new GameObject("Teal Side Panel Glow");
            oppositeSideGlow.transform.position = new Vector3(2.75f, 1.8f, -0.4f);
            var opposite = oppositeSideGlow.AddComponent<Light>();
            opposite.type = LightType.Point;
            opposite.color = new Color(0.22f, 0.75f, 0.70f);
            opposite.intensity = 0.55f;
            opposite.range = 5.2f;
        }

        private static void AddExperienceController()
        {
            var controller = new GameObject("CabinExperience");
            controller.AddComponent<CabinExperienceController>();

            var audio = new GameObject("CabinAudio");
            audio.AddComponent<AmbientAudioController>();
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
                throw new System.InvalidOperationException("Unable to create Android XR manager settings.");
            }

            managerSettings.automaticLoading = true;
            managerSettings.automaticRunning = true;

            var assigned = XRPackageMetadataStore.AssignLoader(
                managerSettings,
                "UnityEngine.XR.OpenXR.OpenXRLoader",
                BuildTargetGroup.Android);

            if (!assigned)
            {
                throw new System.InvalidOperationException("Unable to assign OpenXR loader for Android.");
            }

            var openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXrSettings == null)
            {
                throw new System.InvalidOperationException("Unable to load Android OpenXR settings.");
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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Builds the four authored exterior compositions that turn the M1 sky
    /// benchmark into a useful multi-vista headset review build.
    /// </summary>
    internal static class QuietWatchExteriorBuilder
    {
        internal static readonly Vector3 WeatherCenter = new Vector3(32f, -12f, -104f);
        internal static readonly Vector3 WeatherSun = new Vector3(-0.78f, -0.02f, 0.50f);
        internal static readonly Vector3 WeatherRingAngles = new Vector3(63f, 8f, -14f);
        internal const float WeatherRadius = 70f;
        internal const float RingInner = 76f, RingOuter = 112f;
        internal const float MoonShadowRadius = 96f, MoonTravel = 24f, MoonDiameter = 2.6f;
        internal const float FleetScale = 12f;

        public static AuthoredVista BuildHarbour(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 2 - Harbour of Ten Thousand Lights");
            var station = QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "HarbourSector", "Kilometre Harbour Sector",
                new Vector3(1.5f, 3.2f, -44f), Quaternion.Euler(8f, -5f, -7f), 0.88f);
            QuietWatchArtAssetBuilder.AddExteriorSun(
                vista.transform, "Harbour Distant Sun", Quaternion.Euler(32f, -38f, -18f),
                new Color(0.74f, 0.84f, 1.0f), 1.62f, true);
            BuildHarbourHabitation(station);
            BuildHarbourClearanceVolumes(vista.transform, station);

            var travellers = new List<Transform>();
            travellers.Add(QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "EscortWing", "Inbound Customs Cutter",
                new Vector3(9.8f, 0.4f, -29f), Quaternion.Euler(3f, -16f, 2f), 0.68f));
            travellers.Add(QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "EscortSpear", "Docking Tender",
                new Vector3(-14f, 8.5f, -50f), Quaternion.Euler(-4f, 12f, -2f), 0.52f, false));
            travellers.Add(QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "EscortSpear", "Distant Shuttle",
                new Vector3(12f, 11f, -66f), Quaternion.Euler(0f, -20f, 0f), 0.24f, false));

            // Every route is authored in station space and converted to the
            // vista root. This keeps choreography tied to the actual harbour
            // composition if the station is moved or re-aimed later.
            ConfigureHarbourRoute(travellers[0], StationRoute(vista.transform, station,
                    new Vector3(-36f, 10f, -3f),
                    new Vector3(-39f, 11f, 2f),
                    new Vector3(-43f, 14f, 10f),
                    new Vector3(-48f, 19f, 23f),
                    new Vector3(-55f, 25f, 39f)),
                58f, 110f, 0f, 3.2f, 9f, false, true);
            ConfigureHarbourRoute(travellers[1], StationRoute(vista.transform, station,
                    new Vector3(39f, 14f, 18f),
                    new Vector3(29f, 15f, 22f),
                    new Vector3(15f, 15f, 25f),
                    new Vector3(-4f, 14f, 28f),
                    new Vector3(-24f, 12f, 32f),
                    new Vector3(-42f, 10f, 38f)),
                52f, 112f, 0.16f, 2.8f, 7f, false, false);
            ConfigureHarbourRoute(travellers[2], StationRoute(vista.transform, station,
                    new Vector3(-42f, 2f, 34f),
                    new Vector3(-26f, 3f, 35f),
                    new Vector3(-8f, 4f, 36f),
                    new Vector3(12f, 5f, 37f),
                    new Vector3(30f, 6f, 39f),
                    new Vector3(45f, 8f, 42f)),
                70f, 118f, 0.57f, 1.8f, 5f, true, false);

            vista.transform.localScale = Vector3.one * FleetScale;
            AddBlueBackdrop(vista, "Harbour Night World", new Vector3(450f, -1300f, -3000f), 1300f,
                new Vector3(-0.75f, 0.12f, 0.12f), new Vector3(10f, -35f, -18f));
            return Configure(vista, "harbour", "HARBOUR OF TEN THOUSAND LIGHTS", "ORBITAL SAFE HARBOUR",
                AuthoredVistaKind.Harbour, stars, fill, audio, station, travellers.ToArray(), new Color(0.32f, 0.66f, 0.92f));
        }

        public static AuthoredVista BuildBlueMorning(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 3 - Blue Morning");
            var world = AddBlueBackdrop(vista, "Blue World", new Vector3(22f, -83f, -135f), 116f,
                new Vector3(-0.82f, 0.12f, 0.24f), new Vector3(10f, -28f, -12f));
            return Configure(vista, "blue-morning", "BLUE MORNING", "DAWN ABOVE A LIVING WORLD",
                AuthoredVistaKind.BlueMorning, stars, fill, audio, world,
                Array.Empty<Transform>(), new Color(1.0f, 0.54f, 0.30f));
        }

        public static AuthoredVista BuildGreatWeather(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 4 - The Great Weather");
            var center = WeatherCenter;
            var ringRotation = Quaternion.Euler(WeatherRingAngles);
            var sun = WeatherSun.normalized;
            var planetMaterial = MaterialFromShader("Quiet Watch Great Weather", "StarshipCabin/QuietWatchGasGiant");
            var ringMaterial = MaterialFromShader("Great Weather Rings Authored", "StarshipCabin/QuietWatchRings");
            var moonMaterial = MaterialFromShader("Great Weather Moon", "StarshipCabin/QuietWatchMoon");
            foreach (var material in new[] { planetMaterial, ringMaterial, moonMaterial })
            {
                // One proxy metre represents 1,000 km. Keep every member of
                // this system on the same projection and illumination model.
                material.SetFloat("_DistanceScale", 1000000f);
                material.SetVector("_DistanceOrigin", new Vector4(-1.6f, 1.1f, -1.42f, 0f));
                material.SetVector("_SunDirection", sun);
                material.SetVector("_RingCenter", center);
                material.SetVector("_RingNormal", ringRotation * Vector3.forward);
                material.SetVector("_RingRadii", new Vector4(RingInner, RingOuter, 0f, 0f));
                material.SetVector("_PlanetSphere", new Vector4(center.x, center.y, center.z, WeatherRadius));
                EditorUtility.SetDirty(material);
            }
            const string texturePath = "Assets/Art/QuietWatch/Textures/QW_GreatWeather.png";
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing authored weather atlas: " + texturePath);
            importer.maxTextureSize = 4096;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false; // Alpha encodes storm coverage.
            importer.mipmapEnabled = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 4096,
                format = TextureImporterFormat.ASTC_6x6
            });
            importer.SaveAndReimport();
            planetMaterial.SetTexture("_WeatherMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            planetMaterial.SetTexture("_CloudRelief", CelestialTexture("QW_WeatherRelief.png", false, 2048));
            planetMaterial.SetFloat("_CloudLayerHeight", 0.0012f);
            planetMaterial.SetFloat("_CloudReliefStrength", 0.75f);
            planetMaterial.SetColor("_StormColor", new Color(0.58f, 0.29f, 0.12f));
            ringMaterial.SetColor("_LightColor", new Color(0.71f, 0.62f, 0.48f));
            ringMaterial.SetColor("_DarkColor", new Color(0.11f, 0.09f, 0.075f));
            var planet = ExteriorMesh(vista.transform, "Ringed Giant", WeatherSphere("Great Weather Smooth Globe"), planetMaterial, center);
            planet.transform.localScale = Vector3.one * (WeatherRadius * 2f);
            planet.transform.rotation = Quaternion.Euler(0f, -22f, -8f);
            var rings = ExteriorMesh(vista.transform, "Planetary Rings",
                Annulus("Great Weather Ring System", RingOuter, RingInner, 256), ringMaterial, center, ringRotation);
            var radial = ringRotation * Vector3.left;
            // Start eight proxy metres behind an actual ring-shadow ray. The
            // moon gradually clears the outer ring; the distant moon stays put.
            var shadowMoon = Sphere(vista.transform, "Moon in Ring Shadow", moonMaterial,
                center + radial * MoonShadowRadius - sun * 8f, MoonDiameter);
            var farMoon = Sphere(vista.transform, "Far Moon", moonMaterial, new Vector3(92f, 35f, -174f), 3.6f);
            foreach (var body in new[] { planet, rings, shadowMoon, farMoon })
                body.AddComponent<DistantVistaBounds>();
            var atmosphere = AddAtmosphere(vista.transform, "Great Weather Atmospheric Limb", planet, planetMaterial,
                new Color(0.46f, 0.64f, 0.82f), 0.006f);
            var eclipseMoon = Sphere(vista.transform, "Eclipse Moon", moonMaterial, center,
                GreatWeatherEclipse.MoonRadius * 2f);
            eclipseMoon.AddComponent<DistantVistaBounds>();
            foreach (var material in new[] { planetMaterial, ringMaterial, atmosphere.GetComponent<Renderer>().sharedMaterial })
            {
                material.SetFloat("_SolarAngularRadius", GreatWeatherEclipse.SolarAngularRadius);
                EditorUtility.SetDirty(material);
            }
            vista.AddComponent<GreatWeatherEclipse>().Configure(eclipseMoon.transform,
                new[] { planet.GetComponent<Renderer>(), rings.GetComponent<Renderer>(), atmosphere.GetComponent<Renderer>() },
                center, sun, ringRotation * Vector3.forward);
            var authored = Configure(vista, "great-weather", "THE GREAT WEATHER", "RING SHADOW AND STORMS",
                AuthoredVistaKind.GreatWeather, stars, fill, audio, planet.transform,
                new[] { shadowMoon.transform, farMoon.transform }, new Color(0.75f, 0.60f, 0.43f));
            authored.ConfigureMoonEmergence(radial * MoonTravel);
            return authored;
        }

        public static AuthoredVista BuildLongFormation(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 5 - The Long Formation");
            QuietWatchArtAssetBuilder.AddExteriorSun(
                vista.transform, "Formation Distant Sun", Quaternion.Euler(26f, -44f, -12f),
                new Color(0.78f, 0.88f, 1.0f), 1.45f, true);

            // Scale the entire choreography coherently: a 20 m model now reads
            // as a 240 m vessel hundreds of metres away, with the same movement
            // and angular relationships that were reviewed in M6.3.
            vista.transform.localScale = Vector3.one * FleetScale;
            var formationRig = new GameObject("Formation Flight Rig").transform;
            formationRig.SetParent(vista.transform, false);
            GameObjectUtility.SetStaticEditorFlags(formationRig.gameObject, 0);

            // Three substantial vessels read as a deliberate unit. The three
            // tiny far-field ships from the blockout were cut after headset
            // review because their static silhouettes looked toy-like.
            var ships = new List<Transform>
            {
                QuietWatchArtAssetBuilder.InstantiateLod(formationRig, "CommandShip", "Command Ship Resolute",
                    new Vector3(3.5f, 1.4f, -29f), Quaternion.Euler(0f, -25f, 0f), 0.96f),
                QuietWatchArtAssetBuilder.InstantiateLod(formationRig, "EscortSpear", "Port Escort",
                    new Vector3(-13.5f, 5.0f, -43f), Quaternion.Euler(0f, -20f, -2f), 0.78f),
                QuietWatchArtAssetBuilder.InstantiateLod(formationRig, "EscortWing", "Starboard Escort",
                    new Vector3(11.5f, 6.0f, -46f), Quaternion.Euler(0f, -26f, 2f), 0.72f)
            };

            AddBlueBackdrop(vista, "Formation Crescent World", new Vector3(260f, -820f, -1900f), 850f,
                new Vector3(-0.70f, 0.12f, 0.30f), new Vector3(18f, 28f, -24f));
            return Configure(vista, "long-formation", "THE LONG FORMATION", "FORMATION UNDERWAY",
                AuthoredVistaKind.LongFormation, stars, fill, audio, formationRig, ships.ToArray(), new Color(0.34f, 0.58f, 0.84f));
        }

        private static Transform AddBlueBackdrop(GameObject vista, string name, Vector3 center, float radius,
            Vector3 sun, Vector3 rotation)
        {
            var skyRoot = new GameObject(name + " Distant Space").transform;
            skyRoot.SetParent(vista.transform, false);
            skyRoot.position = Vector3.zero;
            skyRoot.localScale = Vector3.one / vista.transform.lossyScale.x;
            var material = MaterialFromShader(name + " Surface", "StarshipCabin/QuietWatchBlueWorld");
            material.SetVector("_DistanceOrigin", new Vector4(-1.6f, 1.1f, -1.42f, 0f));
            material.SetFloat("_DistanceScale", 6371000f / radius);
            material.SetVector("_PlanetSphere", new Vector4(center.x, center.y, center.z, radius));
            material.SetVector("_SunDirection", sun.normalized);
            material.SetTexture("_SurfaceMap", CelestialTexture("QW_BlueSurface.png", true, 2048));
            material.SetTexture("_CloudMap", CelestialTexture("QW_BlueClouds.png", false, 2048));
            EditorUtility.SetDirty(material);
            var world = ExteriorMesh(skyRoot, name, WeatherSphere(name + " Mesh"), material, center, Quaternion.Euler(rotation));
            world.transform.localScale = Vector3.one * radius * 2f;
            world.AddComponent<DistantVistaBounds>();
            var atmosphere = AddAtmosphere(skyRoot, name + " Atmospheric Limb", world, material,
                new Color(0.12f, 0.42f, 1.0f), 0.005f);
            var layers = vista.GetComponent<VistaBackdropLayers>() ?? vista.AddComponent<VistaBackdropLayers>();
            layers.Configure(new[] { world.GetComponent<Renderer>(), atmosphere.GetComponent<Renderer>() });
            return world.transform;
        }

        private static GameObject AddAtmosphere(Transform parent, string name, GameObject planet,
            Material surface, Color color, float height)
        {
            var material = MaterialFromShader(name, "StarshipCabin/QuietWatchAtmosphere");
            foreach (var property in new[] { "_DistanceOrigin", "_SunDirection", "_PlanetSphere", "_RingCenter", "_RingNormal", "_RingRadii" })
                if (surface.HasProperty(property)) material.SetVector(property, surface.GetVector(property));
            material.SetFloat("_DistanceScale", surface.GetFloat("_DistanceScale"));
            material.SetColor("_AtmosphereColor", color);
            material.SetFloat("_AtmosphereHeight", height);
            EditorUtility.SetDirty(material);
            var shell = ExteriorMesh(parent, name, WeatherSphere(name + " Mesh"), material);
            shell.transform.position = planet.transform.position;
            shell.transform.localScale = planet.transform.localScale * (1f + height * 8f);
            shell.AddComponent<DistantVistaBounds>();
            return shell;
        }

        private static Texture2D CelestialTexture(string name, bool srgb, int resolution)
        {
            var path = "Assets/Art/QuietWatch/Textures/" + name;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing authored celestial texture: " + path);
            importer.sRGBTexture = srgb;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = resolution;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = resolution, format = TextureImporterFormat.ASTC_6x6
            });
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static GameObject NewVistaRoot(string name)
        {
            var root = new GameObject(name);
            GameObjectUtility.SetStaticEditorFlags(root, 0);
            return root;
        }

        private static AuthoredVista Configure(
            GameObject root, string id, string title, string subtitle, AuthoredVistaKind kind,
            StarWindowSurface stars, Light fill, AmbientAudioController audio,
            Transform slowTurn, Transform[] travellers, Color fillColor)
        {
            var component = root.AddComponent<AuthoredVista>();
            component.Configure(id, title, subtitle, kind, stars, fill, audio, slowTurn, travellers, fillColor);
            root.SetActive(false);
            return component;
        }

        private static GameObject Sphere(Transform parent, string name, Material material, Vector3 position, float diameter)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = position;
            sphere.transform.localScale = Vector3.one * diameter;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(sphere, 0);
            return sphere;
        }

        private static GameObject ExteriorMesh(
            Transform parent, string name, Mesh mesh, Material material,
            Vector3 position = default, Quaternion rotation = default)
        {
            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }
            var go = QuartersSceneSetup.MeshObject(parent, name, mesh, material, position, rotation);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = Vector3.one;
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            return go;
        }

        private static void ConfigureHarbourRoute(
            Transform traveller, Vector3[] points, float livingDuration, float quietDuration,
            float phaseOffset, float clearanceRadius, float bankDegrees,
            bool availableInQuiet, bool graceRoute)
        {
            var route = traveller.gameObject.AddComponent<HarbourTrafficRoute>();
            route.Configure(points, livingDuration, quietDuration, phaseOffset,
                clearanceRadius, bankDegrees, availableInQuiet, graceRoute);
            if (points.Length > 0)
            {
                traveller.localPosition = points[0];
            }
        }

        private static Vector3[] StationRoute(Transform vista, Transform station, params Vector3[] stationPoints)
        {
            var result = new Vector3[stationPoints.Length];
            for (var i = 0; i < stationPoints.Length; i++)
            {
                result[i] = vista.InverseTransformPoint(station.TransformPoint(stationPoints[i]));
            }
            return result;
        }

        private static void BuildHarbourClearanceVolumes(Transform vista, Transform station)
        {
            var scale = Mathf.Max(station.lossyScale.x, Mathf.Max(station.lossyScale.y, station.lossyScale.z));

            // Axial core, observation drums and central machinery.
            for (var z = -10f; z <= 10f; z += 4f)
            {
                AddClearance(vista, station, $"Axial core {z:0}", new Vector3(0f, 0f, z), 5.2f * scale);
            }

            // The inhabited torus is represented as overlapping conservative
            // spheres. The open centre remains genuinely open, while no route
            // can accidentally cut through the ring structure.
            for (var i = 0; i < 24; i++)
            {
                var angle = i * Mathf.PI * 2f / 24f;
                AddClearance(vista, station, $"Inhabited torus {i:00}",
                    new Vector3(Mathf.Cos(angle) * 17.4f, Mathf.Sin(angle) * 17.4f, 0f),
                    4.35f * scale);
            }

            // Causeways and docking piers need their own envelopes because
            // they extend well beyond the torus silhouette.
            foreach (var side in new[] { -1f, 1f })
            {
                for (var i = 0; i <= 5; i++)
                {
                    var t = i / 5f;
                    AddClearance(vista, station, $"Docking causeway {side:+0;-0} {i}",
                        Vector3.Lerp(new Vector3(side * 8f, 8.5f, -1.2f),
                            new Vector3(side * 27f, 10f, -3f), t), 2.0f * scale);
                }
                AddClearance(vista, station, $"Docking pier {side:+0;-0}",
                    new Vector3(side * 25.7f, 9.9f, -3f), 4.4f * scale);
            }
        }

        private static void AddClearance(
            Transform vista, Transform station, string label, Vector3 stationLocalPosition, float radius)
        {
            var marker = new GameObject("Clearance - " + label);
            marker.transform.SetParent(vista, false);
            marker.transform.position = station.TransformPoint(stationLocalPosition);
            marker.hideFlags = HideFlags.HideInHierarchy;
            marker.AddComponent<HarbourClearanceVolume>().Configure(label, radius);
            GameObjectUtility.SetStaticEditorFlags(marker, 0);
        }

        private static void BuildHarbourHabitation(Transform station)
        {
            var cyan = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Habitat Cyan", new Color(0.025f, 0.12f, 0.17f),
                new Color(0.10f, 0.72f, 1.0f), 5.4f);
            var amber = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Guidance Amber", new Color(0.19f, 0.065f, 0.018f),
                new Color(1.0f, 0.31f, 0.055f), 6.2f);

            var residenceMaterial = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Residence Windows", new Color(0.12f,0.09f,0.06f),new Color(1f,0.74f,0.43f),1.4f);
            var residences = new MeshDraft();
            for (var deck = 0; deck < 4; deck++)
            {
                for (var bay = 0; bay < 180; bay++)
                {
                    if ((bay * 17 + deck * 11) % 13 < 4) continue;
                    var angle = (-140f + bay * 280f / 179f) * Mathf.Deg2Rad;
                    var radial = new Vector3(Mathf.Cos(angle),-Mathf.Sin(angle),0f);
                    var tangent = new Vector3(-Mathf.Sin(angle),-Mathf.Cos(angle),0f);
                    var center = radial * (17.6f + deck * 0.24f) + Vector3.forward * 2.42f;
                    residences.AddQuadOriented(center-tangent*0.045f-radial*0.029f,
                        center+tangent*0.045f-radial*0.029f,center+tangent*0.045f+radial*0.029f,
                        center-tangent*0.045f+radial*0.029f,Vector3.forward);
                }
            }
            ExteriorMesh(station,"Occupied residential decks",residences.ToMesh("Harbour Room-sized Windows"),residenceMaterial);

            var habitat = new MeshDraft();
            var guidance = new MeshDraft();
            for (var i = 0; i < 54; i++)
            {
                var angleDegrees = -148f + i * (300f / 53f);
                var angle = angleDegrees * Mathf.Deg2Rad;
                var radius = i % 3 == 0 ? 18.75f : 16.25f;
                var z = i % 2 == 0 ? 2.35f : 1.85f;
                // Blender's +Z becomes Unity -Y at the imported child root.
                var position = new Vector3(Mathf.Cos(angle) * radius, -Mathf.Sin(angle) * radius, z);
                QuartersMeshes.AppendChamferedBox(habitat, position,
                    new Vector3(0.44f + (i % 4) * 0.06f, 0.16f, 0.10f), 0.025f,
                    Quaternion.Euler(0f, 0f, -angleDegrees - 90f));
            }

            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 11; i++)
                {
                    var x = side * (10f + i * 2.05f);
                    var pulseSize = i % 5 == 0 ? 0.28f : 0.16f;
                    QuartersMeshes.AppendChamferedBox(guidance,
                        new Vector3(x, 10.35f, -2.2f),
                        new Vector3(pulseSize, pulseSize, 0.14f), 0.025f);
                }
            }

            // A readable luminous berth marks the cutter as docked before the
            // grace-note departure. It is infrastructure, not a floating prop.
            QuartersMeshes.AppendChamferedBox(guidance, new Vector3(-32.8f, 7.2f, -2.9f),
                new Vector3(0.18f, 5.8f, 0.20f), 0.03f);
            QuartersMeshes.AppendChamferedBox(guidance, new Vector3(-32.8f, 12.8f, -2.9f),
                new Vector3(0.18f, 5.8f, 0.20f), 0.03f);
            QuartersMeshes.AppendChamferedBox(guidance, new Vector3(-32.8f, 10.0f, -2.9f),
                new Vector3(0.18f, 0.18f, 5.0f), 0.03f);

            ExteriorMesh(station, "Inhabited Window Lattice", habitat.ToMesh("Harbour Inhabited Window Lattice"), cyan);
            ExteriorMesh(station, "Docking Guidance Lights", guidance.ToMesh("Harbour Docking Guidance Lights"), amber);
        }

        internal static Mesh WeatherSphere(string name)
        {
            const int longitude = 128, latitude = 64;
            var vertices = new Vector3[(longitude + 1) * (latitude + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new List<int>(longitude * latitude * 6);
            for (var y = 0; y <= latitude; y++)
            {
                var angle = Mathf.PI * y / latitude;
                for (var x = 0; x <= longitude; x++)
                {
                    var azimuth = 2f * Mathf.PI * x / longitude;
                    var index = y * (longitude + 1) + x;
                    normals[index] = new Vector3(Mathf.Sin(angle) * Mathf.Cos(azimuth),
                        Mathf.Cos(angle), Mathf.Sin(angle) * Mathf.Sin(azimuth));
                    vertices[index] = normals[index] * 0.5f;
                    if (y == latitude || x == longitude) continue;
                    var next = index + longitude + 1;
                    // Outward winding, with non-degenerate pole triangles.
                    if (y > 0) triangles.AddRange(new[] { index, index + 1, next });
                    if (y < latitude - 1) triangles.AddRange(new[] { index + 1, next + 1, next });
                }
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Annulus(string name, float outerRadius, float innerRadius, int segments)
        {
            // One double-sided sheet: a closed transparent slab blended the
            // front and back surfaces twice at almost identical depths.
            var draft = new MeshDraft();
            for (var i = 0; i < segments; i++)
            {
                var a0 = i * Mathf.PI * 2f / segments;
                var a1 = (i + 1) * Mathf.PI * 2f / segments;
                var d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                var d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                draft.AddQuadOriented(d0 * innerRadius, d1 * innerRadius,
                    d1 * outerRadius, d0 * outerRadius, Vector3.forward);
            }
            return draft.ToMesh(name);
        }

        private enum ShipKind { Spear, Wing }

        private static Transform BuildShip(
            Transform parent, string name, ShipKind kind, Material hull, Material engine,
            Vector3 position, Quaternion rotation, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = rotation;
            root.localScale = Vector3.one * scale;
            GameObjectUtility.SetStaticEditorFlags(root.gameObject, 0);

            var body = kind == ShipKind.Spear ? SpearShipMesh(name + " Body") : WingShipMesh(name + " Body");
            ExteriorMesh(root, "Authored Hull", body, hull);

            var engineDraft = new MeshDraft();
            if (kind == ShipKind.Spear)
            {
                QuartersMeshes.AppendChamferedBox(engineDraft, new Vector3(-0.42f, 0f, 2.15f), new Vector3(0.34f, 0.26f, 0.10f), 0.07f);
                QuartersMeshes.AppendChamferedBox(engineDraft, new Vector3(0.42f, 0f, 2.15f), new Vector3(0.34f, 0.26f, 0.10f), 0.07f);
            }
            else
            {
                QuartersMeshes.AppendChamferedBox(engineDraft, new Vector3(-1.15f, -0.05f, 1.52f), new Vector3(0.46f, 0.24f, 0.12f), 0.07f);
                QuartersMeshes.AppendChamferedBox(engineDraft, new Vector3(1.15f, -0.05f, 1.52f), new Vector3(0.46f, 0.24f, 0.12f), 0.07f);
            }
            ExteriorMesh(root, "Engine Light Grammar", engineDraft.ToMesh(name + " Engines"), engine);
            return root;
        }

        private static Mesh SpearShipMesh(string name)
        {
            var d = new MeshDraft();
            AddWedge(d, new Vector3(0f, 0f, -2.8f), new Vector3(-0.72f, -0.28f, 1.8f), new Vector3(0.72f, 0.28f, 1.8f));
            QuartersMeshes.AppendChamferedBox(d, new Vector3(0f, 0.24f, 0.45f), new Vector3(0.82f, 0.34f, 1.55f), 0.12f);
            QuartersMeshes.AppendChamferedBox(d, new Vector3(-1.05f, -0.05f, 0.85f), new Vector3(1.6f, 0.12f, 0.75f), 0.05f, Quaternion.Euler(0f, -12f, 0f));
            QuartersMeshes.AppendChamferedBox(d, new Vector3(1.05f, -0.05f, 0.85f), new Vector3(1.6f, 0.12f, 0.75f), 0.05f, Quaternion.Euler(0f, 12f, 0f));
            return d.ToMesh(name);
        }

        private static Mesh WingShipMesh(string name)
        {
            var d = new MeshDraft();
            AddWedge(d, new Vector3(0f, 0f, -2.0f), new Vector3(-1.05f, -0.34f, 1.45f), new Vector3(1.05f, 0.34f, 1.45f));
            QuartersMeshes.AppendChamferedBox(d, new Vector3(-1.45f, -0.02f, 0.25f), new Vector3(2.1f, 0.16f, 1.35f), 0.06f, Quaternion.Euler(0f, -18f, -2f));
            QuartersMeshes.AppendChamferedBox(d, new Vector3(1.45f, -0.02f, 0.25f), new Vector3(2.1f, 0.16f, 1.35f), 0.06f, Quaternion.Euler(0f, 18f, 2f));
            QuartersMeshes.AppendChamferedBox(d, new Vector3(0f, 0.42f, 0.25f), new Vector3(0.64f, 0.46f, 1.28f), 0.14f);
            return d.ToMesh(name);
        }

        private static void AddWedge(MeshDraft d, Vector3 nose, Vector3 min, Vector3 max)
        {
            var bl = new Vector3(min.x, min.y, max.z);
            var br = new Vector3(max.x, min.y, max.z);
            var tr = new Vector3(max.x, max.y, max.z);
            var tl = new Vector3(min.x, max.y, max.z);
            d.AddQuadOriented(bl, br, tr, tl, Vector3.forward);
            d.AddTriangle(nose, br, bl);
            d.AddTriangle(nose, tr, br);
            d.AddTriangle(nose, tl, tr);
            d.AddTriangle(nose, bl, tl);
        }

        private static Material MaterialFromShader(string name, string shaderName)
        {
            var path = $"Assets/Materials/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader not found: {shaderName}");
            }
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.shader != shader)
                {
                    existing.shader = shader;
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }
            var material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material TransparentEmission(string name, Color color, Color emission, float intensity)
        {
            var material = QuartersSceneSetup.CreateEmissiveMaterial(name, color, emission, intensity);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}

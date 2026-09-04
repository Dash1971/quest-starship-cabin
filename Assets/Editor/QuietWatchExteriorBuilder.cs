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

            return Configure(vista, "harbour", "HARBOUR OF TEN THOUSAND LIGHTS", "ORBITAL SAFE HARBOUR",
                AuthoredVistaKind.Harbour, stars, fill, audio, station, travellers.ToArray(), new Color(0.32f, 0.66f, 0.92f));
        }

        public static AuthoredVista BuildBlueMorning(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 3 - Blue Morning");
            var worldMaterial = MaterialFromShader("Quiet Watch Blue World", "StarshipCabin/QuietWatchBlueWorld");
            worldMaterial.SetColor("_OceanColor", new Color(0.008f, 0.085f, 0.24f));
            worldMaterial.SetColor("_LandColor", new Color(0.075f, 0.27f, 0.15f));
            worldMaterial.SetColor("_CloudColor", new Color(0.92f, 0.97f, 1.0f));
            worldMaterial.SetColor("_AtmosphereColor", new Color(0.08f, 0.42f, 1.0f));
            worldMaterial.SetColor("_SunsetColor", new Color(1.0f, 0.25f, 0.045f));
            worldMaterial.SetVector("_SunDirection", new Vector4(-0.72f, 0.18f, 0.67f, 0f));
            EditorUtility.SetDirty(worldMaterial);
            var world = Sphere(vista.transform, "Blue World", worldMaterial, new Vector3(9f, -42f, -78f), 110f);
            world.transform.rotation = Quaternion.Euler(2f, -18f, -8f);

            var sunrise = QuartersSceneSetup.CreateEmissiveMaterial(
                "Blue Morning Sun", new Color(0.8f, 0.45f, 0.12f), new Color(1.0f, 0.58f, 0.25f), 5.0f);
            Sphere(vista.transform, "Dawn Sun", sunrise, new Vector3(-46f, 25f, -106f), 6.5f);

            return Configure(vista, "blue-morning", "BLUE MORNING", "DAWN ABOVE A LIVING WORLD",
                AuthoredVistaKind.BlueMorning, stars, fill, audio, world.transform, Array.Empty<Transform>(), new Color(1.0f, 0.54f, 0.30f));
        }

        public static AuthoredVista BuildGreatWeather(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 4 - The Great Weather");
            var planetMaterial = MaterialFromShader("Quiet Watch Great Weather", "StarshipCabin/QuietWatchGasGiant");
            planetMaterial.SetColor("_PaleBand", new Color(0.94f, 0.69f, 0.39f));
            planetMaterial.SetColor("_DarkBand", new Color(0.22f, 0.065f, 0.045f));
            planetMaterial.SetColor("_StormColor", new Color(1.0f, 0.28f, 0.075f));
            planetMaterial.SetVector("_SunDirection", new Vector4(-0.62f, 0.30f, 0.72f, 0f));
            EditorUtility.SetDirty(planetMaterial);
            var planet = Sphere(vista.transform, "Ringed Giant", planetMaterial, new Vector3(8f, 6f, -76f), 58f);
            planet.transform.rotation = Quaternion.Euler(0f, -22f, -8f);

            var ringMaterial = MaterialFromShader("Great Weather Rings Authored", "StarshipCabin/QuietWatchRings");
            ringMaterial.SetColor("_LightColor", new Color(0.82f, 0.61f, 0.38f));
            ringMaterial.SetColor("_DarkColor", new Color(0.10f, 0.052f, 0.045f));
            EditorUtility.SetDirty(ringMaterial);
            var rings = ExteriorMesh(vista.transform, "Planetary Rings", Annulus("Great Weather Ring System", 44f, 31.5f, 0.16f, 96), ringMaterial,
                new Vector3(8f, 6f, -76f), Quaternion.Euler(63f, 8f, -14f));

            var moonMaterial = MaterialFromShader(
                "Great Weather Moon", "StarshipCabin/QuietWatchMoon");
            // The giant's visual sphere extends much closer than its origin;
            // place the moon in front of that surface so the transit survives
            // depth testing instead of disappearing inside the planet mesh.
            var shadowMoon = Sphere(vista.transform, "Moon in Ring Shadow", moonMaterial, new Vector3(0f, 3.5f, -16f), 2.5f);
            var farMoon = Sphere(vista.transform, "Far Moon", moonMaterial, new Vector3(34f, 25f, -96f), 3.0f);

            return Configure(vista, "great-weather", "THE GREAT WEATHER", "RING SHADOW AND STORMS",
                AuthoredVistaKind.GreatWeather, stars, fill, audio, planet.transform,
                new[] { shadowMoon.transform, farMoon.transform }, new Color(0.92f, 0.50f, 0.26f));
        }

        public static AuthoredVista BuildLongFormation(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 5 - The Long Formation");
            QuietWatchArtAssetBuilder.AddExteriorSun(
                vista.transform, "Formation Distant Sun", Quaternion.Euler(26f, -44f, -12f),
                new Color(0.78f, 0.88f, 1.0f), 1.45f, true);

            var formationRig = new GameObject("Formation Flight Rig").transform;
            formationRig.SetParent(vista.transform, false);
            GameObjectUtility.SetStaticEditorFlags(formationRig.gameObject, 0);

            // Three substantial vessels read as a deliberate unit. The three
            // tiny far-field ships from the blockout were cut after headset
            // review because their static silhouettes looked toy-like.
            var ships = new List<Transform>
            {
                QuietWatchArtAssetBuilder.InstantiateLod(formationRig, "CommandShip", "Command Ship Resolute",
                    new Vector3(3.5f, 1.4f, -29f), Quaternion.Euler(0f, -3f, 0f), 0.96f),
                QuietWatchArtAssetBuilder.InstantiateLod(formationRig, "EscortSpear", "Port Escort",
                    new Vector3(-13.5f, 5.0f, -43f), Quaternion.Euler(0f, 5f, -2f), 0.78f),
                QuietWatchArtAssetBuilder.InstantiateLod(formationRig, "EscortWing", "Starboard Escort",
                    new Vector3(11.5f, 6.0f, -46f), Quaternion.Euler(0f, -5f, 2f), 0.72f)
            };

            return Configure(vista, "long-formation", "THE LONG FORMATION", "FORMATION UNDERWAY",
                AuthoredVistaKind.LongFormation, stars, fill, audio, formationRig, ships.ToArray(), new Color(0.34f, 0.58f, 0.84f));
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

        private static Mesh Annulus(string name, float outerRadius, float innerRadius, float depth, int segments)
        {
            var draft = new MeshDraft();
            var hz = depth * 0.5f;
            for (var i = 0; i < segments; i++)
            {
                var a0 = i * Mathf.PI * 2f / segments;
                var a1 = (i + 1) * Mathf.PI * 2f / segments;
                var o0f = new Vector3(Mathf.Cos(a0) * outerRadius, Mathf.Sin(a0) * outerRadius, hz);
                var o1f = new Vector3(Mathf.Cos(a1) * outerRadius, Mathf.Sin(a1) * outerRadius, hz);
                var i0f = new Vector3(Mathf.Cos(a0) * innerRadius, Mathf.Sin(a0) * innerRadius, hz);
                var i1f = new Vector3(Mathf.Cos(a1) * innerRadius, Mathf.Sin(a1) * innerRadius, hz);
                var o0b = new Vector3(o0f.x, o0f.y, -hz);
                var o1b = new Vector3(o1f.x, o1f.y, -hz);
                var i0b = new Vector3(i0f.x, i0f.y, -hz);
                var i1b = new Vector3(i1f.x, i1f.y, -hz);
                draft.AddQuadOriented(i0f, i1f, o1f, o0f, Vector3.forward);
                draft.AddQuadOriented(i0b, o0b, o1b, i1b, Vector3.back);
                draft.AddQuadOriented(o0f, o1f, o1b, o0b, (o0f + o1f).normalized);
                draft.AddQuadOriented(i1f, i0f, i0b, i1b, -(i0f + i1f).normalized);
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

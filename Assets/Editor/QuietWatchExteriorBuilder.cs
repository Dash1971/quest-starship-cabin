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
                new Color(0.74f, 0.84f, 1.0f), 1.35f, true);

            var travellers = new List<Transform>();
            travellers.Add(QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "EscortWing", "Inbound Customs Cutter",
                new Vector3(9.8f, 0.4f, -29f), Quaternion.Euler(3f, -16f, 2f), 0.38f));
            travellers.Add(QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "EscortSpear", "Docking Tender",
                new Vector3(-14f, 8.5f, -50f), Quaternion.Euler(-4f, 12f, -2f), 0.34f, false));
            travellers.Add(QuietWatchArtAssetBuilder.InstantiateLod(
                vista.transform, "EscortSpear", "Distant Shuttle",
                new Vector3(12f, 11f, -66f), Quaternion.Euler(0f, -20f, 0f), 0.24f, false));

            return Configure(vista, "harbour", "HARBOUR OF TEN THOUSAND LIGHTS", "ORBITAL SAFE HARBOUR",
                AuthoredVistaKind.Harbour, stars, fill, audio, station, travellers.ToArray(), new Color(0.32f, 0.66f, 0.92f));
        }

        public static AuthoredVista BuildBlueMorning(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 3 - Blue Morning");
            var worldMaterial = MaterialFromShader("Quiet Watch Blue World", "StarshipCabin/QuietWatchBlueWorld");
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
            var planet = Sphere(vista.transform, "Ringed Giant", planetMaterial, new Vector3(8f, 6f, -76f), 58f);
            planet.transform.rotation = Quaternion.Euler(0f, -22f, -8f);

            var ringMaterial = MaterialFromShader("Great Weather Rings Authored", "StarshipCabin/QuietWatchRings");
            var rings = ExteriorMesh(vista.transform, "Planetary Rings", Annulus("Great Weather Ring System", 44f, 31.5f, 0.16f, 96), ringMaterial,
                new Vector3(8f, 6f, -76f), Quaternion.Euler(63f, 8f, -14f));

            var moonMaterial = QuartersSceneSetup.CreateMaterial("Great Weather Moon", new Color(0.34f, 0.31f, 0.29f));
            Sphere(vista.transform, "Moon in Ring Shadow", moonMaterial, new Vector3(-30f, 18f, -68f), 5.2f);
            Sphere(vista.transform, "Far Moon", moonMaterial, new Vector3(38f, 25f, -101f), 2.8f);

            return Configure(vista, "great-weather", "THE GREAT WEATHER", "RING SHADOW AND STORMS",
                AuthoredVistaKind.GreatWeather, stars, fill, audio, planet.transform, new[] { rings.transform }, new Color(0.92f, 0.50f, 0.26f));
        }

        public static AuthoredVista BuildLongFormation(
            StarWindowSurface stars, Light fill, AmbientAudioController audio)
        {
            var vista = NewVistaRoot("Vista 5 - The Long Formation");
            QuietWatchArtAssetBuilder.AddExteriorSun(
                vista.transform, "Formation Distant Sun", Quaternion.Euler(26f, -44f, -12f),
                new Color(0.78f, 0.88f, 1.0f), 1.45f, true);

            var ships = new List<Transform>
            {
                QuietWatchArtAssetBuilder.InstantiateLod(vista.transform, "CommandShip", "Command Ship Resolute",
                    new Vector3(3.5f, 1.4f, -29f), Quaternion.Euler(0f, -3f, 0f), 0.96f),
                QuietWatchArtAssetBuilder.InstantiateLod(vista.transform, "EscortSpear", "Port Escort Near",
                    new Vector3(-14f, 5.4f, -44f), Quaternion.Euler(0f, 5f, -2f), 0.68f),
                QuietWatchArtAssetBuilder.InstantiateLod(vista.transform, "EscortSpear", "Starboard Escort Near",
                    new Vector3(7.5f, 6.2f, -47f), Quaternion.Euler(0f, -5f, 2f), 0.68f),
                QuietWatchArtAssetBuilder.InstantiateLod(vista.transform, "EscortWing", "Port Escort Far",
                    new Vector3(-16f, 10.5f, -61f), Quaternion.Euler(0f, 7f, -3f), 0.42f, false),
                QuietWatchArtAssetBuilder.InstantiateLod(vista.transform, "EscortWing", "Starboard Escort Far",
                    new Vector3(17f, 11.2f, -64f), Quaternion.Euler(0f, -7f, 3f), 0.41f, false),
                QuietWatchArtAssetBuilder.InstantiateLod(vista.transform, "EscortSpear", "High Scout",
                    new Vector3(0f, 15f, -72f), Quaternion.identity, 0.30f, false)
            };

            return Configure(vista, "long-formation", "THE LONG FORMATION", "THE FLEET HOLDS STATION",
                AuthoredVistaKind.LongFormation, stars, fill, audio, ships[0], ships.ToArray(), new Color(0.34f, 0.58f, 0.84f));
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
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader not found: {shaderName}");
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

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
            var station = new GameObject("Kilometre Station Composition").transform;
            station.SetParent(vista.transform, false);
            station.localPosition = new Vector3(2.0f, 4.2f, -39f);
            station.localRotation = Quaternion.Euler(8f, -9f, -7f);

            var hull = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Hull", new Color(0.075f, 0.090f, 0.110f), new Color(0.08f, 0.13f, 0.19f), 0.72f);
            hull.SetFloat("_Metallic", 0.72f);
            hull.SetFloat("_Smoothness", 0.48f);
            var armour = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Armour", new Color(0.22f, 0.235f, 0.245f), new Color(0.18f, 0.23f, 0.28f), 0.58f);
            armour.SetFloat("_Metallic", 0.48f);
            var windows = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Windows", new Color(0.04f, 0.12f, 0.18f), new Color(0.32f, 0.78f, 1.0f), 3.2f);
            var amber = QuartersSceneSetup.CreateEmissiveMaterial(
                "Harbour Amber Beacons", new Color(0.15f, 0.07f, 0.02f), new Color(1.0f, 0.44f, 0.12f), 3.5f);

            ExteriorMesh(station, "Outer Habitat Ring", Annulus("Harbour Outer Ring", 10.8f, 9.0f, 0.72f, 64), hull);
            ExteriorMesh(station, "Inner Light Ring", Annulus("Harbour Inner Light Ring", 8.85f, 8.45f, 0.80f, 64), windows);
            ExteriorMesh(station, "Rear Habitat Ring", Annulus("Harbour Rear Ring", 7.3f, 6.2f, 0.62f, 56), armour,
                new Vector3(0f, 0f, 3.0f), Quaternion.Euler(0f, 0f, 18f));

            var superstructure = new MeshDraft();
            QuartersMeshes.AppendChamferedBox(superstructure, Vector3.zero, new Vector3(2.2f, 2.2f, 9.5f), 0.34f);
            QuartersMeshes.AppendChamferedBox(superstructure, new Vector3(0f, 0f, -5.6f), new Vector3(4.6f, 1.25f, 2.1f), 0.28f);
            QuartersMeshes.AppendChamferedBox(superstructure, new Vector3(0f, 0f, 5.3f), new Vector3(3.2f, 1.5f, 1.8f), 0.30f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * 45f;
                var direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
                QuartersMeshes.AppendChamferedBox(
                    superstructure,
                    direction * 5.3f,
                    new Vector3(0.48f, 8.4f, 0.55f),
                    0.12f,
                    Quaternion.Euler(0f, 0f, angle));
            }
            ExteriorMesh(station, "Station Spindle and Spokes", superstructure.ToMesh("Harbour Superstructure"), armour);

            var docking = new MeshDraft();
            QuartersMeshes.AppendChamferedBox(docking, new Vector3(-13.8f, -1.8f, 1.4f), new Vector3(9.0f, 0.55f, 0.80f), 0.16f, Quaternion.Euler(0f, 0f, -7f));
            QuartersMeshes.AppendChamferedBox(docking, new Vector3(12.6f, 2.6f, -0.8f), new Vector3(7.6f, 0.52f, 0.78f), 0.15f, Quaternion.Euler(0f, 0f, 9f));
            QuartersMeshes.AppendChamferedBox(docking, new Vector3(3.8f, -11.1f, 0.2f), new Vector3(0.58f, 6.4f, 0.72f), 0.15f, Quaternion.Euler(0f, 0f, -4f));
            ExteriorMesh(station, "Docking Spars", docking.ToMesh("Harbour Docking Spars"), hull);

            var lights = new MeshDraft();
            for (var i = 0; i < 32; i++)
            {
                var angle = i * Mathf.PI * 2f / 32f;
                var p = new Vector3(Mathf.Cos(angle) * 9.75f, Mathf.Sin(angle) * 9.75f, -0.48f);
                QuartersMeshes.AppendChamferedBox(lights, p, new Vector3(0.28f, 0.28f, 0.20f), 0.06f);
            }
            ExteriorMesh(station, "Habitat Light Rhythm", lights.ToMesh("Harbour Habitat Lights"), windows);

            var travellers = new List<Transform>();
            travellers.Add(BuildShip(vista.transform, "Inbound Courier", ShipKind.Spear, hull, windows,
                new Vector3(8.8f, -0.8f, -28f), Quaternion.Euler(4f, -16f, 3f), 0.82f));
            travellers.Add(BuildShip(vista.transform, "Docking Tender", ShipKind.Wing, armour, amber,
                new Vector3(-13f, 8.5f, -48f), Quaternion.Euler(-5f, 12f, -2f), 0.64f));
            travellers.Add(BuildShip(vista.transform, "Distant Shuttle", ShipKind.Spear, hull, windows,
                new Vector3(12f, 10f, -64f), Quaternion.Euler(0f, -20f, 0f), 0.45f));

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
            var hull = QuartersSceneSetup.CreateEmissiveMaterial(
                "Formation Hull", new Color(0.12f, 0.15f, 0.18f), new Color(0.08f, 0.16f, 0.23f), 0.72f);
            hull.SetFloat("_Metallic", 0.62f);
            hull.SetFloat("_Smoothness", 0.44f);
            var paleHull = QuartersSceneSetup.CreateEmissiveMaterial(
                "Formation Pale Armour", new Color(0.34f, 0.37f, 0.39f), new Color(0.22f, 0.29f, 0.34f), 0.72f);
            var engines = QuartersSceneSetup.CreateEmissiveMaterial(
                "Formation Engines", new Color(0.03f, 0.12f, 0.18f), new Color(0.22f, 0.76f, 1.0f), 4.2f);

            var ships = new List<Transform>
            {
                BuildShip(vista.transform, "Command Ship", ShipKind.Wing, paleHull, engines, new Vector3(0f, 2f, -34f), Quaternion.Euler(0f, 0f, 0f), 3.8f),
                BuildShip(vista.transform, "Port Escort Near", ShipKind.Spear, hull, engines, new Vector3(-9f, 5.2f, -43f), Quaternion.Euler(0f, 4f, -2f), 2.35f),
                BuildShip(vista.transform, "Starboard Escort Near", ShipKind.Spear, hull, engines, new Vector3(9.5f, 5.7f, -45f), Quaternion.Euler(0f, -4f, 2f), 2.35f),
                BuildShip(vista.transform, "Port Escort Far", ShipKind.Wing, paleHull, engines, new Vector3(-15f, 10.5f, -61f), Quaternion.Euler(0f, 7f, -3f), 1.65f),
                BuildShip(vista.transform, "Starboard Escort Far", ShipKind.Wing, paleHull, engines, new Vector3(16f, 11.2f, -64f), Quaternion.Euler(0f, -7f, 3f), 1.60f),
                BuildShip(vista.transform, "High Scout", ShipKind.Spear, hull, engines, new Vector3(0f, 15f, -72f), Quaternion.identity, 1.18f)
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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StarshipCabin.EditorTools
{
    /// <summary>
    /// Milestone 2: procedural furnishings for the Crew Quarters V2 scene.
    /// Couch + low table (lounge), raised platform + bed (sleep alcove), desk +
    /// stool, diegetic console pad with the ambience mode strips, entry door
    /// detail, plants and sill dressing. Everything is a beveled ChamferedBox
    /// or a small MeshDraft — no raw cube primitives, no colliders.
    ///
    /// The three mode strips are named exactly "Amber Mode Strip",
    /// "Teal Mode Strip" and "Blue Status Strip" so the existing
    /// CabinExperienceController indicator lookup finds them. They are built
    /// from a shared UNIT mesh and sized via transform.localScale, because
    /// SetIndicator() animates localScale.y with absolute values (0.045/0.075).
    /// They are deliberately NOT static (runtime scale + material changes).
    /// </summary>
    internal static class QuartersFurnishings
    {
        private struct FurnishingMaterials
        {
            public Material Upholstery;
            public Material Linen;
            public Material Wood;
            public Material Green;
            public Material Trim;
            public Material Graphite;
            public Material Plum;
            public Material Deck;
            public Material Cove;
            public Material LampGlow;
            public Material Amber;
            public Material Teal;
            public Material Blue;
        }

        public static void BuildAll(Transform root)
        {
            var mats = CreateFurnishingMaterials();

            var parent = new GameObject("Furnishings").transform;
            parent.SetParent(root);

            BuildLounge(parent, mats);
            BuildSleepAlcove(parent, mats);
            BuildDesk(parent, mats);
            BuildEntryDoor(parent, mats);
            BuildDecor(parent, mats);
            QuartersDecor.Build(parent); // Milestone 6: chess set + library
        }

        private static FurnishingMaterials CreateFurnishingMaterials()
        {
            return new FurnishingMaterials
            {
                // New Milestone 2 materials.
                Upholstery = QuartersSceneSetup.CreateMaterial("Oatmeal Upholstery", new Color(0.812f, 0.776f, 0.706f)),
                Wood = QuartersSceneSetup.CreateMaterial("Cabin Wood", new Color(0.541f, 0.416f, 0.282f)),
                Green = QuartersSceneSetup.CreateMaterial("Plant Green", new Color(0.490f, 0.580f, 0.408f)),
                Amber = QuartersSceneSetup.CreateEmissiveMaterial("Amber Accent", new Color(1.0f, 0.702f, 0.361f), new Color(1.0f, 0.60f, 0.20f), 1.6f),
                Teal = QuartersSceneSetup.CreateEmissiveMaterial("Teal Status", new Color(0.173f, 0.780f, 0.722f), new Color(0.12f, 0.72f, 0.66f), 1.3f),
                Blue = QuartersSceneSetup.CreateEmissiveMaterial("Quiet Blue Status", new Color(0.184f, 0.502f, 0.910f), new Color(0.12f, 0.42f, 0.85f), 1.1f),

                // Reused Milestone 1 materials (loaded by name, not recreated).
                Linen = QuartersSceneSetup.CreateMaterial("Soft Panel White", new Color(0.902f, 0.878f, 0.831f)),
                Trim = QuartersSceneSetup.CreateMaterial("Warm Grey Trim", new Color(0.608f, 0.588f, 0.549f)),
                Graphite = QuartersSceneSetup.CreateMaterial("Graphite Frame", new Color(0.106f, 0.114f, 0.125f)),
                Plum = QuartersSceneSetup.CreateMaterial("Muted Plum Carpet", new Color(0.427f, 0.290f, 0.322f)),
                Deck = QuartersSceneSetup.CreateMaterial("Deck Warm Grey", new Color(0.462f, 0.443f, 0.405f)),
                Cove = QuartersSceneSetup.CreateEmissiveMaterial("Warm White Cove", new Color(1.0f, 0.910f, 0.769f), new Color(1.0f, 0.882f, 0.702f), 2.1f),

                // Milestone 5: dedicated dimmer glow for the desk lamp (the
                // shared cove material at 2.1 was too bright on the desk.
                LampGlow = QuartersSceneSetup.CreateEmissiveMaterial("Desk Lamp Glow", new Color(1.0f, 0.898f, 0.749f), new Color(1.0f, 0.86f, 0.68f), 0.55f)
            };
        }

        // ------------------------------------------------------------------
        // Lounge: couch under the glazing, low table, console pad with strips
        // ------------------------------------------------------------------

        private static void BuildLounge(Transform parent, FurnishingMaterials mats)
        {
            // Couch: back edge at z = -1.95 (clear of the sloped glass — head
            // clearance under the slope there is ~1.7 m), facing into the room.
            const float couchX = -1.6f;

            Box(parent, mats.Trim, "Couch Base", 2.4f, 0.30f, 0.95f, 0.03f,
                new Vector3(couchX, 0.15f, -1.475f));

            for (var i = 0; i < 3; i++)
            {
                var x = couchX + (i - 1) * 0.76f;
                Box(parent, mats.Upholstery, $"Couch Seat Cushion {i + 1}", 0.72f, 0.18f, 0.88f, 0.05f,
                    new Vector3(x, 0.39f, -1.50f));
                Box(parent, mats.Upholstery, $"Couch Back Cushion {i + 1}", 0.72f, 0.42f, 0.16f, 0.05f,
                    new Vector3(x, 0.70f, -1.83f), Quaternion.Euler(-8f, 0f, 0f));
            }

            Box(parent, mats.Trim, "Couch Back Panel", 2.4f, 0.62f, 0.14f, 0.03f,
                new Vector3(couchX, 0.55f, -1.92f), Quaternion.Euler(-8f, 0f, 0f));
            Box(parent, mats.Trim, "Couch Armrest Left", 0.24f, 0.55f, 0.98f, 0.06f,
                new Vector3(couchX - 1.32f, 0.275f, -1.475f));
            Box(parent, mats.Trim, "Couch Armrest Right", 0.24f, 0.55f, 0.98f, 0.06f,
                new Vector3(couchX + 1.32f, 0.275f, -1.475f));

            // Low table on the rug.
            Box(parent, mats.Graphite, "Low Table Plinth", 0.85f, 0.33f, 0.42f, 0.03f,
                new Vector3(couchX, 0.165f, 0.05f));
            var tableTop = Box(parent, mats.Wood, "Low Table Top", 1.25f, 0.06f, 0.70f, 0.02f,
                new Vector3(couchX, 0.36f, 0.05f));

            BuildConsolePad(parent, mats, tableTop.transform.position);
        }

        /// <summary>
        /// Diegetic console pad on the low table, tilted toward the couch,
        /// carrying the three ambience mode strips the controller drives.
        /// </summary>
        private static void BuildConsolePad(Transform parent, FurnishingMaterials mats, Vector3 tableTopCenter)
        {
            var padCenter = tableTopCenter + new Vector3(0.33f, 0.055f, 0.13f);
            var padRotation = Quaternion.Euler(-10f, 0f, 0f); // face tilts toward the couch (-Z)
            var pad = Box(parent, mats.Graphite, "Console Pad", 0.42f, 0.05f, 0.28f, 0.015f,
                padCenter, padRotation);

            // Shared unit mesh; sizes come from localScale so that
            // CabinExperienceController.SetIndicator's absolute y-scale
            // animation (0.045 inactive / 0.075 active) behaves as designed.
            var unit = QuartersMeshes.ChamferedBox("Quarters Indicator Unit", 1f, 1f, 1f, 0.08f);

            Strip(pad.transform, unit, mats.Amber, "Amber Mode Strip", new Vector3(-0.12f, 0.048f, 0f), new Vector3(0.14f, 0.045f, 0.05f));
            Strip(pad.transform, unit, mats.Teal, "Teal Mode Strip", new Vector3(0.03f, 0.048f, 0f), new Vector3(0.10f, 0.045f, 0.05f));
            Strip(pad.transform, unit, mats.Blue, "Blue Status Strip", new Vector3(0.15f, 0.048f, 0f), new Vector3(0.07f, 0.045f, 0.05f));
        }

        private static void Strip(Transform pad, Mesh unit, Material material, string name, Vector3 localPos, Vector3 localScale)
        {
            var strip = QuartersSceneSetup.MeshObject(pad, name, unit, material);
            strip.transform.localPosition = localPos;
            strip.transform.localRotation = Quaternion.identity;
            strip.transform.localScale = localScale;
            // Runtime scale + material animation: must not be static-batched.
            GameObjectUtility.SetStaticEditorFlags(strip, 0);
        }

        // ------------------------------------------------------------------
        // Sleep alcove: raised platform, step, bed with linen
        // ------------------------------------------------------------------

        private static void BuildSleepAlcove(Transform parent, FurnishingMaterials mats)
        {
            // Platform: x 1.05..3.2, z -1.5..1.05, raised 0.25 m.
            Box(parent, mats.Deck, "Sleep Platform", 2.15f, 0.25f, 2.55f, 0.02f,
                new Vector3(2.125f, 0.125f, -0.225f));
            Box(parent, mats.Deck, "Platform Step", 0.35f, 0.125f, 0.90f, 0.02f,
                new Vector3(0.875f, 0.0625f, 0.20f));
            // Step edge glow: subtle safety/way-finding cue.
            Box(parent, mats.Cove, "Platform Edge Strip", 0.03f, 0.02f, 2.55f, 0.006f,
                new Vector3(1.06f, 0.24f, -0.225f));

            // Bed: head toward the alcove window (anchor 3 lies looking up into the pane).
            const float bedX = 2.05f;
            const float bedZ = -0.275f;

            Box(parent, mats.Wood, "Bed Frame", 1.55f, 0.16f, 2.15f, 0.03f,
                new Vector3(bedX, 0.33f, bedZ));
            Box(parent, mats.Linen, "Bed Mattress", 1.45f, 0.18f, 2.05f, 0.06f,
                new Vector3(bedX, 0.50f, bedZ));
            Box(parent, mats.Wood, "Bed Headboard", 1.55f, 0.55f, 0.08f, 0.02f,
                new Vector3(bedX, 0.60f, -1.40f), Quaternion.Euler(-8f, 0f, 0f));

            Box(parent, mats.Upholstery, "Bed Pillow Left", 0.55f, 0.11f, 0.38f, 0.05f,
                new Vector3(bedX - 0.32f, 0.645f, -1.10f), Quaternion.Euler(0f, 4f, 0f));
            Box(parent, mats.Upholstery, "Bed Pillow Right", 0.55f, 0.11f, 0.38f, 0.05f,
                new Vector3(bedX + 0.32f, 0.645f, -1.10f), Quaternion.Euler(0f, -4f, 0f));
            Box(parent, mats.Plum, "Bed Blanket", 1.50f, 0.045f, 0.95f, 0.02f,
                new Vector3(bedX, 0.610f, 0.28f));

            // Reading strip above the headboard (matches the alcove glow light).
            Box(parent, mats.Amber, "Alcove Reading Strip", 0.50f, 0.03f, 0.05f, 0.008f,
                new Vector3(bedX, 0.90f, -1.44f), Quaternion.Euler(-8f, 0f, 0f));
        }

        // ------------------------------------------------------------------
        // Desk zone against the left wall
        // ------------------------------------------------------------------

        private static void BuildDesk(Transform parent, FurnishingMaterials mats)
        {
            Box(parent, mats.Wood, "Desk Top", 0.72f, 0.05f, 1.15f, 0.02f,
                new Vector3(-2.82f, 0.72f, 2.0f));
            Box(parent, mats.Wood, "Desk Side Panel Near", 0.66f, 0.68f, 0.05f, 0.02f,
                new Vector3(-2.82f, 0.34f, 1.48f));
            Box(parent, mats.Wood, "Desk Side Panel Far", 0.66f, 0.68f, 0.05f, 0.02f,
                new Vector3(-2.82f, 0.34f, 2.52f));
            Box(parent, mats.Graphite, "Desk Modesty Panel", 0.05f, 0.50f, 1.00f, 0.01f,
                new Vector3(-3.10f, 0.45f, 2.0f));

            Box(parent, mats.Graphite, "Desk Stool Base", 0.38f, 0.36f, 0.38f, 0.04f,
                new Vector3(-2.2f, 0.18f, 2.0f));
            Box(parent, mats.Upholstery, "Desk Stool Cushion", 0.40f, 0.10f, 0.40f, 0.04f,
                new Vector3(-2.2f, 0.41f, 2.0f));

            // Small desk lamp: graphite stem, warm emissive head.
            Box(parent, mats.Graphite, "Desk Lamp Stem", 0.03f, 0.34f, 0.03f, 0.008f,
                new Vector3(-3.02f, 0.915f, 2.38f));
            Box(parent, mats.LampGlow, "Desk Lamp Head", 0.16f, 0.05f, 0.10f, 0.015f,
                new Vector3(-2.96f, 1.09f, 2.34f), Quaternion.Euler(0f, 25f, -18f));
        }

        // ------------------------------------------------------------------
        // Entry door detail on the inner wall
        // ------------------------------------------------------------------

        private static void BuildEntryDoor(Transform parent, FurnishingMaterials mats)
        {
            // Recessed-looking door slab proud of the wall, graphite reveal frame.
            Box(parent, mats.Trim, "Entry Door Slab", 1.00f, 2.05f, 0.06f, 0.02f,
                new Vector3(0.6f, 1.025f, 2.56f));
            Box(parent, mats.Graphite, "Entry Door Frame Left", 0.10f, 2.15f, 0.09f, 0.02f,
                new Vector3(0.05f, 1.075f, 2.55f));
            Box(parent, mats.Graphite, "Entry Door Frame Right", 0.10f, 2.15f, 0.09f, 0.02f,
                new Vector3(1.15f, 1.075f, 2.55f));
            Box(parent, mats.Graphite, "Entry Door Header", 1.30f, 0.10f, 0.09f, 0.02f,
                new Vector3(0.6f, 2.15f, 2.55f));
            // Tiny door status dot.
            Box(parent, mats.Teal, "Entry Door Status Dot", 0.05f, 0.05f, 0.02f,  0.008f,
                new Vector3(1.08f, 1.35f, 2.52f));
        }

        // ------------------------------------------------------------------
        // Decor: plants, sill books
        // ------------------------------------------------------------------

        private static void BuildDecor(Transform parent, FurnishingMaterials mats)
        {
            // Floor plants: right edge of the lounge glazing, and by the desk corner.
            BuildPlant(parent, mats, "Lounge Plant", new Vector3(0.45f, 0f, -1.90f), 0.26f, 0.55f, seed: 11);
            BuildPlant(parent, mats, "Desk Plant", new Vector3(-2.90f, 0f, 1.10f), 0.24f, 0.48f, seed: 29);

            // Milestone 6: the tabletop plant made way for the chessboard
            // (see QuartersDecor). The two floor plants carry the greenery.

            // Books on the sill near the alcove.
            Box(parent, mats.Wood, "Sill Book 1", 0.045f, 0.17f, 0.12f, 0.006f,
                new Vector3(1.90f, 0.835f, -2.44f));
            Box(parent, mats.Plum, "Sill Book 2", 0.040f, 0.155f, 0.115f, 0.006f,
                new Vector3(1.955f, 0.828f, -2.44f));
            Box(parent, mats.Trim, "Sill Book 3", 0.038f, 0.165f, 0.11f, 0.006f,
                new Vector3(2.02f, 0.828f, -2.44f), Quaternion.Euler(0f, 0f, -9f));
        }

        private static void BuildPlant(
            Transform parent, FurnishingMaterials mats, string name, Vector3 floorPos, float potSize, float foliageHeight, int seed)
        {
            var potHeight = potSize * 0.9f;
            Box(parent, mats.Wood, $"{name} Pot", potSize, potHeight, potSize, potSize * 0.22f,
                floorPos + new Vector3(0f, potHeight * 0.5f, 0f));

            var draft = new MeshDraft();
            var top = floorPos + new Vector3(0f, potHeight * 0.92f, 0f);
            var stems = 6;

            for (var i = 0; i < stems; i++)
            {
                var yaw = (i / (float)stems) * Mathf.PI * 2f + Hash01(seed + i * 3) * 0.8f;
                var tilt = Mathf.Lerp(0.30f, 0.65f, Hash01(seed + i * 7 + 1));
                var length = foliageHeight * Mathf.Lerp(0.65f, 1.0f, Hash01(seed + i * 13 + 2));

                var direction = new Vector3(
                    Mathf.Cos(yaw) * Mathf.Sin(tilt),
                    Mathf.Cos(tilt),
                    Mathf.Sin(yaw) * Mathf.Sin(tilt)).normalized;
                var tip = top + direction * length;

                AddStem(draft, top, tip, potSize * 0.055f);
                AddLeaf(draft, tip, direction, length * 0.55f, length * 0.22f);

                // A mid-stem leaf on every other stem.
                if (i % 2 == 0)
                {
                    var mid = Vector3.Lerp(top, tip, 0.55f);
                    AddLeaf(draft, mid, Quaternion.AngleAxis(50f, Vector3.up) * direction, length * 0.4f, length * 0.17f);
                }
            }

            QuartersSceneSetup.MeshObject(parent, $"{name} Foliage", draft.ToMesh($"Quarters {name} Foliage"), mats.Green);
        }

        /// <summary>Stem as two crossed quads — cheap, and reads well flat-shaded.</summary>
        private static void AddStem(MeshDraft draft, Vector3 from, Vector3 to, float width)
        {
            var axis = (to - from).normalized;
            var side1 = Vector3.Cross(axis, Vector3.up).sqrMagnitude < 0.001f
                ? Vector3.right
                : Vector3.Cross(axis, Vector3.up).normalized;
            var side2 = Vector3.Cross(axis, side1).normalized;

            foreach (var side in new[] { side1, side2 })
            {
                var w = side * (width * 0.5f);
                draft.AddQuad(from - w, from + w, to + w, to - w);
                draft.AddQuad(to - w, to + w, from + w, from - w); // back face
            }
        }

        /// <summary>Leaf as a double-sided convex hexagon perpendicular-ish to the stem tip.</summary>
        private static void AddLeaf(MeshDraft draft, Vector3 baseTip, Vector3 stemDir, float length, float width)
        {
            var forward = stemDir.normalized;
            var right = Vector3.Cross(forward, Vector3.up).sqrMagnitude < 0.001f
                ? Vector3.right
                : Vector3.Cross(forward, Vector3.up).normalized;
            var normal = Vector3.Cross(forward, right).normalized;

            var points = new List<Vector3>
            {
                baseTip,
                baseTip + forward * (length * 0.30f) + right * (width * 0.5f),
                baseTip + forward * (length * 0.75f) + right * (width * 0.35f),
                baseTip + forward * length,
                baseTip + forward * (length * 0.75f) - right * (width * 0.35f),
                baseTip + forward * (length * 0.30f) - right * (width * 0.5f)
            };

            draft.AddConvexPolygon(points, normal);
            draft.AddConvexPolygon(points, -normal); // double-sided
        }

        private static float Hash01(int seed)
        {
            var value = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        // ------------------------------------------------------------------
        // Shared helper
        // ------------------------------------------------------------------

        private static GameObject Box(
            Transform parent, Material material, string name,
            float sizeX, float sizeY, float sizeZ, float chamfer,
            Vector3 position)
        {
            return Box(parent, material, name, sizeX, sizeY, sizeZ, chamfer, position, Quaternion.identity);
        }

        private static GameObject Box(
            Transform parent, Material material, string name,
            float sizeX, float sizeY, float sizeZ, float chamfer,
            Vector3 position, Quaternion rotation)
        {
            var mesh = QuartersMeshes.ChamferedBox($"Quarters {name}", sizeX, sizeY, sizeZ, chamfer);
            return QuartersSceneSetup.MeshObject(parent, name, mesh, material, position, rotation);
        }
    }
}

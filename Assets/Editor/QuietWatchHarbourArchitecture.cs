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
    /// <summary>Original station districts, batched into the existing station LODs.</summary>
    internal static class QuietWatchHarbourArchitecture
    {
        [Serializable] private sealed class Layout { public Block[] blocks; public Route[] routes; public Arch[] arches; }
        [Serializable] private sealed class Arch
        {
            public string name; public float radius,width,depth,z,start,end;
        }
        [Serializable] private sealed class Route
        {
            public string name, family;
            public Vector3[] points;
            public float scale, living, quietDuration, phase, clearance;
            public bool availableInQuiet, grace, shuttle;
        }
        [Serializable] private sealed class Block
        {
            public string name, material;
            public float[] position, size;
            public bool windows;
        }

        private static Layout ReadLayout() => JsonUtility.FromJson<Layout>(File.ReadAllText(
            Path.Combine(Application.dataPath, "../ArtSource/harbour-layout.json")));

        public static void Build(Transform station)
        {
            var layout = ReadLayout();
            if (layout?.blocks == null || layout.blocks.Length == 0)
                throw new InvalidOperationException("Missing authored harbour districts.");
            var materials = new[]
            {
                Surface("Harbour District Ceramic", new Color(0.38f, 0.43f, 0.47f)),
                Surface("Harbour District Graphite", new Color(0.065f, 0.085f, 0.105f)),
                Surface("Harbour District Ochre", new Color(0.42f, 0.24f, 0.095f)),
                PortLight("Harbour District Windows", new Color(2.4f, 1.53f, 0.78f)),
                PortLight("Harbour Berth Edge", new Color(0.5f, 1.5f, 1.85f))
            };
            var group = station.GetComponent<LODGroup>();
            var lods = group.GetLODs();
            for (var lod = 0; lod < lods.Length; lod++)
            {
                var drafts = Enumerable.Range(0, materials.Length).Select(_ => new MeshDraft()).ToArray();
                foreach (var block in layout.blocks)
                {
                    var center = Vector(block.position);
                    var size = Vector(block.size);
                    var surface = block.material == "dark" ? 1 : block.material == "ochre" ? 2 : 0;
                    if (lod == 0)
                        QuartersMeshes.AppendChamferedBox(drafts[surface], center, size,
                            Mathf.Min(0.12f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.15f));
                    else Box(drafts[surface], center, size);
                    if (lod == 0)
                    {
                        // The same authored solids drive geometry AND clearance.
                        // Children inherit exactly the station geometry's transform.
                        var marker = new GameObject("Clearance - " + block.name);
                        marker.transform.SetParent(station, false);
                        marker.transform.localPosition = center;
                        marker.hideFlags = HideFlags.HideInHierarchy;
                        marker.AddComponent<HarbourClearanceVolume>().ConfigureBox(block.name, size);
                    }
                    if (block.windows) Windows(drafts[3], center, size, lod);
                }
                foreach(var arch in layout.arches ?? Array.Empty<Arch>())
                    Archway(station,drafts,arch,lod);
                // Short, physical edge strips reveal the berth's depth; no floating sign.
                var floor = layout.blocks.Single(b => b.name == "East berth floor");
                var roof = layout.blocks.Single(b => b.name == "East berth roof");
                var floorCenter = Vector(floor.position); var floorSize = Vector(floor.size);
                var roofCenter = Vector(roof.position); var roofSize = Vector(roof.size);
                foreach (var side in new[] { -1f, 1f })
                    Box(drafts[4], floorCenter + new Vector3(side * (floorSize.x * 0.5f - 1.15f),
                        floorSize.y * 0.5f + 0.08f, 0.5f), new Vector3(0.10f, 0.08f, floorSize.z - 2f));
                Box(drafts[4], roofCenter + new Vector3(0f, -roofSize.y * 0.5f - 0.05f, roofSize.z * 0.5f - 0.02f),
                    new Vector3(roofSize.x - 3f, 0.12f, 0.08f));
                // Deep work lights sit on the ceiling, not across the open mouth.
                for (var rib=0;rib<4;rib++)
                    Box(drafts[3],new Vector3(20f,9.36f,3.5f+rib*1.8f),new Vector3(9.6f,.10f,.28f));
                // Concourse mullions and roof equipment provide scale between tiny windows and giant hulls.
                for(var pier=0;pier<22;pier++)
                    Box(drafts[1],new Vector3(-42f+pier*4f,26f,-2.92f),new Vector3(.20f,2.5f,.24f));
                var renderers = new List<Renderer>();
                for (var surface = 0; surface < drafts.Length; surface++)
                {

                    var name = $"Harbour Districts LOD{lod} Surface{surface}";
                    var go = QuartersSceneSetup.MeshObject(station, name, drafts[surface].ToMesh(name),
                        materials[surface], Vector3.zero, Quaternion.identity);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    go.layer = QuietWatchArtAssetBuilder.ExteriorLayer;
                    GameObjectUtility.SetStaticEditorFlags(go, 0);
                    var renderer = go.GetComponent<MeshRenderer>();
                    renderer.shadowCastingMode = surface < 3 ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    renderer.receiveShadows = surface < 3;
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    renderers.Add(renderer);
                }
                lods[lod] = new LOD(lods[lod].screenRelativeTransitionHeight,
                    lods[lod].renderers.Concat(renderers).ToArray());
            }
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        public static Transform[] BuildTraffic(Transform vista, Transform station)
        {
            var layout = ReadLayout();
            if (layout?.routes == null || layout.routes.Length != 6)
                throw new InvalidOperationException("Expected six authored harbour corridors.");
            return layout.routes.Select(spec =>
            {
                var points = spec.points.Select(p => vista.InverseTransformPoint(station.TransformPoint(p))).ToArray();
                var ship = QuietWatchArtAssetBuilder.InstantiateLod(vista, spec.family, spec.name,
                    points[0], Quaternion.identity, spec.scale, spec.grace);
                ship.gameObject.AddComponent<HarbourTrafficRoute>().Configure(points, spec.living,
                    spec.quietDuration, spec.phase, spec.clearance, 3f, spec.availableInQuiet, spec.grace, spec.shuttle);
                return ship;
            }).ToArray();
        }

        private static void Archway(Transform station, MeshDraft[] drafts, Arch arch, int lod)
        {
            var steps=lod==0?64:lod==1?40:24;
            Vector3 Point(float angle,float radius,float z) => new Vector3(
                Mathf.Cos(angle*Mathf.Deg2Rad)*radius,Mathf.Sin(angle*Mathf.Deg2Rad)*radius,z);
            for(var i=0;i<steps;i++)
            {
                var a=Mathf.Lerp(arch.start,arch.end,i/(float)steps);
                var b=Mathf.Lerp(arch.start,arch.end,(i+1f)/steps);
                var r0=arch.radius-arch.width*.5f;var r1=arch.radius+arch.width*.5f;
                var z0=arch.z-arch.depth*.5f;var z1=arch.z+arch.depth*.5f;
                foreach(var z in new[] {z0,z1})
                    drafts[0].AddQuadOriented(Point(a,r0,z),Point(b,r0,z),Point(b,r1,z),Point(a,r1,z),z==z1?Vector3.forward:Vector3.back);
                foreach(var r in new[] {r0,r1})
                    drafts[1].AddQuadOriented(Point(a,r,z0),Point(b,r,z0),Point(b,r,z1),Point(a,r,z1),
                        Point((a+b)*.5f,1,0)*(r==r1?1:-1));
                // Recessed, segmented light rail follows the structural curve.
                var gap=(b-a)*.15f;
                drafts[4].AddQuadOriented(Point(a+gap,r0+.20f,z1+.015f),Point(b-gap,r0+.20f,z1+.015f),
                    Point(b-gap,r0+.32f,z1+.015f),Point(a+gap,r0+.32f,z1+.015f),Vector3.forward);
                if(i==0 || i==steps-1)
                {
                    var end=i==0?a:b;
                    var tangent=new Vector3(-Mathf.Sin(end*Mathf.Deg2Rad),Mathf.Cos(end*Mathf.Deg2Rad),0)*(i==0?-1:1);
                    drafts[0].AddQuadOriented(Point(end,r0,z0),Point(end,r1,z0),Point(end,r1,z1),Point(end,r0,z1),tangent);
                }
            }
            if(lod==0)
                for(var i=0;i<=64;i++)
                {
                    var marker=new GameObject("Clearance - "+arch.name+" "+i);
                    marker.transform.SetParent(station,false);
                    marker.transform.localPosition=Point(Mathf.Lerp(arch.start,arch.end,i/64f),arch.radius,arch.z);
                    marker.hideFlags=HideFlags.HideInHierarchy;
                    marker.AddComponent<HarbourClearanceVolume>().Configure(arch.name,1.7f);
                }
        }

        private static void Box(MeshDraft draft, Vector3 center, Vector3 size)
        {
            // Six ordinary faces at lower LODs, without zero-width bevel triangles.
            foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.up,
                Vector3.down, Vector3.forward, Vector3.back })
            {
                var u = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(normal, u);
                var p = center + Vector3.Scale(normal, size) * 0.5f;
                u = Vector3.Scale(u, size) * 0.5f;
                v = Vector3.Scale(v, size) * 0.5f;
                draft.AddQuadOriented(p-u-v, p+u-v, p+u+v, p-u+v, normal);
            }
        }

        private static Vector3 Vector(float[] value)
        {
            if (value == null || value.Length != 3) throw new InvalidOperationException("Invalid harbour vector.");
            return new Vector3(value[0], value[1], value[2]);
        }

        private static Material PortLight(string name, Color color)
        {
            var material = QuartersSceneSetup.CreateMaterial(name,color);
            material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor",color);
            material.SetTexture("_BaseMap",null);
            material.SetFloat("_Surface",0f);
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Surface(string name, Color color)
        {
            var material = QuartersSceneSetup.CreateMaterial(name, color);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_Metallic", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Windows(MeshDraft draft, Vector3 center, Vector3 size, int lod)
        {
            // Roughly one-metre apertures at FleetScale, with unlit rooms and
            // gaps between districts. Coarser LOD keeps the same window size.
            for (var deck = 0; deck < Mathf.FloorToInt(size.y / 0.36f); deck++)
                for (var bay = 0; bay < Mathf.FloorToInt((size.x - 0.4f) / 0.25f); bay++)
                {
                    var stride = lod == 2 ? 3 : lod == 1 ? 2 : 1;
                    if ((bay / stride * 17 + deck * 11) % 13 < 4 || bay % stride != 0) continue;
                    // Far LOD merges adjacent apertures instead of deleting inhabited decks.
                    var halfWidth = .050f * stride;
                    var p = center + new Vector3(-size.x * 0.5f + 0.25f + bay * 0.25f,
                        -size.y * 0.5f + 0.22f + deck * 0.36f, size.z * 0.5f + 0.015f);
                    draft.AddQuadOriented(p + new Vector3(-halfWidth, -0.045f, 0),
                        p + new Vector3(halfWidth, -0.045f, 0), p + new Vector3(halfWidth, 0.045f, 0),
                        p + new Vector3(-halfWidth, 0.045f, 0), Vector3.forward);
                }
        }
    }
}

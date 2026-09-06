using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarshipCabin.EditorTools
{
    /// <summary>Editor-only geometric occlusion. No distant shadow-map expansion or runtime rays.</summary>
    internal static class QuietWatchHullLighting
    {
        private static System.Numerics.Vector3 Numeric(Vector3 v) => new System.Numerics.Vector3(v.x,v.y,v.z);

        [Serializable] private sealed class BakeEvidence
        {
            public string assembly;
            public int meshes, vertices, shadedVertices, shadowedVertices;
            public Vector3 sunDirection;
        }

        public static void Bake(Transform root, Vector3 sun, bool fixedStructure)
        {
            const string folder = "Assets/Generated/ExteriorLighting";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var shader = Shader.Find("StarshipCabin/QuietWatchHull");
            if (shader == null) throw new InvalidOperationException("Missing exterior hull shader.");
            sun.Normalize();
            var evidence = new BakeEvidence { assembly = root.name, sunDirection = sun };
            var triangles = new List<QuietWatchOcclusionBvh.Triangle>();
            var materialCopies = new Dictionary<Material, Material>();
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var groups = root.GetComponentsInChildren<LODGroup>(true);
            var lodRenderers = groups.SelectMany(g => g.GetLODs()).SelectMany(l => l.renderers).ToArray();
            if (lodRenderers.Any(renderer => renderer == null))
                throw new InvalidOperationException(root.name + ": LOD contains a missing renderer.");
            foreach (var renderer in lodRenderers.OfType<MeshRenderer>())
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException(root.name + ": LOD renderer has no mesh: " + renderer.name);
            }
            var allLods = new HashSet<Renderer>(lodRenderers);
            var near = new HashSet<Renderer>(groups.SelectMany(g => g.GetLODs().Take(1)).SelectMany(l => l.renderers));
            {
                // Bake directly against LOD0 triangle geometry. No physics scene,
                // colliders, layer changes or imported-asset mutation are needed.
                foreach (var filter in filters)
                {
                    var renderer = filter.GetComponent<Renderer>();
                    if (!near.Contains(renderer) || (fixedStructure && renderer.shadowCastingMode == ShadowCastingMode.Off)) continue;
                    var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
                    var transform=filter.transform.localToWorldMatrix;
                    for(var i=0;i<indices.Length;i+=3)
                        triangles.Add(new QuietWatchOcclusionBvh.Triangle(
                            Numeric(transform.MultiplyPoint3x4(vertices[indices[i]])),
                            Numeric(transform.MultiplyPoint3x4(vertices[indices[i+1]])),
                            Numeric(transform.MultiplyPoint3x4(vertices[indices[i+2]]))));
                }
                var bvh = new QuietWatchOcclusionBvh(triangles.ToArray());
                var size = Mathf.Max(1f, root.GetComponent<LODGroup>().size * root.lossyScale.x);
                var bias = size * 0.00008f;
                var reach = size * 0.07f;
                bool Blocked(Vector3 origin, Vector3 direction, float distance) =>
                    bvh.Blocked(Numeric(origin), Numeric(direction), distance);
                var index = 0;
                foreach (var filter in filters)
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer == null || !allLods.Contains(renderer) || (fixedStructure && renderer.shadowCastingMode == ShadowCastingMode.Off)) continue;
                    var source = filter.sharedMesh;
                    // Source assets remain immutable. Editor mesh reads work
                    // outside the rendering loop even with Read/Write disabled.
                    var vertices = source.vertices; var normals = source.normals;
                    var colors = new Color32[vertices.Length];
                    var matrix = filter.transform.localToWorldMatrix;
                    var normalMatrix = matrix.inverse.transpose;
                    for (var i = 0; i < vertices.Length; i++)
                    {
                        var normal = normalMatrix.MultiplyVector(normals[i]).normalized;
                        var origin = matrix.MultiplyPoint3x4(vertices[i]) + normal * bias;
                        var tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                        var bitangent = Vector3.Cross(normal, tangent);
                        var hits = 0;
                        foreach (var direction in new[] { normal + tangent, normal - tangent, normal + bitangent, normal - bitangent })
                            if (Blocked(origin, direction.normalized, reach)) hits++;
                        var visibility = fixedStructure && Vector3.Dot(normal, sun) > 0f && Blocked(origin, sun, size * 2f) ? 0f : 1f;
                        colors[i] = new Color32((byte)(visibility*255), (byte)Mathf.RoundToInt((1f-hits*.19f)*255), 255, 255);
                        if (hits > 0) evidence.shadedVertices++;
                        if (visibility < .5f) evidence.shadowedVertices++;
                    }
                    var mesh = new Mesh { indexFormat = source.indexFormat, vertices = vertices, normals = normals,
                        tangents = source.tangents, uv = source.uv, uv2 = source.uv2, subMeshCount = source.subMeshCount };
                    for (var sub = 0; sub < source.subMeshCount; sub++) mesh.SetTriangles(source.GetTriangles(sub), sub);
                    mesh.RecalculateBounds();
                    mesh.name = root.name + " Exterior Occlusion " + index++;
                    mesh.colors32 = colors;
                    var portPositions = new List<Vector3>(vertices.Length);
                    var portNormals = new List<Vector3>(vertices.Length);
                    var intoPort = root.worldToLocalMatrix * matrix;
                    var normalIntoPort = intoPort.inverse.transpose;
                    for(var i=0;i<vertices.Length;i++)
                    {
                        portPositions.Add(intoPort.MultiplyPoint3x4(vertices[i]));
                        portNormals.Add(normalIntoPort.MultiplyVector(normals[i]).normalized);
                    }
                    mesh.SetUVs(2,portPositions); mesh.SetUVs(3,portNormals);
                    evidence.meshes++; evidence.vertices += vertices.Length;
                    var path = folder + "/" + mesh.name.Replace("/", "-") + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing != null) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
                    else AssetDatabase.CreateAsset(mesh, path);
                    filter.sharedMesh = mesh;
                    var slots = renderer.sharedMaterials;
                    for (var slot = 0; slot < slots.Length; slot++)
                    {
                        var original = slots[slot];
                        if (!materialCopies.TryGetValue(original, out var material))
                        {
                            var matPath = folder + "/" + root.name + " - " + original.name + ".mat";
                            material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
                            material.shader = shader;
                            material.SetColor("_BaseColor", original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white);
                            material.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                            material.SetTexture("_BumpMap", original.HasProperty("_BumpMap") ? original.GetTexture("_BumpMap") : null);
                            foreach (var property in new[] { "_MetallicGlossMap", "_OcclusionMap" })
                                material.SetTexture(property, original.HasProperty(property) ? original.GetTexture(property) : null);
                            material.SetFloat("_Metallic", original.HasProperty("_Metallic") ? original.GetFloat("_Metallic") : 0f);
                            material.SetFloat("_Smoothness", original.HasProperty("_Smoothness") ? original.GetFloat("_Smoothness") : .2f);
                            // The baked hull shader always samples its emission inputs and does
                            // not use URP's _EMISSION keyword. Unity can also remove that keyword
                            // from an otherwise valid authored material during ShaderGUI import,
                            // so gating the copy on it silently extinguishes engine apertures.
                            material.SetColor("_EmissionColor", original.HasProperty("_EmissionColor")
                                ? original.GetColor("_EmissionColor")
                                : Color.black);
                            material.SetTexture("_EmissionMap", original.HasProperty("_EmissionMap") ? original.GetTexture("_EmissionMap") : null);
                            material.SetVector("_SunDirection", sun);
                            material.SetColor("_SunColor", new Color(1.2f, 1.10f, 0.91f));
                            material.SetFloat("_FixedShadow", fixedStructure ? 1f : 0f);
                            material.SetFloat("_PortLighting", fixedStructure ? 1f : 0f);
                            EditorUtility.SetDirty(material);
                            materialCopies.Add(original, material);
                        }
                        slots[slot] = material;
                    }
                    renderer.sharedMaterials = slots;
                }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds/Validation");
            File.WriteAllText("Builds/Validation/lighting-" + root.name.Replace(" ", "-") + ".json",
                JsonUtility.ToJson(evidence, true));
        }
    }
}

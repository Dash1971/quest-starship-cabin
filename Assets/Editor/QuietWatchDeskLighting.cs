using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarshipCabin.EditorTools
{
    /// <summary>The proven chess diffuse/probe treatment, preserving desk surface textures.</summary>
    internal static class QuietWatchDeskLighting
    {
        internal static MeshRenderer[] Surfaces(Transform furnishings)
        {
            return furnishings.GetComponentsInChildren<MeshRenderer>(true).Where(r =>
                r.GetComponent<TextMesh>() == null && r.name != QuietWatchChessTerminal.ScreenName &&
                (r.transform.parent.name.StartsWith("Book:") || r.transform.parent.name == "Personal Desk Computer"
                    || r.name == "Desk Lamp Stem" || r.name == "Desk Drawer Hardware")).ToArray();
        }

        internal static void Build(Transform furnishings)
        {
            var group = new GameObject("Desk Diffuse Light Probes").transform;
            group.SetParent(furnishings, false);
            var positions = new List<Vector3>();
            // Free space above the display, books and lamp, inside the cabin walls.
            foreach (var x in new[] { -3.08f, -2.72f, -2.4f })
                foreach (var y in new[] { 1.24f, 1.48f })
                    foreach (var z in new[] { 1.45f, 2.0f, 2.48f })
                        positions.Add(new Vector3(x, y, z));
            group.gameObject.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
            var anchor = new GameObject("Desk Diffuse Anchor").transform;
            anchor.SetParent(group, false);
            anchor.localPosition = new Vector3(-2.72f, 1.24f, 2.05f);
            var materials = new Dictionary<Material, Material>();
            foreach (var renderer in Surfaces(furnishings))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    if (materials.TryGetValue(source, out var cached)) return cached;
                    var material = QuartersSceneSetup.CreateMaterial("Desk Diffuse - " + source.name, source.GetColor("_BaseColor"));
                    var shader = Shader.Find(QuartersDecor.ChessShader);
                    if (shader == null) throw new InvalidOperationException("Missing desk diffuse shader.");
                    // Dedicated materials: graphite is also used elsewhere in the cabin.
                    material.shader = shader;
                    material.shaderKeywords = Array.Empty<string>();
                    material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                    material.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                    material.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
                    material.SetFloat("_Surface", 0); material.SetFloat("_AlphaClip", 0);
                    material.SetFloat("_Cull", (float)CullMode.Back);
                    material.SetFloat("_SrcBlend", (float)BlendMode.One);
                    material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                    material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                    material.SetFloat("_ZWrite", 1);
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.renderQueue = (int)RenderQueue.Geometry;
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    EditorUtility.SetDirty(material);
                    materials.Add(source, material);
                    return material;
                }).ToArray();
                renderer.receiveGI = ReceiveGI.LightProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.probeAnchor = anchor;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                EditorUtility.SetDirty(renderer);
            }
        }

        internal static void Validate(bool requireBake)
        {
            var furnishings = GameObject.Find("Furnishings");
            var group = GameObject.Find("Desk Diffuse Light Probes")?.GetComponent<LightProbeGroup>();
            var anchor = GameObject.Find("Desk Diffuse Anchor")?.transform;
            if (furnishings == null || group == null || group.probePositions.Length != 18 || anchor == null)
                throw new InvalidOperationException("Desk diffuse probes are missing; regenerate the scene.");
            var surfaces = Surfaces(furnishings.transform);
            // Five books x three meshes, six computer meshes, lamp stem and drawer pull.
            if (surfaces.Length != 23) throw new InvalidOperationException("Desk diffuse surface coverage changed: " + surfaces.Length);
            foreach (var renderer in surfaces)
            {
                if (renderer.sharedMaterials.Any(m => m.shader.name != QuartersDecor.ChessShader
                        || !m.shader.isSupported || ShaderUtil.ShaderHasError(m.shader))
                    || renderer.receiveGI != ReceiveGI.LightProbes || renderer.lightProbeUsage != LightProbeUsage.BlendProbes
                    || renderer.reflectionProbeUsage != ReflectionProbeUsage.Off || renderer.probeAnchor != anchor)
                    throw new InvalidOperationException("Desk diffuse lighting contract failed: " + renderer.name);
                if (renderer.transform.parent.name.StartsWith("Book:") && !renderer.name.EndsWith("Rules")
                    && renderer.sharedMaterial.GetTexture("_BaseMap") == null)
                    throw new InvalidOperationException("Book texture lost: " + renderer.name);
                if (requireBake && renderer.lightmapIndex >= 0 && renderer.lightmapIndex < 65534)
                    throw new InvalidOperationException("Desk object still samples a lightmap: " + renderer.name);
            }
            QuietWatchChessTerminal.Validate();
            if (!requireBake) return;
            if (LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.bakedProbes.Length < 36)
                throw new InvalidOperationException("Desk and chess probes need a fresh bake.");
            LightProbes.GetInterpolatedProbe(anchor.position, null, out var probe);
            var colors = new Color[6];
            probe.Evaluate(new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back }, colors);
            var energy = 0f;
            foreach (var color in colors)
            {
                var value = color.r + color.g + color.b;
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("Non-finite desk probe.");
                energy += Mathf.Max(0, value);
            }
            if (energy <= .0001f) throw new InvalidOperationException("Desk probe is black; inspect the bake.");
        }
    }
}

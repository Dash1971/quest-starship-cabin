using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StarshipCabin.EditorTools
{
    /// <summary>One static, matte, original bitmap display: no realtime UI or chess engine.</summary>
    internal static class QuietWatchChessTerminal
    {
        internal const string ScreenName = "Computer Chess Terminal";
        private const string TexturePath = "Assets/Art/QuietWatch/Textures/QW_ChessTerminal.png";
        private const string ScreenShader = "Universal Render Pipeline/Unlit";

        internal static void Build(Transform computer)
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing chess terminal artwork.");
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_4x4 });
            if (AssetDatabase.WriteImportSettingsIfDirty(TexturePath))
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var material = QuartersSceneSetup.CreateMaterial("Computer Static Chess Display", Color.white);
            var shader = Shader.Find(ScreenShader);
            if (shader == null) throw new InvalidOperationException("Missing terminal shader.");
            material.shader = shader;
            material.shaderKeywords = Array.Empty<string>();
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetFloat("_Surface", 0); material.SetFloat("_AlphaClip", 0);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            // Facing +X: screen-right is world +Z. Explicit UVs keep the diagram upright.
            var mesh = new Mesh { name = "Computer Chess Terminal Quad" };
            mesh.vertices = new[] {
                new Vector3(-2.991f, .902f, 1.895f), new Vector3(-2.991f, .902f, 1.505f),
                new Vector3(-2.991f, 1.148f, 1.505f), new Vector3(-2.991f, 1.148f, 1.895f) };
            mesh.uv = new[] { Vector2.right, Vector2.zero, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var display = QuartersSceneSetup.MeshObject(computer, ScreenName, mesh, material, Vector3.zero, Quaternion.identity, false);
            var renderer = display.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveGI = ReceiveGI.LightProbes;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(display, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        internal static void Validate()
        {
            var renderer = GameObject.Find(ScreenName)?.GetComponent<MeshRenderer>();
            var texture = renderer != null ? renderer.sharedMaterial.GetTexture("_BaseMap") : null;
            if (renderer == null || renderer.sharedMaterial.shader.name != ScreenShader
                || !renderer.sharedMaterial.shader.isSupported || ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader)
                || texture == null || texture.width != 1024 || texture.height != 640
                || renderer.reflectionProbeUsage != ReflectionProbeUsage.Off || renderer.lightProbeUsage != LightProbeUsage.Off
                || renderer.sharedMaterial.globalIlluminationFlags != MaterialGlobalIlluminationFlags.None
                || GameObject.Find("Computer Reading Page") != null)
                throw new InvalidOperationException("Static chess display contract failed; regenerate the scene.");
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null || mesh.vertexCount != 4 || mesh.normals[0].x < .99f
                || mesh.uv[0] != Vector2.right || mesh.uv[1] != Vector2.zero
                || mesh.uv[2] != Vector2.up || mesh.uv[3] != Vector2.one)
                throw new InvalidOperationException("Terminal is mirrored or has incorrect UVs.");
        }
    }
}

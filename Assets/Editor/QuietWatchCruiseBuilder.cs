using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    internal static class QuietWatchCruiseBuilder
    {
        public static Renderer Build(Transform parent, out Renderer comet)
        {
            var catalogue = FirstQuestionField.Catalogue();
            var vertices = new Vector3[catalogue.Length*4]; var uv = new Vector2[vertices.Length];
            var data = new List<Vector3>(vertices.Length); var colors = new Color[vertices.Length];
            var indices = new int[catalogue.Length*6];
            var corners = new[] { new Vector2(-1,-1), new Vector2(1,-1), new Vector2(1,1), new Vector2(-1,1) };
            var order = new[] {0,1,2,0,2,3};
            for (var i=0; i<catalogue.Length; i++)
            {
                var star = catalogue[i];
                for (var k=0; k<4; k++)
                {
                    vertices[i*4+k] = new Vector3(star.X,star.Y,star.Z); uv[i*4+k] = corners[k];
                    data.Add(new Vector3(star.Sigma,i,star.Scale));
                    colors[i*4+k] = new Color(star.R,star.G,star.B,star.Flux);
                }
                for(var k=0;k<6;k++) indices[i*6+k]=i*4+order[k];
            }
            var mesh = new Mesh { name="First Question Unified Stellar Field", indexFormat=IndexFormat.UInt32,
                vertices=vertices, uv=uv, colors=colors, triangles=indices };
            mesh.SetUVs(1,data);
            // Shader projects astronomical proxy directions at 12 km; bounds cover every seat/eye.
            mesh.bounds = new Bounds(Vector3.zero,Vector3.one*26000);
            var material = CreateFieldMaterial("First Question Stellar Field", "StarshipCabin/QuietWatchCruiseStars");
            material.SetFloat("_WrapWidth",FirstQuestionField.Period);
            material.SetVector("_DepthRange",new Vector4(FirstQuestionField.NearDepth,FirstQuestionField.FarDepth,0,0));
            material.SetFloat("_ReviewStar",-1);
            EditorUtility.SetDirty(material);
            var renderer = Surface(parent,"First Question Stellar Field",mesh,material);
            var cometMesh = new Mesh { name="First Question Distant Comet" };
            cometMesh.vertices = new[] { Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero };
            cometMesh.uv = corners; cometMesh.triangles=order;
            cometMesh.bounds = mesh.bounds;
            var cometMaterial = CreateFieldMaterial("First Question Distant Comet","StarshipCabin/QuietWatchDistantComet");
            cometMaterial.SetVector("_Extent",new Vector4(FirstQuestionField.CometTailDegrees*Mathf.Deg2Rad,
                FirstQuestionField.CometHalfWidthDegrees*Mathf.Deg2Rad,0,0));
            EditorUtility.SetDirty(cometMaterial);
            comet = Surface(parent,"First Question Distant Comet",cometMesh,cometMaterial);
            return renderer;
        }

        private static Material CreateFieldMaterial(string name,string shaderName)
        {
            var shader=Shader.Find(shaderName);
            if(shader==null) throw new InvalidOperationException("Missing stellar shader: "+shaderName);
            var material=QuartersSceneSetup.CreateMaterial(name,Color.white);
            material.shader=shader; material.shaderKeywords=Array.Empty<string>(); material.renderQueue=-1;
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            return material;
        }

        private static Renderer Surface(Transform parent,string name,Mesh mesh,Material material)
        {
            var go=QuartersSceneSetup.MeshObject(parent,name,mesh,material,Vector3.zero,Quaternion.identity,false);
            go.layer=QuietWatchArtAssetBuilder.ExteriorLayer;
            GameObjectUtility.SetStaticEditorFlags(go,0);
            var renderer=go.GetComponent<Renderer>(); renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false; renderer.allowOcclusionWhenDynamic=false;
            renderer.lightProbeUsage=LightProbeUsage.Off; renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return renderer;
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    internal static class QuietWatchCruiseBuilder
    {
        public static Renderer Build(Transform parent)
        {
            // One draw, 1,536 point sources. Each quad shares a world-space anchor;
            // the vertex shader advances that anchor and expands a filtered core.
            const int count = 1536;
            var random = new System.Random(71029);
            var vertices = new Vector3[count * 4]; var uv = new Vector2[count * 4];
            var colors = new Color[count * 4]; var indices = new int[count * 6];
            var corners = new[] { new Vector2(-1,-1), new Vector2(1,-1), new Vector2(1,1), new Vector2(-1,1) };
            var order=new[] {0,1,2,0,2,3};
            for (var i=0; i<count; i++)
            {
                var anchor = new Vector3((float)(random.NextDouble()-.5)*FirstQuestionVista.CruiseWidth,
                    (float)(random.NextDouble()-.5)*6400, (float)random.NextDouble()*6000);
                var tint = Color.Lerp(new Color(1f,.77f,.57f),new Color(.68f,.83f,1f),(float)random.NextDouble());
                tint.a = Mathf.Lerp(.22f,1f,Mathf.Pow((float)random.NextDouble(),2));
                for(var k=0;k<4;k++) { vertices[i*4+k]=anchor;uv[i*4+k]=corners[k];colors[i*4+k]=tint; }
                for(var k=0;k<6;k++) indices[i*6+k]=i*4+order[k];
            }
            var mesh = new Mesh { name="First Question Cruise Stars", vertices=vertices,uv=uv,colors=colors,triangles=indices };
            mesh.bounds = new Bounds(new Vector3(0,0,-4600),new Vector3(32200,6600,6400));
            var material = QuartersSceneSetup.CreateMaterial("Cruise Stellar Cores",Color.white);
            material.shader = Shader.Find("StarshipCabin/QuietWatchCruiseStars");
            if(material.shader==null) throw new InvalidOperationException("Missing cruise star shader.");
            material.SetFloat("_WrapWidth",FirstQuestionVista.CruiseWidth);
            EditorUtility.SetDirty(material);
            var go = QuartersSceneSetup.MeshObject(parent,"Cruise Star Volume",mesh,material,Vector3.zero,Quaternion.identity,false);
            go.transform.localPosition=Vector3.zero;go.layer=QuietWatchArtAssetBuilder.ExteriorLayer;
            GameObjectUtility.SetStaticEditorFlags(go,0);
            var renderer=go.GetComponent<Renderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;renderer.allowOcclusionWhenDynamic=false;
            return renderer;
        }
    }
}

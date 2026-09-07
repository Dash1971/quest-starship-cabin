using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StarshipCabin.EditorTools
{
    /// <summary>Small, physically constructed personal objects and restrained finishes.</summary>
    internal static class QuartersCabinCraft
    {
        public static void Finish(Material material,string texture,float smoothness,float tiling=1f)
        {
            var path="Assets/Art/QuietWatch/Textures/"+texture+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Missing cabin finish: "+path);
            importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear;importer.maxTextureSize=512;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name="Android",overridden=true,maxTextureSize=512,format=TextureImporterFormat.ASTC_6x6 });
            if(AssetDatabase.WriteImportSettingsIfDirty(path)) AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetTextureScale("_BaseMap",Vector2.one*tiling);
            material.SetFloat("_Metallic",0f);material.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(material);
        }

        public static Mesh Cushion(string name,Vector3 size)
        {
            // A smooth superellipsoid gives the broad faces a soft crown and rounded corners.
            const int rows=16,columns=32;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            float Signed(float value,float power) => Mathf.Sign(value)*Mathf.Pow(Mathf.Abs(value),power);
            for(var row=0;row<=rows;row++)
            {
                var v=row/(float)rows;var latitude=(v-.5f)*Mathf.PI;
                var ring=Mathf.Pow(Mathf.Max(0,Mathf.Cos(latitude)),.45f);
                for(var col=0;col<=columns;col++)
                {
                    var u=col/(float)columns;var longitude=u*Mathf.PI*2;
                    vertices.Add(Vector3.Scale(new Vector3(ring*Signed(Mathf.Cos(longitude),.38f),
                        Signed(Mathf.Sin(latitude),.50f),ring*Signed(Mathf.Sin(longitude),.38f)),size*.5f));
                    uv.Add(new Vector2(u,v));
                }
            }
            for(var row=0;row<rows;row++)
                for(var col=0;col<columns;col++)
                {
                    var a=row*(columns+1)+col;var b=a+columns+1;
                    void Triangle(int i,int j,int k)
                    {
                        var n=Vector3.Cross(vertices[j]-vertices[i],vertices[k]-vertices[i]);
                        if(n.sqrMagnitude<1e-14f)return;
                        if(Vector3.Dot(n,vertices[i]+vertices[j]+vertices[k])<0)(j,k)=(k,j);
                        indices.Add(i);indices.Add(j);indices.Add(k);
                    }
                    Triangle(a,b,b+1);Triangle(a,b+1,a+1);
                }
            var mesh=new Mesh {name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        public static void Build(Transform parent)
        {
            var graphite=QuartersSceneSetup.CreateMaterial("Cabin Computer Graphite",new Color(.10f,.115f,.12f));
            var ceramic=QuartersSceneSetup.CreateMaterial("Tea Cup Ceramic",new Color(.64f,.67f,.61f));
            ceramic.SetFloat("_Smoothness",.28f);
            var screen=QuartersSceneSetup.CreateMaterial("Computer Dark Glass",new Color(.018f,.03f,.032f));
            screen.SetFloat("_Smoothness",.12f);
            var keys=QuartersSceneSetup.CreateMaterial("Computer Keycaps",new Color(.25f,.27f,.26f));
            var root=new GameObject("Personal Desk Computer").transform;root.SetParent(parent,false);
            // Screen faces the desk seat (+X); books retain the far half of the desktop.
            Part(root,"Computer Foot",graphite,new Vector3(-3.00f,.754f,1.70f),new Vector3(.20f,.018f,.24f),.009f);
            Part(root,"Computer Stand",graphite,new Vector3(-3.035f,.824f,1.70f),new Vector3(.025f,.13f,.05f),.008f);
            Part(root,"Computer Display Housing",graphite,new Vector3(-3.015f,1.025f,1.70f),new Vector3(.034f,.285f,.43f),.012f);
            Part(root,"Computer Recessed Screen",screen,new Vector3(-2.996f,1.025f,1.70f),new Vector3(.006f,.251f,.394f),.002f);
            QuietWatchChessTerminal.Build(root);
            Part(root,"Keyboard Tray",graphite,new Vector3(-2.70f,.758f,1.70f),new Vector3(.22f,.021f,.36f),.009f);
            var keyboard=new MeshDraft();
            for(var row=0;row<4;row++)for(var col=0;col<11;col++)
                QuartersMeshes.AppendChamferedBox(keyboard,new Vector3(-2.778f+row*.043f,.772f,1.552f+col*.029f),new Vector3(.031f,.008f,.023f),.002f);
            QuartersSceneSetup.MeshObject(root,"Keyboard Keycaps",keyboard.ToMesh("Computer Keycaps"),keys);
            var trim=new MeshDraft();
            // Actual pull and drawer reveal below the far end of the desk.
            QuartersMeshes.AppendChamferedBox(trim,new Vector3(-2.479f,.64f,2.32f),new Vector3(.018f,.006f,.27f),.002f);
            QuartersMeshes.AppendChamferedBox(trim,new Vector3(-2.465f,.67f,2.32f),new Vector3(.025f,.015f,.12f),.005f);
            QuartersSceneSetup.MeshObject(parent,"Desk Drawer Hardware",trim.ToMesh("Desk Drawer Hardware"),graphite);
            // A small tea vignette fits beside the chessboard, entirely on the table.
            var tea=new GameObject("Tea beside the chessboard").transform;tea.SetParent(parent,false);
            Part(tea,"Tea Tray",graphite,new Vector3(-1.18f,.397f,.17f),new Vector3(.23f,.014f,.18f),.014f);
            var cup=new MeshDraft();
            var profile=new[] {new Vector2(0,.0f),new Vector2(.031f,0),new Vector2(.039f,.073f),
                new Vector2(.040f,.082f),new Vector2(.035f,.083f),new Vector2(.034f,.074f),new Vector2(.026f,.008f),new Vector2(0,.008f)};
            var center=new Vector3(-1.18f,.405f,.17f);
            for(var side=0;side<32;side++)for(var j=0;j<profile.Length-1;j++)
            {
                Vector3 P(int s,int k){var a=s*Mathf.PI*2/32;return center+new Vector3(Mathf.Cos(a)*profile[k].x,profile[k].y,Mathf.Sin(a)*profile[k].x);}
                var a=P(side,j);var b=P(side+1,j);var c=P(side+1,j+1);var d=P(side,j+1);
                if((Vector3.Cross(b-a,c-a)).sqrMagnitude>1e-14f)cup.AddTriangle(a,c,b);
                if((Vector3.Cross(c-a,d-a)).sqrMagnitude>1e-14f)cup.AddTriangle(a,d,c);
            }
            // Rounded loop handle, merged into the cup mesh.
            for(var i=0;i<24;i++)for(var j=0;j<8;j++)
            {
                Vector3 P(int a,int b){var u=a*Mathf.PI*2/24;var v=b*Mathf.PI*2/8;return center+new Vector3(.044f+(.025f+.004f*Mathf.Cos(v))*Mathf.Cos(u),.044f+(.025f+.004f*Mathf.Cos(v))*Mathf.Sin(u),.004f*Mathf.Sin(v));}
                cup.AddQuad(P(i,j),P(i+1,j),P(i+1,j+1),P(i,j+1));
            }
            var cupMesh=cup.ToMesh("Cabin Ceramic Cup");cupMesh.RecalculateNormals();
            QuartersSceneSetup.MeshObject(tea,"Ceramic Tea Cup",cupMesh,ceramic);
            var liquid=QuartersSceneSetup.CreateMaterial("Tea Surface",new Color(.11f,.054f,.02f));liquid.SetFloat("_Smoothness",.24f);
            var surface=new MeshDraft();
            for(var i=0;i<32;i++)
            {
                var a=i*Mathf.PI*2/32;var b=(i+1)*Mathf.PI*2/32;var c=center+Vector3.up*.062f;
                surface.AddTriangle(c,c+new Vector3(Mathf.Cos(b)*.031f,0,Mathf.Sin(b)*.031f),c+new Vector3(Mathf.Cos(a)*.031f,0,Mathf.Sin(a)*.031f));
            }
            QuartersSceneSetup.MeshObject(tea,"Tea Surface",surface.ToMesh("Cabin Tea Surface"),liquid);
        }

        private static void Part(Transform root,string name,Material material,Vector3 center,Vector3 size,float bevel)
        {
            QuartersSceneSetup.MeshObject(root,name,QuartersMeshes.ChamferedBox(name,size.x,size.y,size.z,bevel),material,center,Quaternion.identity);
        }
    }
}

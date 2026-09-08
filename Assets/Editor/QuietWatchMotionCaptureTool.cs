using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    /// <summary>Optional real Unity frame sequences. A still image cannot validate vection or a momentary fleet snap.</summary>
    public static class QuietWatchMotionCaptureTool
    {
        [Serializable] private sealed class Manifest
        {
            public string sourceHash, unityVersion;
            public int fps=24, width=960, height=640, framesPerClip=432;
            public string[] clips;
            public float maximumFormationRotationStep;
            public bool baked;
        }

        [MenuItem("Starship Cabin/Quiet Watch/Capture Motion Clips")]
        public static void CaptureMotionClips()
        {
            const string scene="Assets/Scenes/Cabin_Quarters_V2.unity";
            EditorSceneManager.OpenScene(scene);
            var generation=QuietWatchBuildValidation.RequireCurrentScene(false);
            var camera=Camera.main;
            if(camera==null) throw new InvalidOperationException("Generated scene has no camera.");
            var points=UnityEngine.Object.FindObjectsByType<VistaCapturePoint>(FindObjectsSortMode.None);
            var vistas=UnityEngine.Object.FindObjectsByType<VistaEnvironment>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var first=vistas.OfType<FirstQuestionVista>().Single();
            var formation=vistas.OfType<AuthoredVista>().Single(v=>v.VistaId=="long-formation");
            var views=QuietWatchFirstQuestionViews.All(points).Where(v=>v.Name=="Couch" || v.Name=="Desk"
                || v.Name=="Bed Sitting" || v.Name=="Standing upper window").ToArray();
            var output=Path.Combine("Builds/Motion",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            var evidence=new Manifest { sourceHash=generation.SourceHash, unityVersion=Application.unityVersion,
                baked=generation.BakedSourceHash==generation.SourceHash && LightmapSettings.lightmaps.Length>0 };
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldAspect=camera.aspect;
            var target=new RenderTexture(evidence.width,evidence.height,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(evidence.width,evidence.height,TextureFormat.RGB24,false);
            try
            {
                camera.transform.SetParent(null,true);camera.targetTexture=target;
                camera.aspect=(float)evidence.width/evidence.height;
                void Frame(string folder,int frame)
                {
                    camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply(false);
                    File.WriteAllBytes(Path.Combine(folder,$"{frame:0000}.png"),pixels.EncodeToPNG());
                }
                foreach(var vista in vistas)vista.gameObject.SetActive(vista==first);
                first.Enter(LifeMode.Quiet,MotionMode.Still);
                foreach(var view in views)
                {
                    var folder=Path.Combine(output,"cruise-"+view.Name.ToLowerInvariant().Replace(" ","-"));
                    Directory.CreateDirectory(folder);
                    camera.transform.SetPositionAndRotation(view.Position,view.Rotation);
                    for(var frame=0;frame<evidence.framesPerClip;frame++)
                    {
                        first.PreviewAt((float)frame/evidence.fps,LifeMode.Quiet,MotionMode.Drift);
                        Frame(folder,frame);
                    }
                }
                first.Exit();
                foreach(var vista in vistas)vista.gameObject.SetActive(vista==formation);
                formation.Enter(LifeMode.Living,MotionMode.Still);
                var couch=points.Single(p=>p.CaptureName=="Couch");
                camera.transform.SetPositionAndRotation(couch.transform.position,couch.transform.rotation);
                var rig=formation.transform.Find("Formation Flight Rig");
                var fleetFolder=Path.Combine(output,"formation-preview-cancel-replay");Directory.CreateDirectory(fleetFolder);
                for(var frame=0;frame<evidence.framesPerClip;frame++)
                {
                    var before=rig.localRotation;
                    if(frame==2*evidence.fps)formation.PreviewGraceNote();
                    if(frame==8*evidence.fps)formation.ApplyComfort(LifeMode.Quiet,MotionMode.Still);
                    if(frame==12*evidence.fps)
                    {
                        formation.ApplyComfort(LifeMode.Living,MotionMode.Still);
                        formation.PreviewGraceNote();
                    }
                    formation.AdvancePreview(1f/evidence.fps);
                    evidence.maximumFormationRotationStep=Mathf.Max(evidence.maximumFormationRotationStep,
                        Quaternion.Angle(before,rig.localRotation));
                    Frame(fleetFolder,frame);
                }
                if(evidence.maximumFormationRotationStep>.25f)
                    throw new InvalidOperationException("Formation motion clip contains an attitude discontinuity.");
                evidence.clips=Directory.GetDirectories(output).Select(Path.GetFileName).OrderBy(v=>v).ToArray();
                File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(evidence,true));
                Debug.Log("Quiet Watch motion sequences: "+Path.GetFullPath(output)+". Encode at 24 fps; these are graphics captures, not device performance measurements.");
            }
            finally
            {
                camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;
                UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.OpenScene(scene);
            }
        }
    }
}

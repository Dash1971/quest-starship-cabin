using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using StarshipCabin.QuietWatch;

namespace StarshipCabin.EditorTools
{
    /// <summary>Measures real rendered star centroids. Numerical world-X signs alone missed M11's regression.</summary>
    internal static class QuietWatchStarMotionAudit
    {
        [Serializable] private sealed class Evidence { public string sourceHash; public Sample[] samples; }
        [Serializable] private sealed class Sample
        {
            public string seat; public int star; public float beforeX,afterX,expectedDeltaX,measuredDeltaX;
        }

        internal static void Run(Camera camera,VistaCapturePoint[] points,FirstQuestionVista first,string sourceHash)
        {
            var stars=first.GetComponentsInChildren<Renderer>().Single(r=>r.sharedMaterial.shader.name=="StarshipCabin/QuietWatchCruiseStars");
            var comet=first.GetComponentsInChildren<Renderer>().Single(r=>r.sharedMaterial.shader.name=="StarshipCabin/QuietWatchDistantComet");
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var oldMask=camera.cullingMask;var oldClear=camera.clearFlags;var oldBackground=camera.backgroundColor;
            var oldAspect=camera.aspect;var oldForce=comet.forceRenderingOff;
            var data=camera.GetComponent<UniversalAdditionalCameraData>();var oldPost=data!=null && data.renderPostProcessing;
            var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(512,512,TextureFormat.RGB24,false);
            var block=new MaterialPropertyBlock();stars.GetPropertyBlock(block);
            var catalogue=FirstQuestionField.Catalogue();var samples=new List<Sample>();
            var clock=new VistaTimeline(780,120);clock.Reset(false,false);clock.SetModes(false,true);clock.Advance(20);
            try
            {
                camera.targetTexture=target;camera.aspect=1;camera.cullingMask=1<<QuietWatchArtAssetBuilder.ExteriorLayer;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                if(data!=null)data.renderPostProcessing=false;
                comet.forceRenderingOff=true;
                foreach(var point in points)
                {
                    camera.transform.SetPositionAndRotation(point.transform.position,point.transform.rotation);
                    // Cover near/far and both halves of the real camera projection at every seat.
                    foreach(var near in new[]{true,false}) foreach(var left in new[]{true,false})
                    {
                        var selected=-1;var expected=0f;
                        for(var i=0;i<catalogue.Length;i++)
                        {
                            var star=catalogue[i];if((star.Z>-23000)!=near)continue;
                            var a=FirstQuestionField.At(star,i,0);var b=FirstQuestionField.At(star,i,clock.DriftTravel);
                            var before=camera.WorldToViewportPoint(new Vector3((float)a.X,(float)a.Y,(float)a.Z));
                            var after=camera.WorldToViewportPoint(new Vector3((float)b.X,(float)b.Y,(float)b.Z));
                            if(a.Visibility<.99 || b.Visibility<.99 || before.z<=0 || after.z<=0)continue;
                            if((before.x<.5f)!=left || before.x<.2f || before.x>.8f || before.y<.2f || before.y>.8f
                                || after.x<.15f || after.x>.85f || after.y<.15f || after.y>.85f)continue;
                            expected=(after.x-before.x)*512;selected=i;break;
                        }
                        if(selected<0)throw new InvalidOperationException("No motion-audit star in camera: "+point.CaptureName);
                        float Centroid(float seconds)
                        {
                            first.PreviewAt(seconds,LifeMode.Quiet,MotionMode.Drift);
                            stars.GetPropertyBlock(block);block.SetFloat("_ReviewStar",selected);stars.SetPropertyBlock(block);
                            camera.Render();RenderTexture.active=target;
                            pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply(false);
                            var image=pixels.GetPixels32();double energy=0,weighted=0;
                            for(var i=0;i<image.Length;i++)
                            {
                                var c=image[i];var weight=(c.r+c.g+c.b)/3.0;
                                if(weight<2)continue;energy+=weight;weighted+=weight*(i%512+.5);
                            }
                            if(energy<10)throw new InvalidOperationException("Star shader did not render audit point: "+selected);
                            return (float)(weighted/energy);
                        }
                        var x0=Centroid(0);var x1=Centroid(20);var measured=x1-x0;
                        samples.Add(new Sample{seat=point.CaptureName,star=selected,beforeX=x0,afterX=x1,
                            expectedDeltaX=expected,measuredDeltaX=measured});
                        if(expected>=-.5f || measured>=-.5f || Mathf.Abs(expected-measured)>.8f)
                            throw new InvalidOperationException($"Rendered stellar flow is reversed or disagrees with projection: {point.CaptureName}, star {selected}, expected {expected:F2}, rendered {measured:F2} px.");
                    }
                }
                Directory.CreateDirectory("Builds/Validation");
                File.WriteAllText("Builds/Validation/first-question-motion.json",JsonUtility.ToJson(
                    new Evidence{sourceHash=sourceHash,samples=samples.ToArray()},true));
                Debug.Log("First Question GPU audit: 16 near/far, left/right camera samples move LEFT and agree with projection.");
            }
            finally
            {
                stars.GetPropertyBlock(block);block.SetFloat("_ReviewStar",-1);stars.SetPropertyBlock(block);
                first.PreviewAt(0,LifeMode.Quiet,MotionMode.Still);
                comet.forceRenderingOff=oldForce;
                camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;
                camera.cullingMask=oldMask;camera.clearFlags=oldClear;camera.backgroundColor=oldBackground;
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
                if(data!=null)data.renderPostProcessing=oldPost;
                UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}

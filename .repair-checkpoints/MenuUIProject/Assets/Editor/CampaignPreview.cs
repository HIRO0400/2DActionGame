using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

public static class CampaignPreview
{
    public static void Run() => EditorApplication.delayCall+=Capture;
    static void Capture()
    {
        var report=new List<string>();
        try
        {
            Directory.CreateDirectory("StageAuthoringReports/CampaignPreview");
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            for(int n=2;n<=5;n++)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage"+n+".unity");
                var target=new RenderTexture(960,540,24); target.Create();
                var loop=UnityEngine.Object.FindFirstObjectByType<BackgroundLooper>();
                var cameraTransform=(Transform)typeof(BackgroundLooper).GetField("cameraTransform",flags).GetValue(loop);
                var camera=cameraTransform.GetComponent<Camera>();
                foreach(var p in loop.GetComponentsInChildren<ParallaxBackground>()) typeof(ParallaxBackground).GetMethod("Awake",flags).Invoke(p,null);
                camera.backgroundColor=Color.magenta; camera.clearFlags=CameraClearFlags.SolidColor; camera.aspect=960f/540;
                var points=n==2?new[]{new Vector2(12,2),new Vector2(60,2),new Vector2(145,2)}:
                    n==3?new[]{new Vector2(30,3),new Vector2(86,3),new Vector2(138,3)}:
                    n==4?new[]{new Vector2(28,4),new Vector2(80,4),new Vector2(146,4)}:
                    new[]{new Vector2(41,5),new Vector2(94,5),new Vector2(137,5)};
                foreach(var point in points)
                {
                    cameraTransform.position=new Vector3(point.x,point.y,cameraTransform.position.z);
                    typeof(BackgroundLooper).GetMethod("OnCameraUpdated",flags).Invoke(loop,new object[]{camera.GetComponent<CinemachineBrain>()});
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;
                    var image=new Texture2D(960,540,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,960,540),0,0); image.Apply();
                    int holes=0; foreach(var color in image.GetPixels32()) if(color.r>200 && color.b>200 && color.g<80) holes++;
                    File.WriteAllBytes("StageAuthoringReports/CampaignPreview/Stage"+n+"_x"+point.x+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    if(holes!=0) throw new Exception("Exposed background pixels: Stage"+n+" at "+point+" count="+holes);
                    report.Add("PASS: Stage"+n+" camera "+point+" rendered with zero exposed clear-color pixels.");
                }
                RenderTexture.active=null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
            File.WriteAllLines("StageAuthoringReports/campaign-preview.txt",report); EditorApplication.Exit(0);
        }
        catch(Exception e) { report.Add("FAIL: "+e); File.WriteAllLines("StageAuthoringReports/campaign-preview.txt",report); EditorApplication.Exit(1); }
    }
}

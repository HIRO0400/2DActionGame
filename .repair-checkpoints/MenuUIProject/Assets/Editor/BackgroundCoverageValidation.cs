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

public static class BackgroundCoverageValidation
{
    public static void Diagnose() => Start(false);
    public static void Run() => Start(true);
    static void Start(bool assertCoverage)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage1.unity");
        EditorApplication.delayCall += () => Capture(assertCoverage);
    }
    static void Capture(bool assertCoverage)
    {
        var report = new List<string>();
        int uncovered = 0;
        try
        {
            var loop = UnityEngine.Object.FindFirstObjectByType<BackgroundLooper>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var cameraTransform = (Transform)typeof(BackgroundLooper).GetField("cameraTransform", flags).GetValue(loop);
            var camera = cameraTransform.GetComponent<Camera>();
            var brain = camera.GetComponent<CinemachineBrain>();
            var panels = loop.GetComponentsInChildren<ParallaxBackground>();
            foreach (var panel in panels) typeof(ParallaxBackground).GetMethod("Awake", flags).Invoke(panel, null);
            var callback = typeof(BackgroundLooper).GetMethod("OnCameraUpdated", flags);
            var target = new RenderTexture(960, 540, 24);
            target.Create();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.magenta;
            camera.aspect = 960f / 540f;
            string folder = "StageAuthoringReports/BackgroundCoverage/" + (assertCoverage ? "After" : "Before");
            Directory.CreateDirectory(folder);
            foreach (var point in new[] { new Vector2(2.5f, 1f), new Vector2(25f, 6f), new Vector2(45f, 1f), new Vector2(70f, 8f), new Vector2(100f, 3f), new Vector2(150f, 1f), new Vector2(-80f, 1f), new Vector2(45f, -16f) })
            {
                cameraTransform.position = new Vector3(point.x, point.y, cameraTransform.position.z);
                callback.Invoke(loop, new object[] { brain });
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                image.Apply();
                int count = 0;
                foreach (var pixel in image.GetPixels32()) if (pixel.r > 200 && pixel.b > 200 && pixel.g < 80) count++;
                uncovered += count;
                report.Add("Camera " + point + ": exposed clear-color pixels=" + count);
                foreach (var panel in panels)
                    foreach (var sprite in panel.GetComponentsInChildren<SpriteRenderer>())
                        if (sprite.name == "Far") report.Add(panel.name + " sky bounds=" + sprite.bounds + ", draw mode=" + sprite.drawMode);
                File.WriteAllBytes(folder + "/x" + point.x + "_y" + point.y + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            RenderTexture.active = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            report.Add((uncovered == 0 ? "PASS" : "FAIL") + ": no clear-color holes across rendered views; exposed pixels=" + uncovered);
            File.WriteAllLines(folder + "/result.txt", report);
            EditorApplication.Exit(assertCoverage && uncovered != 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            File.WriteAllText("StageAuthoringReports/background-coverage-failure.txt", e.ToString());
            EditorApplication.Exit(1);
        }
    }
}

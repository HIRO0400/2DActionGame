using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class StageAuthoringPreview
{
    public static void Run()
    {
        EditorSceneManager.OpenScene(StageAuthoringBootstrap.ScenePath);
        EditorApplication.delayCall += Capture;
    }
    static void Capture()
    {
        try
        {
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var texture = new RenderTexture(1280, 720, 24);
            texture.Create();
            foreach (var point in new[] { new Vector2(4f, 1.5f), new Vector2(21f, 3f), new Vector2(44f, 1f) })
            {
                camera.transform.position = new Vector3(point.x, point.y, -10f);
                camera.aspect = 1280f / 720;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes("StageAuthoringReports/Stage1_View_" + point.x + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            RenderTexture.active = null;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.WriteAllText("StageAuthoringReports/preview-failure.txt", e.ToString());
            EditorApplication.Exit(1);
        }
    }
}

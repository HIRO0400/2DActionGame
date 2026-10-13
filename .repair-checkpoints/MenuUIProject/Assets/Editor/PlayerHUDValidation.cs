using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PlayerHUDValidation
{
    static PlayerHUDValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("HUDTest", false))
            {
                SessionState.SetBool("HUDTest", false);
                new GameObject("HUDValidation").AddComponent<PlayerHUDTestRunner>();
            }
        };
    }
    public static void Run()
    {
        for (int n = 1; n <= 5; n++)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage" + n + ".unity");
            if (UnityEngine.Object.FindFirstObjectByType<Player>().GetComponent<PlayerHUD>() == null)
                throw new Exception("Missing HUD in Stage" + n);
        }
        SessionState.SetBool("HUDTest", true);
        EditorApplication.EnterPlaymode();
    }
}

public class PlayerHUDTestRunner : MonoBehaviour
{
    readonly List<string> report = new List<string>();
    void Check(bool ok, string message)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + message);
        File.WriteAllLines("StageAuthoringReports/hud-validation.txt", report);
        if (!ok) { EditorApplication.Exit(1); throw new Exception(message); }
    }
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        var player = FindFirstObjectByType<Player>();
        var hud = player.GetComponent<PlayerHUD>();
        Check(hud.CapacityLabel == "×1", "Initial capacity ×1; HUD present in all five scenes");
        var canvas = GameObject.Find("PlayerHUD").GetComponent<Canvas>();
        Check(canvas.renderMode == RenderMode.ScreenSpaceOverlay, "HUD independent of camera motion");
        Check(canvas.GetComponent<GraphicRaycaster>() == null, "HUD does not intercept mouse input");
        Check(canvas.transform.Find("CloneCapacity/CloneIcon").GetComponent<Image>().sprite != null, "Existing clone sprite assigned");
        var body = player.GetComponent<Rigidbody2D>();
        body.position = new Vector2(6, 0.9f);
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.5f);
        Check(player.CloneCapacity == 2 && hud.CapacityLabel == "×2", "Actual coin trigger increases capacity and displayed number");
        var camera = Camera.main;
        // Render an equivalent camera canvas for image QA; the shipped HUD remains overlay.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var target = new RenderTexture(1280, 720, 24);
        target.Create();
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        yield return null;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        texture.Apply();
        File.WriteAllBytes("StageAuthoringReports/hud-preview.png", texture.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        target.Release(); Destroy(target); Destroy(texture);
        Check(true, "HUD preview image generated");
        EditorApplication.Exit(0);
    }
}

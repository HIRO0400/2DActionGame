using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MenuScreenValidation
{
    static MenuScreenValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MenuUI.Test", false))
            { SessionState.SetBool("MenuUI.Test", false); new GameObject("MenuUIValidation").AddComponent<MenuScreenTest>(); }
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("StageAuthoringReports/MenuUI");
        SessionState.SetString("MenuUI.Prefs", JsonUtility.ToJson(ReleaseUIPrefs.Capture()));
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/TitleScene.unity");
        SessionState.SetBool("MenuUI.Test", true); EditorApplication.EnterPlaymode();
    }
}
public class MenuScreenTest : MonoBehaviour
{
    readonly List<string> report = new List<string>(); Mouse mouse; Keyboard keyboard; bool finished;
    void Check(bool ok, string message)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + message);
        File.WriteAllLines("StageAuthoringReports/MenuUI/test.txt", report);
        if (!ok) { Finish(false); throw new Exception(message); }
    }
    void Finish(bool ok)
    {
        if (finished) return; finished = true;
        JsonUtility.FromJson<ReleaseUIPrefs>(SessionState.GetString("MenuUI.Prefs", "{}")).Restore();
        EditorApplication.Exit(ok ? 0 : 1);
    }
    void Update() { if (!finished && Time.realtimeSinceStartup > 120) Check(false, "Timeout"); }
    IEnumerator Click(string name)
    {
        GameObject.Find("MenuEventSystem").GetComponent<InputSystemUIInputModule>().actionsAsset.devices = new InputDevice[] { mouse, keyboard };
        Canvas.ForceUpdateCanvases(); var button = GameObject.Find(name).GetComponent<Button>();
        Check(button != null && button.interactable, name + " available");
        var rect = button.GetComponent<RectTransform>(); var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, true));
        yield return new WaitForSecondsRealtime(.1f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, false));
        yield return new WaitForSecondsRealtime(.4f);
    }
    IEnumerator Capture(string name, int width, int height)
    {
        Screen.SetResolution(width, height, false); yield return new WaitForSecondsRealtime(.2f);
        var camera = Camera.main; var target = new RenderTexture(width, height, 24); target.Create();
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
        camera.targetTexture = target; Canvas.ForceUpdateCanvases(); yield return null;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target; var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        File.WriteAllBytes("StageAuthoringReports/MenuUI/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null;
        foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
        target.Release(); Destroy(target); Destroy(texture); yield return null; Canvas.ForceUpdateCanvases(); yield return null;
        var rectangles = new List<Rect>();
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (GameMenus.IsPaused && !button.transform.IsChildOf(GameObject.Find("ModalBlocker").transform)) continue;
            var corners = new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(corners);
            var rect = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            var rootCorners = new Vector3[4];
            button.GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>().GetWorldCorners(rootCorners);
            var viewport = Rect.MinMaxRect(rootCorners[0].x, rootCorners[0].y, rootCorners[2].x, rootCorners[2].y);
            Check(rect.xMin >= viewport.xMin - 1 && rect.yMin >= viewport.yMin - 1 && rect.xMax <= viewport.xMax + 1 && rect.yMax <= viewport.yMax + 1, name + ": " + button.name + " inside screen " + rect + " / " + viewport);
            foreach (var other in rectangles) Check(!rect.Overlaps(other), name + ": buttons do not overlap");
            rectangles.Add(rect);
        }
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
        yield return new WaitForSecondsRealtime(.5f);
        ProgressStore.ResetProgress(); ProgressStore.CompleteStage(1, "Stage2");
        yield return Capture("title-1280", 1280, 720);
        yield return Click("HowToPlayButton"); Check(GameMenus.IsPaused && GameObject.Find("Instructions") != null, "How-to-play opens");
        yield return Capture("how-to-play", 1280, 720);
        yield return Click("CloseHowToPlay"); Check(!GameMenus.IsPaused, "How-to-play closes");
        yield return Click("MenuOpen"); yield return Click("CloseSettings"); Check(Time.timeScale == 1, "Title settings work");
        yield return Click("ContinueButton"); Check(SceneManager.GetActiveScene().name == "Stage2", "Continue loads highest unlocked stage");
        GameMenus.Instance.Navigate("TitleScene"); yield return new WaitForSecondsRealtime(.3f);
        yield return Click("StartButton"); Check(SceneManager.GetActiveScene().name == "StageSelectScene", "Title opens selection");
        Check(!GameObject.Find("Stage3").GetComponent<Button>().interactable && GameObject.Find("Stage6") == null, "Locked stage disabled and absent stage hidden");
        yield return Capture("selection-1280", 1280, 720);
        yield return Capture("selection-800", 800, 600);
        yield return Click("Stage2"); Check(SceneManager.GetActiveScene().name == "Stage2", "Stage card loads stage");
        GameMenus.Instance.Navigate("StageSelectScene"); yield return new WaitForSecondsRealtime(.3f);
        yield return Click("Back"); Check(SceneManager.GetActiveScene().name == "TitleScene", "Back returns to title");
        yield return Capture("title-800", 800, 600);
        Finish(true);
    }
}

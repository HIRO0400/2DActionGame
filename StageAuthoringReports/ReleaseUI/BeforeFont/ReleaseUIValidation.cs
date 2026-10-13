using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[Serializable] public class ReleaseUIPrefs
{
    public List<ReleaseUIPref> values = new List<ReleaseUIPref>();
    public static ReleaseUIPrefs Capture()
    {
        var result = new ReleaseUIPrefs();
        var keys = new List<string> { "UnlockedStage", "Progress.Version", "Settings.BGM", "Settings.SFX", "Settings.Muted" };
        for (int i = 1; i <= 10; i++) keys.Add("Progress.Cleared." + i);
        foreach (var key in keys) result.values.Add(new ReleaseUIPref { key = key, exists = PlayerPrefs.HasKey(key), isFloat = key == "Settings.BGM" || key == "Settings.SFX", number = PlayerPrefs.GetFloat(key), integer = PlayerPrefs.GetInt(key) });
        return result;
    }
    public void Restore()
    {
        foreach (var value in values)
            if (!value.exists) PlayerPrefs.DeleteKey(value.key); else if (value.isFloat) PlayerPrefs.SetFloat(value.key, value.number); else PlayerPrefs.SetInt(value.key, value.integer);
        PlayerPrefs.Save();
    }
}
[Serializable] public class ReleaseUIPref { public string key; public bool exists, isFloat; public int integer; public float number; }

[InitializeOnLoad]
public static class ReleaseUIValidation
{
    static ReleaseUIValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ReleaseUI.Run", false))
            {
                SessionState.SetBool("ReleaseUI.Run", false);
                new GameObject("ReleaseUITest").AddComponent<ReleaseUITestRunner>();
            }
        };
    }
    public static void Pause() => Begin(1);
    public static void Begin(int phase)
    {
        SessionState.SetInt("ReleaseUI.Phase", phase);
        SessionState.SetString("ReleaseUI.Prefs", JsonUtility.ToJson(ReleaseUIPrefs.Capture()));
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage1.unity");
        SessionState.SetBool("ReleaseUI.Run", true);
        EditorApplication.EnterPlaymode();
    }
}

public class ReleaseUITestRunner : MonoBehaviour
{
    readonly List<string> report = new List<string>();
    Mouse mouse; Keyboard keyboard;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
    InputSettings.UpdateMode updateMode;
    bool finished;
    int phase;
    void Check(bool ok, string text)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + text);
        File.WriteAllLines("StageAuthoringReports/ReleaseUI/phase" + phase + "-test.txt", report);
        if (!ok) { Finish(false); throw new Exception(text); }
    }
    void Finish(bool ok)
    {
        if (finished) return; finished = true;
        JsonUtility.FromJson<ReleaseUIPrefs>(SessionState.GetString("ReleaseUI.Prefs", "{}")).Restore();
        InputSystem.settings.backgroundBehavior = background;
        InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        InputSystem.settings.updateMode = updateMode;
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        EditorApplication.Exit(ok ? 0 : 1);
    }
    IEnumerator Click(string name)
    {
        Canvas.ForceUpdateCanvases();
        var target = GameObject.Find(name);
        Check(target != null, "Button exists: " + name);
        var rect = target.GetComponent<RectTransform>();
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        Check(hits.Count > 0 && (hits[0].gameObject == target || hits[0].gameObject.transform.IsChildOf(target.transform)), "Pointer raycast reaches " + name);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, true));
        yield return new WaitForSecondsRealtime(0.08f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, false));
        yield return new WaitForSecondsRealtime(0.25f);
    }
    IEnumerator Escape()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        yield return new WaitForSecondsRealtime(0.08f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSecondsRealtime(0.25f);
    }
    IEnumerator Slide(string name, float value)
    {
        Canvas.ForceUpdateCanvases();
        var rect = GameObject.Find(name).GetComponent<RectTransform>();
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(Mathf.Lerp(rect.rect.xMin + 12, rect.rect.xMax - 12, value), rect.rect.center.y)));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, true));
        yield return new WaitForSecondsRealtime(0.1f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, false));
        yield return new WaitForSecondsRealtime(0.2f);
    }
    IEnumerator Capture(string name)
    {
        var camera = Camera.main;
        var canvas = GameObject.Find("MenuCanvas").GetComponent<Canvas>();
        var target = new RenderTexture(1280, 720, 24); target.Create();
        var oldTarget = camera.targetTexture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases(); yield return null;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
        File.WriteAllBytes("StageAuthoringReports/ReleaseUI/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = oldTarget;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        target.Release(); Destroy(target); Destroy(texture);
        Canvas.ForceUpdateCanvases(); yield return null;
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        phase = SessionState.GetInt("ReleaseUI.Phase", 1);
        background = InputSystem.settings.backgroundBehavior; editorInput = InputSystem.settings.editorInputBehaviorInPlayMode; updateMode = InputSystem.settings.updateMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        Application.runInBackground = true;
        mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
        var module = GameObject.Find("MenuEventSystem").GetComponent<InputSystemUIInputModule>();
        module.actionsAsset.devices = new InputDevice[] { mouse, keyboard };
        yield return new WaitForSecondsRealtime(0.5f);
        var player = FindFirstObjectByType<Player>();
        player.GetComponent<PlayerInput>().actions.devices = new InputDevice[] { mouse, keyboard };
        yield return Click("MenuOpen");
        Check(GameMenus.IsPaused && Time.timeScale == 0 && AudioListener.pause, "Actual pointer click pauses time and audio");
        var position = player.transform.position;
        yield return new WaitForSecondsRealtime(0.3f);
        Check(Vector3.Distance(position, player.transform.position) < 0.001f, "Player stationary during pause");
        yield return Capture("pause");
        yield return Click("Resume");
        Check(!GameMenus.IsPaused && Time.timeScale == 1 && !AudioListener.pause, "Actual pointer click resumes");
        Check((int)typeof(Player).GetField("currentClones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player) == 0, "Resume click does not create an unintended clone");
        yield return Escape(); Check(GameMenus.IsPaused, "Escape opens pause through new Input System");
        yield return Escape(); Check(!GameMenus.IsPaused, "Escape resumes");
        yield return Click("MenuOpen"); yield return Click("Restart");
        yield return new WaitForSecondsRealtime(0.3f);
        Check(SceneManager.GetActiveScene().name == "Stage1" && Time.timeScale == 1, "Pointer restart resets scene and time");
        yield return Click("MenuOpen"); yield return Click("Select");
        Check(SceneManager.GetActiveScene().name == "StageSelectScene" && Time.timeScale == 1, "Pointer opens selection");
        GameMenus.Instance.Navigate("Stage1"); yield return new WaitForSecondsRealtime(0.5f);
        yield return Click("MenuOpen"); yield return Click("Title");
        Check(SceneManager.GetActiveScene().name == "TitleScene" && Time.timeScale == 1, "Pointer opens title");
        if (phase >= 2)
        {
            yield return Click("MenuOpen");
            yield return Slide("BGM", 0.7f); yield return Slide("SFX", 0.3f);
            Check(Mathf.Abs(AudioSettings.BGM - 0.7f) < 0.02f && Mathf.Abs(AudioSettings.SFX - 0.3f) < 0.02f, "Actual pointer controls independent volume sliders");
            yield return Capture("settings");
            yield return Click("Mute");
            ReleaseAssets.Current.audioMixer.GetFloat("BGMVolume", out float db);
            Check(AudioSettings.Muted && db == -80, "Pointer toggles mute");
            yield return Click("Mute"); yield return Slide("BGM", 0);
            ReleaseAssets.Current.audioMixer.GetFloat("BGMVolume", out db);
            Check(db == -80 && !float.IsInfinity(AudioSettings.Decibels(0)), "Zero volume is silent without invalid logarithm");
            yield return Slide("BGM", 0.7f); yield return Click("CloseSettings");
            AudioSettings.Reload();
            Check(!GameMenus.IsPaused && Mathf.Abs(AudioSettings.BGM - 0.7f) < 0.02f, "Close saves volume and restores time");
        }
        Finish(true);
    }
}

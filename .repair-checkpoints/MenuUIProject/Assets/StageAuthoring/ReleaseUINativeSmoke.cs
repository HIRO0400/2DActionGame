#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runs only in a development build explicitly launched with --release-ui-smoke.
public class ReleaseUINativeSmoke : MonoBehaviour
{
    class Pref { public string key; public bool exists, floating; public int integer; public float number; }
    readonly List<Pref> original = new List<Pref>();
    readonly List<string> report = new List<string>();
    Mouse mouse; Keyboard keyboard; bool finished;
    string Folder => Path.Combine(Environment.CurrentDirectory, "StageAuthoringReports/ReleaseUI");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--release-ui-smoke") >= 0)
            new GameObject("NativeUISmoke").AddComponent<ReleaseUINativeSmoke>();
    }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Folder);
        var keys = new List<string> { "UnlockedStage", "Progress.Version", "Settings.BGM", "Settings.SFX", "Settings.Muted" };
        for (int i = 1; i <= 10; i++) keys.Add("Progress.Cleared." + i);
        foreach (var key in keys) original.Add(new Pref { key = key, exists = PlayerPrefs.HasKey(key), floating = key == "Settings.BGM" || key == "Settings.SFX", integer = PlayerPrefs.GetInt(key), number = PlayerPrefs.GetFloat(key) });
        Application.logMessageReceived += Log;
    }
    void Log(string message, string stack, LogType type) { if (!finished && type == LogType.Exception) { report.Add("FAIL: " + message + "\n" + stack); Finish(false); } }
    void Update() { if (!finished && Time.realtimeSinceStartup > 180) { report.Add("FAIL: native smoke timeout"); Finish(false); } }
    void Check(bool ok, string text)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + text);
        File.WriteAllLines(Path.Combine(Folder, "native-test.txt"), report);
        if (!ok) { Finish(false); throw new Exception(text); }
    }
    void Finish(bool ok)
    {
        if (finished) return; finished = true;
        foreach (var value in original) if (!value.exists) PlayerPrefs.DeleteKey(value.key); else if (value.floating) PlayerPrefs.SetFloat(value.key, value.number); else PlayerPrefs.SetInt(value.key, value.integer);
        PlayerPrefs.Save();
        File.WriteAllLines(Path.Combine(Folder, "native-test.txt"), report);
        Application.Quit(ok ? 0 : 1);
    }
    void Bind()
    {
        GameObject.Find("MenuEventSystem").GetComponent<InputSystemUIInputModule>().actionsAsset.devices = new InputDevice[] { mouse, keyboard };
        var player = FindFirstObjectByType<Player>();
        if (player != null) player.GetComponent<PlayerInput>().actions.devices = new InputDevice[] { mouse, keyboard };
    }
    GameObject Widget(string name)
    {
        foreach (var child in GameObject.Find("MenuCanvas").GetComponentsInChildren<Transform>()) if (child.name == name) return child.gameObject;
        throw new Exception("Missing widget: " + name);
    }
    IEnumerator Click(string name)
    {
        Bind(); Canvas.ForceUpdateCanvases();
        var rect = Widget(name).GetComponent<RectTransform>();
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, true));
        yield return new WaitForSecondsRealtime(0.1f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, false));
        yield return new WaitForSecondsRealtime(0.3f);
    }
    IEnumerator Capture()
    {
        var camera = Camera.main;
        var canvases = new List<Canvas>();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) { canvases.Add(canvas); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
        var target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
        Canvas.ForceUpdateCanvases(); yield return null;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
        File.WriteAllBytes(Path.Combine(Folder, "native-pause.png"), image.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null;
        foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
        target.Release(); Destroy(target); Destroy(image); Canvas.ForceUpdateCanvases(); yield return null;
    }
    IEnumerator Start()
    {
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        mouse = InputSystem.AddDevice<Mouse>(); keyboard = InputSystem.AddDevice<Keyboard>();
        yield return new WaitForSecondsRealtime(0.7f);
        Check(SceneManager.GetActiveScene().name == "TitleScene", "Native build starts at title");
        yield return Click("MenuOpen");
        Check(GameMenus.IsPaused && Widget("BGM") != null, "Native pointer opens volume settings");
        yield return Click("Mute"); Check(AudioSettings.Muted, "Native pointer toggles mute");
        yield return Click("Mute"); yield return Click("CloseSettings");
        Check(Time.timeScale == 1, "Native settings close restores time");
        GameMenus.Instance.Navigate("Stage1"); yield return new WaitForSecondsRealtime(0.7f); Bind();
        var player = FindFirstObjectByType<Player>();
        Coin coin = null; foreach (var candidate in FindObjectsByType<Coin>(FindObjectsSortMode.None)) if (coin == null || candidate.transform.position.x < coin.transform.position.x) coin = candidate;
        var body = player.GetComponent<Rigidbody2D>(); body.position = coin.transform.position; Physics2D.SyncTransforms();
        yield return new WaitForSecondsRealtime(0.4f);
        Check(player.CloneCapacity == 2 && player.GetComponent<PlayerHUD>().CapacityLabel == "×2", "Native coin pickup updates clone HUD");
        yield return Click("MenuOpen");
        Check(GameMenus.IsPaused && Time.timeScale == 0, "Native pointer pauses gameplay");
        Check(UIFont.Font.HasCharacter('設') && UIFont.Font.HasCharacter('開'), "Native bundled Japanese font has glyphs");
        yield return Capture();
        yield return Click("Resume"); Check(Time.timeScale == 1 && !GameMenus.IsPaused, "Native pointer resumes gameplay");
        body.position = FindFirstObjectByType<Goal>().transform.position; Physics2D.SyncTransforms();
        yield return new WaitForSecondsRealtime(0.5f);
        Check(GameMenus.Instance.IsClearVisible && ProgressStore.IsCleared(1), "Native goal shows clear screen and saves record");
        yield return Click("NextStage");
        Check(SceneManager.GetActiveScene().name == "Stage2", "Native next-stage button loads build scene");
        GameMenus.Instance.Navigate("Stage5"); yield return new WaitForSecondsRealtime(0.7f);
        body = FindFirstObjectByType<Player>().GetComponent<Rigidbody2D>(); body.position = FindFirstObjectByType<Goal>().transform.position; Physics2D.SyncTransforms();
        yield return new WaitForSecondsRealtime(0.5f);
        Check(GameMenus.Instance.IsClearVisible && ProgressStore.IsCleared(5), "Native final clear works");
        yield return Click("ClearSelect");
        Check(SceneManager.GetActiveScene().name == "StageSelectScene" && Time.timeScale == 1, "Native final clear returns to selection");
        yield return Click("ResetProgress"); yield return Click("CancelReset");
        Check(ProgressStore.IsCleared(5), "Native reset cancel preserves record");
        yield return Click("ResetProgress"); yield return Click("ConfirmReset");
        Check(ProgressStore.UnlockedStage == 1 && !ProgressStore.IsCleared(5), "Native reset confirm clears progress");
        Finish(true);
    }
}
#endif

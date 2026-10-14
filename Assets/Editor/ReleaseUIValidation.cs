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
    public static void Save() => Begin(4);
    public static void Clear() => Begin(5);
    public static void Begin(int phase)
    {
        Directory.CreateDirectory("StageAuthoringReports/ReleaseUI");
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
    void Update() { if (!finished && Time.realtimeSinceStartup > 120) Check(false, "UI test timeout"); }
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
        GameObject target = null;
        foreach (var child in GameObject.Find("MenuCanvas").GetComponentsInChildren<Transform>()) if (child.name == name) { target = child.gameObject; break; }
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
        RectTransform rect = null;
        foreach (var child in GameObject.Find("MenuCanvas").GetComponentsInChildren<RectTransform>()) if (child.name == name) { rect = child; break; }
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
        var sceneCanvases = new List<Canvas>();
        if (name == "selection" || name == "controls") foreach (var other in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (other != canvas && other.renderMode == RenderMode.ScreenSpaceOverlay) sceneCanvases.Add(other);
        var target = new RenderTexture(1280, 720, 24); target.Create();
        var oldTarget = camera.targetTexture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        camera.targetTexture = target;
        foreach (var other in sceneCanvases) { other.renderMode = RenderMode.ScreenSpaceCamera; other.worldCamera = camera; other.planeDistance = 1; }
        Canvas.ForceUpdateCanvases(); yield return null;
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
        File.WriteAllBytes("StageAuthoringReports/ReleaseUI/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = oldTarget;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        foreach (var other in sceneCanvases) { other.renderMode = RenderMode.ScreenSpaceOverlay; other.worldCamera = null; }
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
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        yield return new WaitForSecondsRealtime(0.12f);
        Check(player.GetComponent<Rigidbody2D>().linearVelocity.y > 0, "Actual keyboard jump starts before pause");
        yield return Click("MenuOpen");
        Check(GameMenus.IsPaused && Time.timeScale == 0 && AudioListener.pause, "Actual pointer click pauses time and audio");
        var position = player.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSecondsRealtime(0.3f);
        Check(Vector3.Distance(position, player.transform.position) < 0.001f, "Player stationary during pause");
        Check(!(bool)typeof(Player).GetField("jumpHeld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player), "Jump-key release during pause is retained");
        yield return Capture("pause");
        yield return Click("Resume");
        Check(!GameMenus.IsPaused && Time.timeScale == 1 && !AudioListener.pause, "Actual pointer click resumes");
        Check((int)typeof(Player).GetField("currentClones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(player) == 0, "Resume click does not create an unintended clone");
        var controls=GameObject.Find("PlayerHUD/Controls").transform;
        var move=controls.Find("Move").GetComponent<RectTransform>();
        var jump=controls.Find("Jump").GetComponent<RectTransform>();
        var cloneLabel=controls.Find("Clone").GetComponent<RectTransform>();
        Check(move.anchoredPosition.x<jump.anchoredPosition.x && jump.anchoredPosition.x<cloneLabel.anchoredPosition.x &&
            Mathf.Approximately(move.anchoredPosition.y,jump.anchoredPosition.y) && Mathf.Approximately(jump.anchoredPosition.y,cloneLabel.anchoredPosition.y),"Controls displayed horizontally");
        yield return Capture("controls");
        yield return new WaitForSecondsRealtime(1f);
        var cloneCount=typeof(Player).GetField("currentClones",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(1.5f);
        InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width*.5f,Screen.height*.5f)}.WithButton(MouseButton.Left,true).WithButton(MouseButton.Right,true));
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==0,"Mouse buttons no longer create clones");
        InputSystem.QueueStateEvent(mouse,new MouseState());
        EventSystem.current.SetSelectedGameObject(GameObject.Find("MenuCanvas/MenuOpen"));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==1 && !GameMenus.IsPaused,"Enter press creates clone without submitting selected pause button");
        yield return new WaitForSecondsRealtime(1.8f);
        Check((int)cloneCount.GetValue(player)==1,"Held Enter does not repeat clone creation");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==1,"Enter release does not create clone");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==1,"Standing on clone still prevents generation without jumping");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.1f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space,Key.Enter));
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==2,"Simultaneous Space and Enter creates clone on takeoff");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(1.5f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
        yield return new WaitForSecondsRealtime(.02f);
        yield return new WaitForFixedUpdate();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space,Key.Enter));
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==3,"Enter immediately after jumping creates clone");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(1.5f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
        yield return new WaitForSecondsRealtime(.2f);
        Check(player.GetComponent<PlayerInput>().actions["Jump"].IsPressed(),"Space remains held before Enter");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space,Key.Enter));
        yield return new WaitForSecondsRealtime(.1f);
        Check((int)cloneCount.GetValue(player)==4,"Enter creates clone while Space remains held");
        yield return new WaitForSecondsRealtime(1.8f);
        Check((int)cloneCount.GetValue(player)==4,"Held Space and Enter do not repeat creation");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.1f);
        yield return Escape(); Check(GameMenus.IsPaused,"Escape opens pause");
        EventSystem.current.SetSelectedGameObject(GameObject.Find("MenuCanvas/ModalBlocker/MenuPanel/Resume"));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));
        yield return new WaitForSecondsRealtime(.1f);
        Check(!GameMenus.IsPaused && (int)cloneCount.GetValue(player)==4,"Enter submits Resume without creating clone");
        yield return new WaitForSecondsRealtime(.3f);
        Check((int)cloneCount.GetValue(player)==4,"Held menu-submit Enter does not leak into gameplay");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSecondsRealtime(.1f);
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
        if (phase >= 3)
        {
            Check(ReleaseAssets.Current.japaneseFont != null, "Japanese font bundled with runtime resources");
            bool glyphs = true;
            foreach (char c in "操作方法移動ジャンプクローン作成音量設定保存再開選択完了×←→") glyphs &= UIFont.Font.HasCharacter(c);
            Check(glyphs, "Bundled font contains all Japanese HUD and menu glyphs");
            bool tmpGlyphs = true;
            foreach (char character in "ステージ選択クリア済み") tmpGlyphs &= UIFont.TMPFont.HasCharacter(character, false, true);
            Check(tmpGlyphs, "TextMeshPro contains or creates Japanese glyphs from bundled font");
            foreach (var text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None)) Check(text.font == UIFont.Font, "Menu text uses bundled font");
        }
        if (phase >= 4)
        {
            PlayerPrefs.SetInt("UnlockedStage", -5);
            Check(ProgressStore.UnlockedStage == 1, "Invalid negative progress repaired");
            PlayerPrefs.SetInt("UnlockedStage", 999);
            Check(ProgressStore.UnlockedStage == 5, "Unlocks limited to existing stages");
            PlayerPrefs.DeleteKey("Progress.Version"); PlayerPrefs.SetInt("UnlockedStage", 3);
            ProgressStore.Initialize();
            Check(ProgressStore.IsCleared(1) && ProgressStore.IsCleared(2), "Existing unlocks migrate to clear records");
            ProgressStore.ResetProgress(); ProgressStore.CompleteStage(1, "Stage2");
            Check(ProgressStore.UnlockedStage == 2 && ProgressStore.IsCleared(1), "Goal progress explicitly saved");
            yield return Click("ResetProgress");
            yield return Capture("reset-confirmation");
            yield return Click("CancelReset");
            Check(ProgressStore.UnlockedStage == 2, "Pointer cancel preserves progress");
            float volume = AudioSettings.BGM;
            yield return Click("ResetProgress"); yield return Click("ConfirmReset");
            Check(ProgressStore.UnlockedStage == 1 && !ProgressStore.IsCleared(1) && Mathf.Approximately(AudioSettings.BGM, volume), "Pointer confirm resets only progress");
        }
        if (phase >= 5)
        {
            GameMenus.Instance.Navigate("Stage1"); yield return new WaitForSecondsRealtime(0.5f);
            player = FindFirstObjectByType<Player>();
            var body = player.GetComponent<Rigidbody2D>();
            body.position = FindFirstObjectByType<Goal>().transform.position; Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(1.2f);
            Check(GameMenus.Instance.IsClearVisible && Time.timeScale == 0 && SceneManager.GetActiveScene().name == "Stage1", "Actual goal waits on clear screen");
            Check(!player.enabled && !body.simulated && ProgressStore.UnlockedStage == 2, "Clear freezes player and saves next unlock");
            yield return Capture("clear");
            yield return Click("NextStage");
            Check(SceneManager.GetActiveScene().name == "Stage2" && Time.timeScale == 1, "Actual pointer next-stage button works");
            GameMenus.Instance.Navigate("StageSelectScene"); yield return new WaitForSecondsRealtime(0.5f);
            var select = FindFirstObjectByType<StageSelectManager>();
            var buttons = (UnityEngine.UI.Button[])typeof(StageSelectManager).GetField("stageButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(select);
            Check(buttons[0].interactable && buttons[1].interactable && !buttons[2].interactable, "Stage selection respects unlocks");
            Check(buttons[0].GetComponentInChildren<TMPro.TMP_Text>().text.Contains("クリア済み") && !buttons[5].gameObject.activeSelf, "Clear status shown; unbuilt Stage6 hidden");
            yield return Capture("selection");
            yield return ClickSceneButton(buttons[1]);
            Check(SceneManager.GetActiveScene().name == "Stage2", "Actual pointer stage-selection button works");
            GameMenus.Instance.Navigate("Stage5"); yield return new WaitForSecondsRealtime(0.5f);
            body = FindFirstObjectByType<Player>().GetComponent<Rigidbody2D>();
            body.position = FindFirstObjectByType<Goal>().transform.position; Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.5f);
            Check(GameMenus.Instance.IsClearVisible && ProgressStore.IsCleared(5), "Final-stage completion screen and record work");
            yield return Capture("completed");
            yield return Click("ClearSelect");
            Check(SceneManager.GetActiveScene().name == "StageSelectScene" && Time.timeScale == 1, "Actual pointer final-stage return works");
        }
        Finish(true);
    }
    IEnumerator ClickSceneButton(UnityEngine.UI.Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = button.GetComponent<RectTransform>();
        var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, true));
        yield return new WaitForSecondsRealtime(0.08f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, false));
        yield return new WaitForSecondsRealtime(0.3f);
    }
}

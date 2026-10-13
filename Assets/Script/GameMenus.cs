using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runtime uGUI menus driven by the new Input System.</summary>
public class GameMenus : MonoBehaviour
{
    public static GameMenus Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.paused;
    public static bool BlocksGameplay => IsPaused || (Instance != null && Time.unscaledTime < Instance.inputResumeAt);
    public bool IsClearVisible { get; private set; }
    bool paused;
    float previousScale, inputResumeAt;
    bool previousAudioPause;
    string nextStage;
    Font font;
    Canvas canvas;
    GameObject shade;
    RectTransform content;
    Button topButton;
    Button resetButton;
    EventSystem events;
    InputSystemUIInputModule module;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install() { if (Instance == null) new GameObject("GameMenus").AddComponent<GameMenus>(); }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        font = UIFont.Font;
        var input = new GameObject("MenuEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        input.transform.SetParent(transform, false);
        events = input.GetComponent<EventSystem>();
        module = input.GetComponent<InputSystemUIInputModule>();
        events.enabled = module.enabled = false;
        module.AssignDefaultActions();
        var root = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        topButton = MakeButton(root.transform, "MenuOpen", "ポーズ (Esc)", Pause);
        var topRect = topButton.GetComponent<RectTransform>();
        topRect.anchorMin = topRect.anchorMax = topRect.pivot = Vector2.one;
        topRect.anchoredPosition = new Vector2(-16, -16);
        topRect.sizeDelta = new Vector2(196, 48);
        resetButton = MakeButton(root.transform, "ResetProgress", "進行データの初期化", ConfirmReset);
        var resetRect = resetButton.GetComponent<RectTransform>();
        resetRect.anchorMin = resetRect.anchorMax = resetRect.pivot = Vector2.zero;
        resetRect.anchoredPosition = new Vector2(16, 16); resetRect.sizeDelta = new Vector2(260, 48);
        shade = new GameObject("ModalBlocker", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(root.transform, false);
        Stretch(shade.GetComponent<RectTransform>());
        shade.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        var panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(shade.transform, false);
        content = panel.GetComponent<RectTransform>();
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(480, 440);
        panel.GetComponent<Image>().color = new Color(0.06f, 0.1f, 0.16f, 1);
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        shade.SetActive(false);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Resume();
        events.enabled = module.enabled = false;
        foreach (var other in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (other == events) continue;
            foreach (var input in other.GetComponents<BaseInputModule>()) input.enabled = false;
            other.enabled = false;
        }
        events.enabled = module.enabled = true;
        bool inStage = FindFirstObjectByType<StageManager>() != null;
        topButton.GetComponentInChildren<Text>().text = inStage ? "ポーズ (Esc)" : "設定";
        topButton.onClick.RemoveAllListeners();
        topButton.onClick.AddListener(() => { if (inStage) Pause(); else OpenSettings(); });
        resetButton.gameObject.SetActive(!inStage);
        UIFont.Apply(scene);
    }
    void Start() { AudioSettings.Reload(); ProgressStore.Initialize(); }
    void OnApplicationQuit() => AudioSettings.Save();
    void Update()
    {
        if (!IsClearVisible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (paused) Resume(); else if (FindFirstObjectByType<StageManager>() != null) Pause();
        }
    }
    void StopGame()
    {
        if (!paused) { previousScale = Time.timeScale; previousAudioPause = AudioListener.pause; paused = true; }
        Time.timeScale = 0;
        AudioListener.pause = true;
        shade.SetActive(true);
    }
    void Empty(string heading)
    {
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        Label(content, "MenuHeading", heading, 28, 56);
    }
    public void Pause()
    {
        StopGame(); Empty("ポーズ");
        MakeButton(content, "Resume", "再開", Resume);
        MakeButton(content, "Restart", "リスタート", () => Navigate(SceneManager.GetActiveScene().name));
        MakeButton(content, "Select", "ステージ選択へ", () => Navigate("StageSelectScene"));
        MakeButton(content, "Title", "タイトルへ", () => Navigate("TitleScene"));
        MakeButton(content, "Settings", "音量設定", OpenSettings);
    }
    public void OpenSettings()
    {
        StopGame(); Empty("音量設定");
        var bgm = Label(content, "BGMLabel", "BGM  " + Mathf.RoundToInt(AudioSettings.BGM * 100) + "%", 22, 32);
        MakeSlider("BGM", AudioSettings.BGM, value => { AudioSettings.SetBGM(value); bgm.text = "BGM  " + Mathf.RoundToInt(value * 100) + "%"; });
        var sfx = Label(content, "SFXLabel", "効果音  " + Mathf.RoundToInt(AudioSettings.SFX * 100) + "%", 22, 32);
        MakeSlider("SFX", AudioSettings.SFX, value => { AudioSettings.SetSFX(value); sfx.text = "効果音  " + Mathf.RoundToInt(value * 100) + "%"; });
        var mute = MakeButton(content, "Mute", AudioSettings.Muted ? "ミュート：ON" : "ミュート：OFF", () => {});
        mute.onClick.AddListener(() => { AudioSettings.SetMuted(!AudioSettings.Muted); mute.GetComponentInChildren<Text>().text = AudioSettings.Muted ? "ミュート：ON" : "ミュート：OFF"; });
        MakeButton(content, "CloseSettings", "保存して閉じる", Resume);
    }
    public void OpenHowToPlay()
    {
        StopGame(); Empty("遊び方");
        Label(content, "Instructions", "A / D・← / →：移動\nSpace：ジャンプ\n左クリックを離す：クローン作成\nEsc：ポーズ／再開", 20, 140);
        Label(content, "CloneHelp", "クローンは足場として使えます。\nコインを取ると作れる数が増えます。\n死亡時は最後に接地した地面付近へ戻ります。", 18, 110);
        MakeButton(content, "CloseHowToPlay", "閉じる", Resume);
    }
    public void ConfirmReset()
    {
        StopGame(); Empty("進行データを初期化しますか？");
        Label(content, "ResetExplanation", "ステージ解放とクリア記録を削除します。\n音量設定は残ります。", 18, 72);
        MakeButton(content, "CancelReset", "キャンセル", Resume);
        MakeButton(content, "ConfirmReset", "初期化する", () => { ProgressStore.ResetProgress(); Navigate(SceneManager.GetActiveScene().name); });
    }
    Slider MakeSlider(string name, float value, UnityAction<float> changed)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider), typeof(LayoutElement));
        obj.transform.SetParent(content, false);
        obj.GetComponent<Image>().color = new Color(0.1f, 0.17f, 0.24f, 1);
        obj.GetComponent<LayoutElement>().preferredHeight = 36;
        var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
        track.transform.SetParent(obj.transform, false);
        var trackRect = track.GetComponent<RectTransform>(); Stretch(trackRect); trackRect.offsetMin = new Vector2(12, 12); trackRect.offsetMax = new Vector2(-12, -12);
        track.GetComponent<Image>().color = new Color(0.24f, 0.3f, 0.36f); track.GetComponent<Image>().raycastTarget = false;
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(track.transform, false);
        Stretch(fill.GetComponent<RectTransform>()); fill.GetComponent<Image>().color = new Color(0.3f, 0.72f, 0.9f); fill.GetComponent<Image>().raycastTarget = false;
        var area = new GameObject("HandleArea", typeof(RectTransform)); area.transform.SetParent(obj.transform, false);
        var areaRect = area.GetComponent<RectTransform>(); Stretch(areaRect); areaRect.offsetMin = new Vector2(12, 0); areaRect.offsetMax = new Vector2(-12, 0);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(area.transform, false);
        var handleRect = handle.GetComponent<RectTransform>(); handleRect.anchorMin = handleRect.anchorMax = new Vector2(0, 0.5f); handleRect.sizeDelta = new Vector2(24, -4);
        handle.GetComponent<Image>().color = new Color(0.9f, 0.95f, 1);
        var slider = obj.GetComponent<Slider>(); slider.targetGraphic = handle.GetComponent<Image>(); slider.fillRect = fill.GetComponent<RectTransform>(); slider.handleRect = handleRect;
        slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(value); slider.onValueChanged.AddListener(changed);
        return slider;
    }
    public void Resume()
    {
        AudioSettings.Save();
        if (paused) { Time.timeScale = previousScale; AudioListener.pause = previousAudioPause; paused = false; }
        IsClearVisible = false;
        shade.SetActive(false);
        inputResumeAt = Time.unscaledTime + 0.15f;
        events.SetSelectedGameObject(null);
    }
    public void Navigate(string scene) { Resume(); SceneManager.LoadScene(scene); }
    public void ShowClear(int stage, string next)
    {
        StopGame();
        nextStage = StageManager.CanLoadScene(next) ? next : "";
        IsClearVisible = true;
        Empty(string.IsNullOrEmpty(nextStage) ? "全ステージクリア！" : "ステージ " + stage + " クリア！");
        if (!string.IsNullOrEmpty(nextStage)) MakeButton(content, "NextStage", "次のステージへ", ContinueAfterClear);
        MakeButton(content, "ClearSelect", "ステージ選択へ", () => Navigate("StageSelectScene"));
        MakeButton(content, "ClearTitle", "タイトルへ", () => Navigate("TitleScene"));
        MakeButton(content, "Replay", "もう一度プレイ", () => Navigate(SceneManager.GetActiveScene().name));
    }
    public void ContinueAfterClear() => Navigate(string.IsNullOrEmpty(nextStage) ? "StageSelectScene" : nextStage);
    Text Label(Transform parent, string name, string value, int size, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = font; text.fontSize = size; text.text = value;
        text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        obj.GetComponent<LayoutElement>().preferredHeight = height;
        return text;
    }
    Button MakeButton(Transform parent, string name, string label, UnityAction action)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = new Color(0.15f, 0.28f, 0.4f, 1);
        obj.GetComponent<LayoutElement>().preferredHeight = 48;
        var button = obj.GetComponent<Button>();
        button.onClick.AddListener(action);
        var text = Label(obj.transform, "Label", label, 22, 48);
        Stretch(text.GetComponent<RectTransform>());
        return button;
    }
    void OnDestroy()
    {
        if (Instance != this) return;
        if (paused) { Time.timeScale = previousScale; AudioListener.pause = previousAudioPause; }
        SceneManager.sceneLoaded -= SceneLoaded;
        Instance = null;
    }
}

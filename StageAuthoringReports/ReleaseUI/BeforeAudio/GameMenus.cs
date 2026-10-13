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
    Font font;
    Canvas canvas;
    GameObject shade;
    RectTransform content;
    Button topButton;
    EventSystem events;
    InputSystemUIInputModule module;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install() { if (Instance == null) new GameObject("GameMenus").AddComponent<GameMenus>(); }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 24);
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
        topButton.gameObject.SetActive(FindFirstObjectByType<StageManager>() != null);
    }
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
    }
    public void Resume()
    {
        if (paused) { Time.timeScale = previousScale; AudioListener.pause = previousAudioPause; paused = false; }
        IsClearVisible = false;
        shade.SetActive(false);
        inputResumeAt = Time.unscaledTime + 0.15f;
        events.SetSelectedGameObject(null);
    }
    public void Navigate(string scene) { Resume(); SceneManager.LoadScene(scene); }
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
        if (font != null) Destroy(font);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameMenus : MonoBehaviour
{
    public static GameMenus Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.paused;
    public static bool BlocksGameplay => IsPaused || (Instance != null && Time.unscaledTime < Instance.inputResumeAt);
    bool paused;
    bool settings;
    EventSystem blockedEvents;
    float previousTimeScale;
    bool previousAudioPause;
    float inputResumeAt;
    GUIStyle heading, button;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (Instance == null) new GameObject("GameMenus").AddComponent<GameMenus>();
    }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneLoaded;
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Resume();
        UIFont.Apply(scene);
    }
    void Start() => AudioSettings.Reload();
    void OnApplicationQuit() => AudioSettings.Save();
    void OnDestroy()
    {
        if (Instance != this) return;
        Resume();
        SceneManager.sceneLoaded -= SceneLoaded;
        Instance = null;
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && FindFirstObjectByType<StageManager>() != null)
        {
            if (paused) Resume(); else Pause();
        }
    }
    public void Pause()
    {
        if (paused) return;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        paused = true;
        blockedEvents = EventSystem.current;
        if (blockedEvents != null && blockedEvents.enabled) blockedEvents.enabled = false;
        else blockedEvents = null;
        Time.timeScale = 0;
        AudioListener.pause = true;
    }
    public void Resume()
    {
        settings = false;
        AudioSettings.Save();
        if (blockedEvents != null) blockedEvents.enabled = true;
        blockedEvents = null;
        if (paused)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            paused = false;
        }
        inputResumeAt = Time.unscaledTime + 0.15f;
    }
    public void Navigate(string scene)
    {
        Resume();
        SceneManager.LoadScene(scene);
    }
    void OnGUI()
    {
        if (heading == null)
        {
            heading = new GUIStyle(GUI.skin.label) { font = UIFont.Font, fontSize = 28, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { font = UIFont.Font, fontSize = 22 };
        }
        var oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
        if (!paused)
        {
            if (GUI.Button(new Rect(1104, 16, 160, 44), "設定", button)) { Pause(); settings = true; }
            GUI.matrix = oldMatrix;
            return;
        }
        if (settings)
        {
            GUI.Box(new Rect(400, 155, 480, 410), GUIContent.none);
            GUI.Label(new Rect(420, 175, 440, 44), "音量設定", heading);
            GUI.Label(new Rect(440, 235, 400, 32), "BGM  " + Mathf.RoundToInt(AudioSettings.BGM * 100) + "%", heading);
            float bgm = GUI.HorizontalSlider(new Rect(460, 282, 360, 24), AudioSettings.BGM, 0, 1);
            if (!Mathf.Approximately(bgm, AudioSettings.BGM)) AudioSettings.SetBGM(bgm);
            GUI.Label(new Rect(440, 312, 400, 32), "効果音  " + Mathf.RoundToInt(AudioSettings.SFX * 100) + "%", heading);
            float sfx = GUI.HorizontalSlider(new Rect(460, 359, 360, 24), AudioSettings.SFX, 0, 1);
            if (!Mathf.Approximately(sfx, AudioSettings.SFX)) AudioSettings.SetSFX(sfx);
            if (GUI.Button(new Rect(440, 395, 400, 44), AudioSettings.Muted ? "ミュート：ON" : "ミュート：OFF", button)) AudioSettings.SetMuted(!AudioSettings.Muted);
            if (GUI.Button(new Rect(440, 463, 400, 48), "保存して閉じる", button)) Resume();
            GUI.matrix = oldMatrix;
            return;
        }
        GUI.Box(new Rect(400, 155, 480, 410), GUIContent.none);
        GUI.Label(new Rect(420, 175, 440, 44), "ポーズ", heading);
        if (GUI.Button(new Rect(440, 235, 400, 48), "再開", button)) Resume();
        if (GUI.Button(new Rect(440, 297, 400, 48), "リスタート", button)) Navigate(SceneManager.GetActiveScene().name);
        if (GUI.Button(new Rect(440, 359, 400, 48), "ステージ選択へ", button)) Navigate("StageSelectScene");
        if (GUI.Button(new Rect(440, 421, 400, 48), "タイトルへ", button)) Navigate("TitleScene");
        if (GUI.Button(new Rect(440, 489, 400, 48), "音量設定", button)) settings = true;
        GUI.matrix = oldMatrix;
    }
}

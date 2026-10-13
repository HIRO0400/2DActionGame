using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameMenus : MonoBehaviour
{
    public static GameMenus Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.paused;
    public static bool BlocksGameplay => IsPaused || (Instance != null && Time.unscaledTime < Instance.inputResumeAt);
    bool paused;
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
    void SceneLoaded(Scene scene, LoadSceneMode mode) => Resume();
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
        Time.timeScale = 0;
        AudioListener.pause = true;
    }
    public void Resume()
    {
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
        if (!paused) return;
        if (heading == null)
        {
            heading = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
        }
        var oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
        GUI.Box(new Rect(400, 155, 480, 410), GUIContent.none);
        GUI.Label(new Rect(420, 175, 440, 44), "ポーズ", heading);
        if (GUI.Button(new Rect(440, 235, 400, 48), "再開", button)) Resume();
        if (GUI.Button(new Rect(440, 297, 400, 48), "リスタート", button)) Navigate(SceneManager.GetActiveScene().name);
        if (GUI.Button(new Rect(440, 359, 400, 48), "ステージ選択へ", button)) Navigate("StageSelectScene");
        if (GUI.Button(new Rect(440, 421, 400, 48), "タイトルへ", button)) Navigate("TitleScene");
        GUI.Label(new Rect(440, 489, 400, 38), "Esc：ポーズ / 再開", button);
        GUI.matrix = oldMatrix;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [Header("ステージ情報")]
    [SerializeField] private int stageNumber = 1; // このステージ番号
    [SerializeField] private string nextStageName; // 次のシーン名

    private const string StageSelectSceneName = "StageSelectScene";

    private bool isCleared = false;

    private void Awake()
    {
        // Shared manager prefabs must use the stage actually being played.
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Stage") &&
            int.TryParse(sceneName.Substring(5), out int number) && number > 0)
        {
            stageNumber = number;
            string next = "Stage" + (number + 1);
            nextStageName = CanLoadScene(next) ? next : string.Empty;
        }
    }

    public static bool CanLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
#if UNITY_EDITOR
        // CanStreamedLevelBeLoaded does not report the build list reliably outside Play Mode.
        foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
        {
            if (scene.enabled && System.IO.Path.GetFileNameWithoutExtension(scene.path) == sceneName &&
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scene.path) != null)
                return true;
        }
        return false;
#else
        return Application.CanStreamedLevelBeLoaded(sceneName);
#endif
    }

    // ======================
    // ゴール到達
    // ======================
    public void OnStageClear()
    {
        if (isCleared) return;
        isCleared = true;

        UnlockNextStage();
        GameMenus.Instance.ShowClear(stageNumber, nextStageName);
    }

    // ======================
    // 次ステージ解放
    // ======================
    void UnlockNextStage()
    {
        ProgressStore.CompleteStage(stageNumber, nextStageName);
    }

    // ======================
    // 次ステージへ
    // ======================
    public void GoToNextStage()
    {
        GameMenus.Instance.Resume();
        if (!string.IsNullOrEmpty(nextStageName) &&
            CanLoadScene(nextStageName))
        {
            SceneManager.LoadScene(nextStageName);
        }
        else
        {
            SceneManager.LoadScene(StageSelectSceneName);
        }
    }

    // ======================
    // ステージ選択へ戻る
    // ======================
    public void BackToSelect()
    {
        SceneManager.LoadScene(StageSelectSceneName);
    }

    // ======================
    // リスタート
    // ======================
    public void RestartStage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

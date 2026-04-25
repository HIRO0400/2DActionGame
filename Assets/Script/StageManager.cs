using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [Header("ステージ情報")]
    public int stageNumber = 1; // このステージ番号
    public string nextStageName; // 次のシーン名

    private bool isCleared = false;

    // ======================
    // ゴール到達
    // ======================
    public void OnStageClear()
    {
        if (isCleared) return;
        isCleared = true;

        UnlockNextStage();
        Invoke(nameof(GoToNextStage), 1.0f); // 1秒後に遷移
    }

    // ======================
    // 次ステージ解放
    // ======================
    void UnlockNextStage()
    {
        int unlocked = PlayerPrefs.GetInt("UnlockedStage", 1);

        if (stageNumber >= unlocked)
        {
            PlayerPrefs.SetInt("UnlockedStage", stageNumber + 1);
        }
    }

    // ======================
    // 次ステージへ
    // ======================
    void GoToNextStage()
    {
        if (!string.IsNullOrEmpty(nextStageName))
        {
            SceneManager.LoadScene(nextStageName);
        }
        else
        {
            SceneManager.LoadScene("StageSelectScene");
        }
    }

    // ======================
    // ステージ選択へ戻る
    // ======================
    public void BackToSelect()
    {
        SceneManager.LoadScene("StageSelectScene");
    }

    // ======================
    // リスタート
    // ======================
    public void RestartStage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
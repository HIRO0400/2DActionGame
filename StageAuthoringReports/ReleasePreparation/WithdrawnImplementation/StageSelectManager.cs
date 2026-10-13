using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StageSelectManager : MonoBehaviour
{
    [SerializeField] private Button[] stageButtons; // Inspectorで登録
    [SerializeField] private string[] stageSceneNames;

    private const string TitleSceneName = "TitleScene";

    void Start()
    {
        UpdateStageButtons();
    }

    void UpdateStageButtons()
    {
        if (stageButtons == null || stageSceneNames == null) return;

        int count = Mathf.Min(stageButtons.Length, stageSceneNames.Length);
        int unlockedStage = ProgressStore.UnlockedStage;

        for (int i = 0; i < count; i++)
        {
            if (stageButtons[i] == null) continue;

            int stageIndex = i + 1;
            bool available = StageManager.CanLoadScene(stageSceneNames[i]);
            stageButtons[i].gameObject.SetActive(available);
            stageButtons[i].onClick.RemoveAllListeners();
            var label = stageButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.font = UIFont.TMPFont;
                label.text = "Stage " + stageIndex + "\n" + (ProgressStore.IsCleared(stageIndex) ? "クリア済み" : stageIndex <= unlockedStage ? "挑戦可能" : "未解放");
                label.enableAutoSizing = true;
                label.fontSizeMin = 14;
                label.fontSizeMax = 24;
            }

            if (stageIndex <= unlockedStage &&
                !string.IsNullOrEmpty(stageSceneNames[i]) &&
                StageManager.CanLoadScene(stageSceneNames[i]))
            {
                stageButtons[i].interactable = true;

                int index = i; // ローカルコピー
                stageButtons[i].onClick.RemoveAllListeners();
                stageButtons[i].onClick.AddListener(() => LoadStage(index));
            }
            else
            {
                stageButtons[i].interactable = false;
            }
        }
    }

    void LoadStage(int index)
    {
        if (stageSceneNames == null || index < 0 || index >= stageSceneNames.Length) return;

        if (index + 1 <= ProgressStore.UnlockedStage && StageManager.CanLoadScene(stageSceneNames[index]))
            SceneManager.LoadScene(stageSceneNames[index]);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene(TitleSceneName);
    }
}

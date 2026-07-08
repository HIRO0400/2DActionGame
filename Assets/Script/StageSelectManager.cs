using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);

        for (int i = 0; i < count; i++)
        {
            if (stageButtons[i] == null) continue;

            int stageIndex = i + 1;

            if (stageIndex <= unlockedStage)
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

        SceneManager.LoadScene(stageSceneNames[index]);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene(TitleSceneName);
    }
}
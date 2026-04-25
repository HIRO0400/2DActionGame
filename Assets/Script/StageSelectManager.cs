using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageSelectManager : MonoBehaviour
{
    public Button[] stageButtons; // Inspectorで登録
    public string[] stageSceneNames;

    void Start()
    {
        UpdateStageButtons();
    }

    void UpdateStageButtons()
    {
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);

        for (int i = 0; i < stageButtons.Length; i++)
        {
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
        SceneManager.LoadScene(stageSceneNames[index]);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
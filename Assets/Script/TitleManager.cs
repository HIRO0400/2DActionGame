using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    private const string StageSelectSceneName = "StageSelectScene";

    void Start()
    {
        if (GameObject.Find("ContinueButton") == null) MenuScreenLayout.Title();
        Bind("ContinueButton", OnClickContinue);
        Bind("HowToPlayButton", OnClickHowToPlay);
    }
    void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        var button = GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0) button.onClick.AddListener(action);
    }
    public void OnClickContinue() => SceneManager.LoadScene("Stage" + ProgressStore.UnlockedStage);
    public void OnClickHowToPlay() => GameMenus.Instance.OpenHowToPlay();

    // スタートボタン
    public void OnClickStart()
    {
        SceneManager.LoadScene(StageSelectSceneName); // 次のシーン名
    }

    // 終了ボタン
    public void OnClickQuit()
    {
        Application.Quit();
        Debug.Log("ゲーム終了"); // エディタ用
    }
}

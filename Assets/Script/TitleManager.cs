using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    private const string StageSelectSceneName = "StageSelectScene";

    void Start()
    {
        if (GameObject.Find("ContinueButton") == null) MenuScreenLayout.Title();
        var heading = GameObject.Find("TitleHeading");
        if (heading != null) heading.GetComponent<TMPro.TMP_Text>().text = "Clone Advanture";
        Bind("ContinueButton", OnClickContinue);
        Bind("HowToPlayButton", OnClickHowToPlay);
        if (GameObject.Find("CreditsButton") == null)
            MenuScreenLayout.Button(GameObject.Find("Canvas").transform, "CreditsButton", "クレジット", new Vector2(0, -202), new Vector2(440, 48));
        Bind("CreditsButton", OnClickCredits);
    }
    void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        var button = GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0) button.onClick.AddListener(action);
    }
    public void OnClickContinue() => SceneManager.LoadScene("Stage" + ProgressStore.UnlockedStage);
    public void OnClickHowToPlay() => GameMenus.Instance.OpenHowToPlay();
    public void OnClickCredits() => GameMenus.Instance.OpenCredits();

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

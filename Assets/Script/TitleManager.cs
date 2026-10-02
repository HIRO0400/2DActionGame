using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    private const string StageSelectSceneName = "StageSelectScene";

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

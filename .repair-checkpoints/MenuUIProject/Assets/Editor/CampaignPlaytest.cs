using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CampaignPlaytest
{
    static CampaignPlaytest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CampaignPlaytest",false))
            {
                SessionState.SetBool("CampaignPlaytest",false);
                new GameObject("CampaignPlaytest").AddComponent<CampaignPlaytestRunner>();
            }
        };
    }
    public static void Run()
    {
        SessionState.SetString("Campaign.OriginalProgress", JsonUtility.ToJson(new ProgressTestSnapshot()));
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage2.unity");
        SessionState.SetBool("CampaignPlaytest",true);
        EditorApplication.EnterPlaymode();
    }
}

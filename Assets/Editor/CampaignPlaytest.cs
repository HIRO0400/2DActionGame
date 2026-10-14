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
    { Begin(1); }
    public static void RunStage5() { Begin(5); }
    public static void RunStage4() { Begin(4); }
    public static void RunStage3() { Begin(3); }
    static void Begin(int start)
    {
        SessionState.SetInt("Campaign.StartStage",start);
        SessionState.SetString("Campaign.OriginalProgress", JsonUtility.ToJson(new ProgressTestSnapshot()));
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage"+start+".unity");
        SessionState.SetBool("CampaignPlaytest",true);
        EditorApplication.EnterPlaymode();
    }
}

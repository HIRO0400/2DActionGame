using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StageAuthoringPlaytest
{
    static StageAuthoringPlaytest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("StageAuthoringTest", false))
            {
                SessionState.SetBool("StageAuthoringTest", false);
                new GameObject("StagePlaytest").AddComponent<StagePlaytestRunner>();
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(StageAuthoringBootstrap.ScenePath);
        SessionState.SetBool("StageAuthoringTest", true);
        EditorApplication.EnterPlaymode();
    }
}

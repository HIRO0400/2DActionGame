using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReleaseValidation
{
    static ReleaseValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ReleaseTest", false))
            {
                SessionState.SetBool("ReleaseTest", false);
                new GameObject("ReleaseTest").AddComponent<ReleaseTestRunner>();
            }
        };
    }
    public static void Pause()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage1.unity");
        SessionState.SetBool("ReleaseTest", true);
        EditorApplication.EnterPlaymode();
    }
}

public class ReleaseTestRunner : MonoBehaviour
{
    readonly List<string> report = new List<string>();
    void Check(bool ok, string text)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + text);
        File.WriteAllLines("StageAuthoringReports/ReleasePreparation/pause-test.txt", report);
        if (!ok) { EditorApplication.Exit(1); throw new Exception(text); }
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        yield return new WaitForSeconds(0.5f);
        var menus = GameMenus.Instance;
        var player = FindFirstObjectByType<Player>();
        menus.Pause();
        var position = player.transform.position;
        yield return new WaitForSecondsRealtime(0.3f);
        Check(Time.timeScale == 0 && AudioListener.pause && GameMenus.BlocksGameplay, "Pause freezes time, sound, and gameplay input");
        Check(Vector3.Distance(position, player.transform.position) < 0.001f, "Player remains stationary while paused");
        menus.Resume();
        yield return new WaitForSecondsRealtime(0.2f);
        Check(Time.timeScale == 1 && !AudioListener.pause && !GameMenus.BlocksGameplay, "Resume restores time, sound, and input");
        menus.Pause();
        menus.Navigate("Stage1");
        yield return new WaitForSecondsRealtime(0.5f);
        Check(Time.timeScale == 1 && FindFirstObjectByType<Player>() != null, "Restart from pause works");
        menus.Pause(); menus.Navigate("StageSelectScene");
        yield return new WaitForSecondsRealtime(0.3f);
        Check(SceneManager.GetActiveScene().name == "StageSelectScene" && Time.timeScale == 1, "Back to selection restores time");
        menus.Navigate("TitleScene");
        yield return new WaitForSecondsRealtime(0.3f);
        Check(SceneManager.GetActiveScene().name == "TitleScene", "Back to title works");
        EditorApplication.Exit(0);
    }
}

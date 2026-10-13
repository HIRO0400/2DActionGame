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
        SessionState.SetInt("ReleasePhase", 1);
        Begin();
    }
    public static void Audio()
    {
        SessionState.SetInt("ReleasePhase", 2);
        Begin();
    }
    static void Begin()
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
        File.WriteAllLines("StageAuthoringReports/ReleasePreparation/phase" + SessionState.GetInt("ReleasePhase", 1) + "-test.txt", report);
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
        if (SessionState.GetInt("ReleasePhase", 1) >= 2)
        {
            float bgm = AudioSettings.BGM, sfx = AudioSettings.SFX; bool muted = AudioSettings.Muted;
            AudioSettings.SetBGM(0); AudioSettings.SetSFX(0);
            var mixer = ReleaseAssets.Current.audioMixer;
            mixer.GetFloat("BGMVolume", out float db);
            Check(db == -80 && !float.IsInfinity(AudioSettings.Decibels(0)), "Zero volume is finite and silent");
            AudioSettings.SetBGM(0.5f); AudioSettings.SetSFX(0.25f); AudioSettings.SetMuted(true);
            mixer.GetFloat("SFXVolume", out db);
            Check(db == -80, "Mute affects both mixer groups");
            AudioSettings.SetMuted(false); AudioSettings.Save(); AudioSettings.Reload();
            mixer.GetFloat("BGMVolume", out db);
            Check(Mathf.Abs(db + 6.0206f) < 0.01f && AudioSettings.SFX == 0.25f, "Independent volumes persist and reload");
            AudioSettings.SetBGM(bgm); AudioSettings.SetSFX(sfx); AudioSettings.SetMuted(muted); AudioSettings.Save();
        }
        EditorApplication.Exit(0);
    }
}

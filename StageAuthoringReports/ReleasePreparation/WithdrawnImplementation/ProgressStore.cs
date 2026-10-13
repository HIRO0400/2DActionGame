using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProgressStore
{
    public static int LastStage
    {
        get
        {
            int maximum = 1;
#if UNITY_EDITOR
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
                if (scene.enabled && UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scene.path) != null)
                    Include(scene.path, ref maximum);
#else
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) Include(SceneUtility.GetScenePathByBuildIndex(i), ref maximum);
#endif
            return maximum;
        }
    }
    static void Include(string path, ref int maximum)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (name.StartsWith("Stage") && int.TryParse(name.Substring(5), out int stage)) maximum = Mathf.Max(maximum, stage);
    }
    public static void Initialize()
    {
        int original = PlayerPrefs.GetInt("UnlockedStage", 1);
        int unlocked = Mathf.Clamp(original, 1, LastStage);
        bool changed = original != unlocked;
        if (PlayerPrefs.GetInt("Progress.Version", 0) < 1)
        {
            // Existing unlocks prove that earlier stages were cleared.
            for (int i = 1; i < unlocked; i++) PlayerPrefs.SetInt("Progress.Cleared." + i, 1);
            PlayerPrefs.SetInt("Progress.Version", 1);
            changed = true;
        }
        if (changed) { PlayerPrefs.SetInt("UnlockedStage", unlocked); PlayerPrefs.Save(); }
    }
    public static int UnlockedStage { get { Initialize(); return Mathf.Clamp(PlayerPrefs.GetInt("UnlockedStage", 1), 1, LastStage); } }
    public static bool IsCleared(int stage) => PlayerPrefs.GetInt("Progress.Cleared." + stage, 0) == 1;
    public static void CompleteStage(int stage, string nextScene)
    {
        if (stage < 1 || stage > LastStage) return;
        Initialize();
        PlayerPrefs.SetInt("Progress.Cleared." + stage, 1);
        if (stage >= UnlockedStage && StageManager.CanLoadScene(nextScene))
            PlayerPrefs.SetInt("UnlockedStage", Mathf.Min(stage + 1, LastStage));
        PlayerPrefs.Save();
    }
    public static void ResetProgress()
    {
        for (int i = 1; i <= LastStage; i++) PlayerPrefs.DeleteKey("Progress.Cleared." + i);
        PlayerPrefs.SetInt("UnlockedStage", 1);
        PlayerPrefs.SetInt("Progress.Version", 1);
        PlayerPrefs.Save();
    }
}

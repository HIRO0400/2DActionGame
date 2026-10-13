#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public sealed class ProgressTestSnapshot
{
    readonly Dictionary<string, int?> values = new Dictionary<string, int?>();
    public ProgressTestSnapshot()
    {
        Capture("UnlockedStage");
        Capture("Progress.Version");
        for (int i = 1; i <= ProgressStore.LastStage; i++) Capture("Progress.Cleared." + i);
    }
    void Capture(string key) => values[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
    public void Restore()
    {
        foreach (var pair in values)
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
        PlayerPrefs.Save();
    }
}
#endif

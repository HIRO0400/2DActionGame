#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProgressTestSnapshot
{
    [Serializable] public class Entry { public string key; public bool exists; public int value; }
    public List<Entry> entries = new List<Entry>();
    public ProgressTestSnapshot()
    {
        Capture("UnlockedStage"); Capture("Progress.Version");
        for (int i = 1; i <= ProgressStore.LastStage; i++) Capture("Progress.Cleared." + i);
    }
    void Capture(string key) => entries.Add(new Entry { key = key, exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) });
    public void Restore()
    {
        foreach (var entry in entries) if (entry.exists) PlayerPrefs.SetInt(entry.key, entry.value); else PlayerPrefs.DeleteKey(entry.key);
        PlayerPrefs.Save();
    }
}
#endif

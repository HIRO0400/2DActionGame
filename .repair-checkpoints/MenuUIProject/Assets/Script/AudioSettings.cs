using UnityEngine;

public static class AudioSettings
{
    public static float BGM { get; private set; }
    public static float SFX { get; private set; }
    public static bool Muted { get; private set; }
    public static void Reload()
    {
        BGM = SafeVolume(PlayerPrefs.GetFloat("Settings.BGM", Mathf.Pow(10, -16.03596f / 20)));
        SFX = SafeVolume(PlayerPrefs.GetFloat("Settings.SFX", Mathf.Pow(10, -21.302511f / 20)));
        Muted = PlayerPrefs.GetInt("Settings.Muted", 0) == 1;
        Apply();
    }
    static float SafeVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Mathf.Clamp01(value);
    public static void SetBGM(float value) { BGM = SafeVolume(value); PlayerPrefs.SetFloat("Settings.BGM", BGM); Apply(); }
    public static void SetSFX(float value) { SFX = SafeVolume(value); PlayerPrefs.SetFloat("Settings.SFX", SFX); Apply(); }
    public static void SetMuted(bool value) { Muted = value; PlayerPrefs.SetInt("Settings.Muted", value ? 1 : 0); Apply(); }
    public static float Decibels(float volume) => volume <= 0 ? -80 : Mathf.Max(-80, Mathf.Log10(volume) * 20);
    public static void Apply()
    {
        var assets = ReleaseAssets.Current;
        if (assets == null || assets.audioMixer == null) return;
        assets.audioMixer.SetFloat("BGMVolume", Muted ? -80 : Decibels(BGM));
        assets.audioMixer.SetFloat("SFXVolume", Muted ? -80 : Decibels(SFX));
    }
    public static void Save() => PlayerPrefs.Save();
}

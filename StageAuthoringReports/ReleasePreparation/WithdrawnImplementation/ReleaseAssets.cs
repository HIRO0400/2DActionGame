using UnityEngine;
using UnityEngine.Audio;

public class ReleaseAssets : ScriptableObject
{
    public AudioMixer audioMixer;
    public Font japaneseFont;
    public static ReleaseAssets Current => Resources.Load<ReleaseAssets>("ReleaseAssets");
}

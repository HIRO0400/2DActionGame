using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class ReleaseAssetsSetup
{
    public static void Font()
    {
        AssetDatabase.Refresh();
        var assets = AssetDatabase.LoadAssetAtPath<ReleaseAssets>("Assets/Resources/ReleaseAssets.asset");
        assets.japaneseFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKjp-Regular.otf");
        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
        ReleaseValidation.Font();
    }
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var assets = AssetDatabase.LoadAssetAtPath<ReleaseAssets>("Assets/Resources/ReleaseAssets.asset");
        if (assets == null)
        {
            assets = ScriptableObject.CreateInstance<ReleaseAssets>();
            AssetDatabase.CreateAsset(assets, "Assets/Resources/ReleaseAssets.asset");
        }
        assets.audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/GameAudioMixer.mixer");
        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
        ReleaseValidation.Audio();
    }
}

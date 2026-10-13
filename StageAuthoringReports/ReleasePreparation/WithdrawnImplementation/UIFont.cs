using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIFont
{
    static TMP_FontAsset tmpFont;
    public static Font Font => ReleaseAssets.Current != null && ReleaseAssets.Current.japaneseFont != null
        ? ReleaseAssets.Current.japaneseFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public static TMP_FontAsset TMPFont
    {
        get
        {
            if (tmpFont == null) tmpFont = TMP_FontAsset.CreateFontAsset(Font);
            return tmpFont;
        }
    }
    public static void Apply(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true)) text.font = Font;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font = TMPFont;
        }
    }
}

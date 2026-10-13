using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MenuScreenBuilder
{
    [MenuItem("Tools/UI/タイトル・選択画面を整える")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var assets = ReleaseAssets.Current;
        if (assets.japaneseTMPFont == null)
        {
            var font = UIFont.TMPFont;
            font.name = "NotoSansCJKjp UI";
            AssetDatabase.CreateAsset(font, "Assets/Fonts/NotoSansCJKjp-UI.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            assets.japaneseTMPFont = font; EditorUtility.SetDirty(assets);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/TitleScene.unity");
        MenuScreenLayout.Title();
        MenuBackgroundSetup.ApplyCurrentScene();
        var title = Object.FindFirstObjectByType<TitleManager>();
        Wire("ContinueButton", title.OnClickContinue);
        Wire("HowToPlayButton", title.OnClickHowToPlay);
        Wire("StartButton", title.OnClickStart); Wire("QuitButton", title.OnClickQuit);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.OpenScene("Assets/Scenes/Stage/StageSelectScene.unity");
        var manager = Object.FindFirstObjectByType<StageSelectManager>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var buttons = (Button[])typeof(StageSelectManager).GetField("stageButtons", flags).GetValue(manager);
        var scenes = (string[])typeof(StageSelectManager).GetField("stageSceneNames", flags).GetValue(manager);
        MenuScreenLayout.Selection(buttons, scenes);
        MenuBackgroundSetup.ApplyCurrentScene();
        for (int i = 0; i < buttons.Length; i++) if (buttons[i] != null)
        {
            var label = buttons[i].GetComponentInChildren<TMPro.TMP_Text>(true);
            label.font = UIFont.TMPFont; label.text = "Stage " + (i + 1) + "\n" + (i == 0 ? "挑戦可能" : "未解放");
            label.fontSize = 24; label.alignment = TMPro.TextAlignmentOptions.Center;
        }
        Wire("Back", manager.BackToTitle);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
    static void Wire(string name, UnityEngine.Events.UnityAction action)
    {
        var button = GameObject.Find(name).GetComponent<Button>();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
    public static void Run()
    {
        Build();
        MenuScreenValidation.Run();
    }
}

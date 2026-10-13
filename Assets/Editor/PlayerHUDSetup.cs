using UnityEditor;
using UnityEngine;

public static class PlayerHUDSetup
{
    public static void Run()
    {
        const string path = "Assets/Prefab/Player.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud = root.GetComponent<PlayerHUD>() ?? root.AddComponent<PlayerHUD>();
            var data = new SerializedObject(hud);
            data.FindProperty("cloneIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/クローン_0.prefab").GetComponent<SpriteRenderer>().sprite;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }
}

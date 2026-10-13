using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuBackgroundSetup
{
    const string TitleImage = "Assets/UI/Backgrounds/Title_BrickRuins.png";
    const string SelectImage = "Assets/UI/Backgrounds/Select_LayeredForest.png";
    public static void ApplyCurrentScene()
    {
        bool title = SceneManager.GetActiveScene().name == "TitleScene";
        var path = title ? TitleImage : SelectImage;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) return;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var canvas = GameObject.Find("Canvas").transform;
        var root = canvas.Find("MenuBackground");
        if (root == null) root = new GameObject("MenuBackground", typeof(RectTransform), typeof(RectMask2D)).transform;
        root.SetParent(canvas, false); root.SetAsFirstSibling(); Stretch((RectTransform)root);
        var artwork = root.Find("Artwork");
        if (artwork == null) artwork = new GameObject("Artwork", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)).transform;
        artwork.SetParent(root, false); Stretch((RectTransform)artwork);
        var image = artwork.GetComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
        var fit = artwork.GetComponent<AspectRatioFitter>();
        fit.aspectRatio = (float)texture.width / texture.height;
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        var tint = root.Find("ReadabilityShade");
        if (tint == null) tint = new GameObject("ReadabilityShade", typeof(RectTransform), typeof(Image)).transform;
        tint.SetParent(root, false); tint.SetAsLastSibling(); Stretch((RectTransform)tint);
        var shade = tint.GetComponent<Image>(); shade.color = new Color(.025f, .045f, .075f, title ? .42f : .24f);
        shade.raycastTarget = false;
    }
    static void Stretch(RectTransform rect)
    {
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public static void Run()
    {
        foreach (var name in new[] { "TitleScene", "StageSelectScene" })
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Stage/" + name + ".unity");
            ApplyCurrentScene(); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        AssetDatabase.SaveAssets();
        MenuScreenValidation.Run();
    }
}

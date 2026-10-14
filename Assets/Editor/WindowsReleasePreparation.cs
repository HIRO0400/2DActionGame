using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WindowsReleasePreparation
{
    // Run against the validation copy; no BuildPipeline call is made here.
    public static void Run()
    {
        try
        {
            var clone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/クローン_0.prefab").GetComponent<SpriteRenderer>();
            var sprite = clone.sprite;
            var target = RenderTexture.GetTemporary(sprite.texture.width,sprite.texture.height,0,RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D source = null, icon = null;
            try
            {
                Graphics.Blit(sprite.texture,target); RenderTexture.active = target;
                source = new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                source.ReadPixels(new Rect(0,0,target.width,target.height),0,0); source.Apply();
                var rect = sprite.textureRect;
                const int size = 256;
                icon = new Texture2D(size,size,TextureFormat.RGBA32,false);
                var pixels = new Color[size*size];
                float scale = 208f / Mathf.Max(rect.width,rect.height);
                int width = Mathf.RoundToInt(rect.width*scale), height = Mathf.RoundToInt(rect.height*scale);
                int left = (size-width)/2, bottom = (size-height)/2;
                for(int y=0;y<height;y++) for(int x=0;x<width;x++)
                    pixels[(bottom+y)*size+left+x] = source.GetPixel((int)rect.x+Mathf.Min((int)(x/scale),(int)rect.width-1),
                        (int)rect.y+Mathf.Min((int)(y/scale),(int)rect.height-1)) * clone.color;
                icon.SetPixels(pixels); icon.Apply();
                File.WriteAllBytes("Assets/UI/AppIcon.png",icon.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
                if(source!=null) UnityEngine.Object.DestroyImmediate(source);
                if(icon!=null) UnityEngine.Object.DestroyImmediate(icon);
            }
            AssetDatabase.ImportAsset("Assets/UI/AppIcon.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/UI/AppIcon.png");
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var iconAsset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/AppIcon.png");
            int count = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone,IconKind.Any).Length;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,Enumerable.Repeat(iconAsset,count).ToArray(),IconKind.Any);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage/TitleScene.unity");
            GameObject.Find("TitleHeading").GetComponent<TMPro.TMP_Text>().text = PlayerSettings.productName;
            var button = MenuScreenLayout.Button(GameObject.Find("Canvas").transform,"CreditsButton","クレジット",new Vector2(0,-202),new Vector2(440,48));
            if(button.onClick.GetPersistentEventCount()==0)
                UnityEventTools.AddPersistentListener(button.onClick,UnityEngine.Object.FindFirstObjectByType<TitleManager>().OnClickCredits);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var options = WindowsReleaseBuild.CreateOptions();
            if(options.options!=UnityEditor.BuildOptions.None || options.target!=BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Invalid release build options.");
            Directory.CreateDirectory("StageAuthoringReports/WindowsPreparation");
            File.WriteAllText("StageAuthoringReports/WindowsPreparation/settings-check.txt",
                "PASS: Release options use Windows64 without Development Build\nPASS: Options prepared without building\n"+
                "PASS: Title is " + PlayerSettings.productName + "\nPASS: Company is " + PlayerSettings.companyName +
                "\nPASS: Windows icon has " + count + " assigned sizes\nOutput: " + options.locationPathName);
            EditorApplication.Exit(0);
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}

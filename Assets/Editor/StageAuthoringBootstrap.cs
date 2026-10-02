using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class StageAuthoringBootstrap
{
    public const string LayoutPath = "Assets/StageAuthoring/Layouts/Stage1Layout.asset";
    public const string ScenePath = "Assets/Scenes/Stage/Stage1.unity";
    public const string ArchivePath = "Assets/Scenes/Archive/Stage1_BeforeAuthoring.unity";
    public const string TemplatePath = "Assets/Scenes/Templates/StageTemplate.unity";

    public static void Build()
    {
        try
        {
            Directory.CreateDirectory("StageAuthoringReports");
            AssetDatabase.Refresh();
            var data = AssetDatabase.LoadAssetAtPath<StageLayout>(LayoutPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StageLayout>();
                data.sceneName = "Stage1";
                data.title = "足場を残して、もう一度";
                data.concept = "安全な場所でクローンを体験し、コインで足場数を増やし、段差と小さな穴を攻略する導入ステージ。";
                data.difficulty = 1;
                data.playerStart = new Vector2(2.5f, 0.5f);
                data.goalPosition = new Vector2(70.5f, 0.5f);
                data.playerPrefab = Load<GameObject>("Assets/Prefab/Player.prefab");
                data.goalPrefab = Load<GameObject>("Assets/Prefab/Goal.prefab");
                data.stageManagerPrefab = Load<GameObject>("Assets/Prefab/StageManager.prefab");
                data.audioManagerPrefab = Load<GameObject>("Assets/Prefab/AudioManager.prefab");
                data.bgmPrefab = Load<GameObject>("Assets/Prefab/BGM.prefab");
                data.groundTile = Load<TileBase>("Assets/Ground/Brick/UM.asset");
                data.spikeTile = Load<TileBase>("Assets/Ground/Spike/Spike.asset");
                data.groundMaterial = Load<PhysicsMaterial2D>("Assets/Material/Function.physicsMaterial2D");
                data.terrain = new[]
                {
                    new StageLayout.GroundRegion("スタートと最初のクローン学習", -2, -2, 45, 2),
                    new StageLayout.GroundRegion("ジャンプ練習の低い段差", 8, 0, 3, 1),
                    new StageLayout.GroundRegion("クローンで越える段差", 20, 0, 4, 4),
                    new StageLayout.GroundRegion("下りの安全足場", 24, 0, 3, 2),
                    new StageLayout.GroundRegion("コイン学習の段差", 31, 0, 3, 2),
                    new StageLayout.GroundRegion("任意のコイン足場", 34, 0, 4, 3),
                    new StageLayout.GroundRegion("穴の後の安全地面とゴール", 46, -2, 29, 2),
                    new StageLayout.GroundRegion("最後のクローン応用", 60, 0, 5, 4),
                    new StageLayout.GroundRegion("最後の下り足場", 65, 0, 2, 2),
                };
                data.hazards = new[] { new StageLayout.GroundRegion("見える浅い穴の底のトゲ", 43, -3, 3, 1) };
                var coin = Load<GameObject>("Assets/Prefab/Coin.prefab");
                data.placements = new[]
                {
                    new StageLayout.PrefabPlacement("Coin_FirstCapacity", coin, 28.5f, 1.2f),
                    new StageLayout.PrefabPlacement("Coin_OptionalHighRoute", coin, 36f, 4.2f),
                };
                data.areas = new[]
                {
                    new StageLayout.LearningArea("A まずは歩いて、ジャンプ", "A/D または ←/→ で移動。Spaceを長く押すと高く跳べます。低い段差で試してみましょう。", -2, 12),
                    new StageLayout.LearningArea("B 足場を残して、戻ってみよう", "高い段差の少し手前で左クリック。離すと足場が残り、自分は戻ります。残した足場に乗ってジャンプ！", 12, 27),
                    new StageLayout.LearningArea("C コインで足場を増やそう", "コインを取ると残せる足場が1つ増えます。高い場所のコインは、取らなくても先へ進めます。", 27, 40),
                    new StageLayout.LearningArea("D 小さな穴を越えよう", "穴の底にはトゲがあります。Spaceを長押ししてジャンプ。空中にもクローンの足場を残せます。", 40, 56),
                    new StageLayout.LearningArea("E 覚えた操作で、ゴールへ", "最後の高い段差も、手前に残した足場を使って越えましょう。コインで増えた足場数も使えます。", 56, 76),
                };
                Directory.CreateDirectory(Path.GetDirectoryName(LayoutPath));
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(data, LayoutPath);
                AssetDatabase.SaveAssets();
            }
            var issues = StageBuilder.Validate(data);
            if (issues.Count > 0) throw new Exception(string.Join("\n", issues));
            Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath));
            AssetDatabase.Refresh();
            if (File.Exists(ScenePath) && !File.Exists(ArchivePath))
            {
                if (!AssetDatabase.CopyAsset(ScenePath, ArchivePath)) throw new Exception("旧Stage1の保存に失敗しました。");
                File.Copy("ProjectSettings/EditorBuildSettings.asset", "StageAuthoringReports/EditorBuildSettings.before.asset", true);
            }
            const string backgroundPath = "Assets/StageAuthoring/Prefabs/StageBackground.prefab";
            if (!File.Exists(backgroundPath))
            {
                EditorSceneManager.OpenScene(ArchivePath);
                var background = GameObject.Find("BackGround");
                if (background != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backgroundPath));
                    AssetDatabase.Refresh();
                    PrefabUtility.SaveAsPrefabAsset(background, backgroundPath);
                }
            }
            data = Load<StageLayout>(LayoutPath);
            data.backgroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(backgroundPath);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (UnityEngine.Object.FindFirstObjectByType<GeneratedStage>() == null)
            {
                // The original is archived first; rebuilding Stage1 is explicitly requested.
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            data = Load<StageLayout>(LayoutPath);
            StageBuilder.Build(data, scene, false);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Stage1を保存できませんでした。");
            if (!File.Exists(TemplatePath))
            {
                var template = UnityEngine.Object.Instantiate(data);
                template.sceneName = "StageTemplate";
                template.title = "ステージ制作テンプレート";
                template.playerStart = new Vector2(2.5f, 0.5f);
                template.goalPosition = new Vector2(12.5f, 0.5f);
                template.terrain = new[] { new StageLayout.GroundRegion("安全な基本床", -2, -2, 18, 2) };
                template.hazards = Array.Empty<StageLayout.GroundRegion>();
                template.placements = Array.Empty<StageLayout.PrefabPlacement>();
                template.areas = new[] { new StageLayout.LearningArea("制作テンプレート", "移動：A/D・左右矢印　ジャンプ：Space　足場を残す：左クリック", -2, 18) };
                AssetDatabase.CreateAsset(template, "Assets/StageAuthoring/Layouts/StageTemplateLayout.asset");
                StageBuilder.CreateScene(template, TemplatePath);
            }
            EditorSceneManager.OpenScene(ScenePath);
            var report = new List<string> { "Stage1 created; original archived at " + ArchivePath, "Ground uses CompositeCollider2D and Static Rigidbody2D.", "Spike uses the Spike layer and trigger collider.", "AudioManager is a scene root; Goal is static and has an explicit manager reference.", "Player uses existing movement, clone, respawn and input settings." };
            File.WriteAllLines("StageAuthoringReports/build.txt", report);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Directory.CreateDirectory("StageAuthoringReports");
            File.WriteAllText("StageAuthoringReports/build-failure.txt", e.ToString());
            EditorApplication.Exit(1);
        }
    }
    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new Exception("アセットがありません：" + path);
        return asset;
    }
}

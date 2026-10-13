using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampaignStageBuilder
{
    public static void Build()
    {
        try
        {
            Directory.CreateDirectory("StageAuthoringReports/CampaignBackup");
            if (!File.Exists("StageAuthoringReports/CampaignBackup/EditorBuildSettings.asset"))
                File.Copy("ProjectSettings/EditorBuildSettings.asset", "StageAuthoringReports/CampaignBackup/EditorBuildSettings.asset");
            AssetDatabase.Refresh();
            var report = new List<string>();
            for (int n = 2; n <= 5; n++)
            {
                string path = "Assets/Scenes/Stage/Stage" + n + ".unity";
                string archive = "Assets/Scenes/Archive/Stage" + n + "_BeforeCampaign.unity";
                if (File.Exists(path) && !File.Exists(archive) && !AssetDatabase.CopyAsset(path, archive))
                    throw new Exception("Backup failed: " + path);
                string layoutPath = "Assets/StageAuthoring/Layouts/Stage" + n + "Layout.asset";
                var data = AssetDatabase.LoadAssetAtPath<StageLayout>(layoutPath);
                if (data == null)
                {
                    data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<StageLayout>(StageAuthoringBootstrap.LayoutPath));
                    AssetDatabase.CreateAsset(data, layoutPath);
                }
                Configure(data, n);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                data = AssetDatabase.LoadAssetAtPath<StageLayout>(layoutPath);
                StageBuilder.Build(data, scene, false);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception("Save failed: " + path);
                report.Add("Created Stage" + n + ", goal=" + data.goalPosition + ", regions=" + data.terrain.Length);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int n = 2; n <= 5; n++)
            {
                string path = "Assets/Scenes/Stage/Stage" + n + ".unity";
                int index = scenes.FindIndex(s => s.path == path);
                if (index >= 0) scenes[index] = new EditorBuildSettingsScene(path, true);
                else scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllLines("StageAuthoringReports/campaign-build.txt", report);
            EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage2.unity");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.WriteAllText("StageAuthoringReports/campaign-build-failure.txt", e.ToString());
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }
    static StageLayout.GroundRegion R(string label, int x, int y, int w, int h) => new StageLayout.GroundRegion(label,x,y,w,h);
    static StageLayout.LearningArea A(string label, string text, int x, int end) => new StageLayout.LearningArea(label,text,x,end);
    static void Configure(StageLayout d, int n)
    {
        d.sceneName = "Stage" + n;
        d.playerStart = new Vector2(2.5f,0.5f);
        var terrain = new List<StageLayout.GroundRegion>();
        var hazards = new List<StageLayout.GroundRegion>();
        var objects = new List<StageLayout.PrefabPlacement>();
        var coin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab");
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/enemy_zako1.prefab");
        if (enemy == null) throw new Exception("Enemy prefab missing");
        if (n == 2)
        {
            d.title = "危険を見て、跳び越える"; d.difficulty = 2;
            d.concept = "見えるトゲと3マスの穴を、広い着地先へ通常ジャンプで越える。最後の安全地面で敵を1体紹介。";
            terrain.AddRange(new[] { R("A安全地面",-8,-2,32,2), R("B安全地面",27,-2,33,2), R("C安全地面",63,-2,34,2), R("D安全地面",100,-2,67,2), R("低い段差",36,0,3,1), R("低い段差",78,0,4,2) });
            hazards.AddRange(new[] { R("最初の見えるトゲ",12,0,1,1), R("穴1の底",24,-3,3,1), R("2マスのトゲ",45,0,2,1), R("穴2の底",60,-3,3,1), R("応用トゲ",88,0,2,1), R("穴3の底",97,-3,3,1), R("最後のトゲ",110,0,2,1) });
            objects.Add(new StageLayout.PrefabPlacement("Coin_Safe",coin,17,0.9f));
            objects.Add(new StageLayout.PrefabPlacement("Coin_Jump",coin,80,3.1f));
            objects.Add(new StageLayout.PrefabPlacement("Enemy_OnWideGround",enemy,145,0.6f));
            d.goalPosition = new Vector2(158.5f,0.5f);
            d.areas = new[] { A("A 見てからジャンプ","先に見えるトゲを、少し手前から長押しジャンプ。失敗しても近くの地面へ戻ります。",-8,24), A("B 小さな穴と広い着地先","穴は3マス。端ぎりぎりを狙わず、手前から長押しで越えましょう。",24,63), A("C 段差とトゲ","まず安全な段差に着地し、次のトゲを確認。コインは任意です。",63,100), A("D 危険の組み合わせ","一つずつ越えれば大丈夫。地面に着地してから次のジャンプへ。",100,134), A("E 敵を越えてゴール","敵は横から触れると危険。ジャンプで避けるか、上から踏めます。",134,168) };
        }
        else if (n == 3)
        {
            d.title = "空中に、ひとつの足場"; d.difficulty = 3;
            d.concept = "5マスの壁を空中に残すクローン1つで攻略。安全な床で設置を試し、段差と小さな穴へ応用する。";
            terrain.AddRange(new[] { R("A地面",-8,-2,28,2), R("B地面",23,-2,52,2), R("C地面",78,-2,43,2), R("D地面",124,-2,51,2) });
            AddWall(terrain,30,5); AddWall(terrain,86,5); AddWall(terrain,138,5);
            hazards.AddRange(new[] { R("穴1",20,-3,3,1), R("トゲ1",63,0,1,1), R("穴2",75,-3,3,1), R("トゲ2",113,0,2,1), R("穴3",121,-3,3,1), R("最後のトゲ",156,0,2,1) });
            objects.Add(new StageLayout.PrefabPlacement("Coin_AfterLearning",coin,51,0.9f));
            objects.Add(new StageLayout.PrefabPlacement("Coin_Optional",coin,109,0.9f));
            d.goalPosition = new Vector2(167.5f,0.5f);
            d.areas = new[] { A("A 空中に足場を残す練習","地面からジャンプして左クリックを離すと、その高さに足場が残ります。まず安全な床で試しましょう。",-8,26), A("B 足場1つで高い壁へ","壁の少し手前で、空中に足場を1つ残します。地面へ戻り、その足場に乗って長押しジャンプ！",26,62), A("C 穴の後で、もう一度","穴を越えて安全な床に着地してから足場を設置。必要なのは使える足場1つです。",62,110), A("D 覚えた高さを再現","最後の高い壁も同じ方法で攻略。トゲの手前には安全な床があります。",110,150), A("E 着地してゴールへ","壁を越えたら、最後の見えるトゲを長押しジャンプで越えましょう。",150,176) };
        }
        else if (n == 4)
        {
            d.title = "足場の高さを選ぶ"; d.difficulty = 4;
            d.concept = "6マスの壁をクローン1つで攻略。足場の高さを考え、見えるトゲと穴を組み合わせる。敵は最後の広い床に1体。";
            terrain.AddRange(new[] { R("A地面",-8,-2,28,2), R("B地面",23,-2,44,2), R("C地面",70,-2,42,2), R("D地面",115,-2,65,2) });
            AddWall(terrain,28,6); AddWall(terrain,80,6); AddWall(terrain,146,6);
            hazards.AddRange(new[] { R("穴1",20,-3,3,1), R("トゲ1",54,0,2,1), R("穴2",67,-3,3,1), R("トゲ2",99,0,2,1), R("穴3",112,-3,3,1), R("トゲ3",132,0,2,1) });
            objects.Add(new StageLayout.PrefabPlacement("Coin_AfterFirstWall",coin,47,0.9f));
            objects.Add(new StageLayout.PrefabPlacement("Enemy_AfterFinalPuzzle",enemy,168,0.6f));
            d.goalPosition = new Vector2(174.5f,0.5f);
            d.areas = new[] { A("A 少し高い足場を試す","足場を地面に置くだけでは、次の壁に届きません。ジャンプ中に残し、地面から届く高さを探しましょう。",-8,24), A("B 高さを選んで壁を越える","壁の手前に空中の足場を残す → 地面から足場へ → 足場から壁へ。足場1つで攻略できます。",24,53), A("C トゲ・穴・高い壁","トゲと穴を先に越え、安全な床へ着地してからクローンを使います。",53,99), A("D 安全な場所を選ぶ","危険の直上ではなく、安全な床の上に足場を残すと失敗から復帰しやすくなります。",99,142), A("E 最後の壁と敵","最後の壁の後は広い床。敵はジャンプで避けるか上から踏み、ゴールへ進みましょう。",142,181) };
        }
        else
        {
            d.title = "ふたつの足場をつなぐ"; d.difficulty = 5;
            d.concept = "容量を増やす安全なコインの後、低いクローンと高いクローンの2つを同時に残して8マスの壁を攻略する。敵踏みによる迂回を避けるため敵は配置しない。";
            terrain.AddRange(new[] { R("A地面",-8,-2,74,2), R("B地面",69,-2,46,2), R("C地面",118,-2,58,2) });
            AddWall(terrain,41,8); AddWall(terrain,94,8); AddWall(terrain,137,8);
            hazards.AddRange(new[] { R("穴1",66,-3,3,1), R("トゲ1",79,0,2,1), R("穴2",115,-3,3,1), R("トゲ2",126,0,2,1), R("最後のトゲ",157,0,2,1) });
            objects.Add(new StageLayout.PrefabPlacement("Coin_RequiredSecondSlot",coin,6,0.9f));
            d.goalPosition = new Vector2(169.5f,0.5f);
            d.areas = new[] { A("A 足場を2つ残せる準備","最初の安全なコインを取りましょう。残せる足場が2つになります。まず地面で1つ残す操作を復習。",-8,29), A("B 低い足場と高い足場","壁の少し手前に低い足場。そこからジャンプし、少し前の高い位置に2つ目を残します。戻ったら、低い足場 → 高い足場 → 壁の順へ。",29,66), A("C 2つを残したまま進む","穴を越えてから、安全な床で2つの足場を作り直します。高い足場へは真上に跳び上がってから横へ移動すると安全です。",66,115), A("D 危険を越え、足場を準備","トゲを先に越え、安全な床で低い足場と高い足場を作りましょう。",115,133), A("E 最後の2段階ジャンプ","2つの足場を同時に残し、低い足場 → 高い足場 → 最後の壁へ。下りの後にも見えるトゲがあります。",133,177) };
        }
        d.terrain = terrain.ToArray(); d.hazards = hazards.ToArray(); d.placements = objects.ToArray();
    }
    static void AddWall(List<StageLayout.GroundRegion> terrain, int x, int height)
    {
        terrain.Add(R("必須のクローン段差",x,0,6,height));
        terrain.Add(R("安全な下り上段",x+6,0,3,Math.Max(2,height-3)));
        terrain.Add(R("安全な下り下段",x+9,0,3,2));
    }
}

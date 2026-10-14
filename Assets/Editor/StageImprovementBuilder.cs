using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using R = StageLayout.GroundRegion;
using A = StageLayout.LearningArea;
using P = StageLayout.PrefabPlacement;

// Batch entry points intentionally rebuild only the generated portion of each Scene.
public static class StageImprovementBuilder
{
    public static void Phase1() => Build(1);
    public static void Phase2() => Build(2);
    public static void Phase3() => Build(3);
    public static void Phase4() => Build(4);
    static void Build(int phase)
    {
        try
        {
            Directory.CreateDirectory("StageAuthoringReports/Improvements");
            var report = new List<string>();
            for (int n = 1; n <= 5; n++)
            {
                var data = AssetDatabase.LoadAssetAtPath<StageLayout>($"Assets/StageAuthoring/Layouts/Stage{n}Layout.asset");
                Configure(data, n, phase);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                var scene = EditorSceneManager.OpenScene($"Assets/Scenes/Stage/Stage{n}.unity");
                StageBuilder.Build(data, scene, false);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
                report.Add($"Stage{n}: goal={data.goalPosition}, terrain={data.terrain.Length}, hazards={data.hazards.Length}");
            }
            File.WriteAllLines($"StageAuthoringReports/Improvements/phase{phase}-build.txt", report);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void Configure(StageLayout d, int n, int phase)
    {
        if (d == null) throw new InvalidOperationException("Missing Stage layout");
        var ground = new List<R>(); var spikes = new List<R>(); var items = new List<P>(); var areas = new List<A>();
        var coin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab");
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/enemy_zako1.prefab");
        void Base(int x, int end) => ground.Add(new R("主ルートの地面", x, -2, end-x, 2));
        void Wall(int x, int h) { ground.Add(new R("必須のクローン段差",x,0,6,h)); ground.Add(new R("降りる段差",x+6,0,3,2)); }
        void Coin(float x,float y) => items.Add(new P("Coin",coin,x,y));
        d.sceneName = "Stage"+n; d.playerStart = new Vector2(2.5f,.5f); d.difficulty=n;
        if(n==1)
        {
            d.title="はじめの一歩"; d.concept="歩く、跳ぶ、寄り道、クローンの足場を安全に順番に学ぶ。"; d.goalPosition=new Vector2(44.5f,.5f);
            Base(-2,14); Base(17,49); ground.Add(new R("穴の安全な受け床",14,-3,3,2));
            ground.Add(new R("低い段差",8,0,3,1)); ground.Add(new R("上の寄り道",21,2,4,1));
            ground.Add(new R("低いルートの天井",18,4,3,1)); Wall(30,4); Coin(22.5f,4f);
            areas.Add(new A("A 歩いて、跳ぶ","A / D または ← / → で移動。Space を長く押すほど高く跳べます。",-2,13));
            areas.Add(new A("B 安全な穴と分岐","穴に落ちても床があります。上のコインは寄り道。下を通っても進めます。",13,28));
            areas.Add(new A("C クローンを足場に","壁の手前で左クリックでクローンを残します。戻ったら、クローンに乗って壁を越えましょう。",28,40));
            areas.Add(new A("D ゴールへ","クローンは足場になります。コインで作れる数が増えます。",40,49));
        }
        if(n==2)
        {
            d.title="見える危険を越える"; d.concept="短いトゲ、穴、上下の分岐を組み合わせる実践。"; d.goalPosition=new Vector2(112.5f,.5f);
            Base(-2,24); Base(27,84); Base(87,117);
            spikes.Add(new R("短いトゲ",12,0,2,1)); spikes.Add(new R("穴のトゲ",24,-3,3,1)); spikes.Add(new R("穴のトゲ",84,-3,3,1)); spikes.Add(new R("最後のトゲ",96,0,2,1));
            ground.Add(new R("低い段差",36,0,3,1)); ground.Add(new R("上の寄り道",43,2,4,1)); ground.Add(new R("天井を見て跳ぶ",60,4,8,1));
            Coin(45,4); items.Add(new P("Enemy",enemy,110,.5f));
            areas.Add(new A("A 見えるトゲ","トゲの手前には安全な地面があります。短いトゲをジャンプで越えましょう。",-2,22));
            areas.Add(new A("B 穴と寄り道","穴は走って跳び越えます。上のコインは余裕があれば取りましょう。",22,53));
            areas.Add(new A("C 天井とタイミング","天井の下は歩いて通れます。先の穴では、端で焦らず準備しましょう。",53,94));
            areas.Add(new A("D 仕上げ","敵は少数です。無理に戦わず、ジャンプで避けてゴールへ。",94,117));
        }
        if(n==3)
        {
            d.title="ひとつの足場、ふたつの使い方"; d.concept="空中に残すクローンを、通常の壁と天井のある壁に応用する。"; d.goalPosition=new Vector2(122.5f,.5f);
            Base(-2,20); Base(23,75); Base(78,127); Wall(30,5); Wall(86,5);
            ground.Add(new R("上の寄り道",52,2,4,1)); ground.Add(new R("天井",65,5,8,1));
            ground.Add(new R("最後の段差",109,0,3,1));
            spikes.Add(new R("穴のトゲ",20,-3,3,1)); spikes.Add(new R("短いトゲ",63,0,1,1)); spikes.Add(new R("穴のトゲ",75,-3,3,1)); Coin(54,4);
            areas.Add(new A("A 空中のクローン","高さ5の壁は普通のジャンプでは届きません。壁の少し手前で跳び、空中でクローンを残しましょう。",-2,45));
            areas.Add(new A("B 上下の分岐","上の道はコインの寄り道。下の道も通れます。",45,63));
            areas.Add(new A("C 天井と穴","天井を避けて穴を越え、次の壁にも空中のクローンを使いましょう。",63,101));
            areas.Add(new A("D 回収してゴール","残りは低い段差です。落ち着いて進みましょう。",101,127));
        }
        if(n==4)
        {
            d.title="残して動かす"; d.concept="クローンを足場とスイッチのおもりに使い分ける。"; d.goalPosition=new Vector2(134.5f,.5f);
            Base(-2,20); Base(23,108); Base(111,139); Wall(28,6);
            ground.Add(new R("上の寄り道",55,2,6,1)); ground.Add(new R("天井",68,5,8,1));
            spikes.Add(new R("穴のトゲ",20,-3,3,1)); spikes.Add(new R("短いトゲ",62,0,1,1)); spikes.Add(new R("穴のトゲ",108,-3,3,1)); Coin(58,4);

            areas.Add(new A("A 高い壁","空中のクローンを足場にして、高さ6の壁を越えましょう。",-2,44));
            areas.Add(new A("B 選べる道","上に寄り道するとコイン。下の道は安全です。",44,80));
            areas.Add(new A("C 残して開く","この先では、クローンを置いて進む使い方を学びます。",80,106));
            areas.Add(new A("D 仕上げ","最後の穴と敵を避けて進みましょう。",106,139));
        }
        if(n==5)
        {
            d.title="ふたつを同時に"; d.concept="ふたつのクローンで高い壁を越え、役割を分担する。"; d.goalPosition=new Vector2(142.5f,.5f);
            Base(-2,66); Base(69,147); Wall(41,8); Wall(94,8);
            ground.Add(new R("上の寄り道",57,2,4,1)); ground.Add(new R("天井",73,5,5,1));
            spikes.Add(new R("穴のトゲ",66,-3,3,1)); spikes.Add(new R("短いトゲ",79,0,2,1)); spikes.Add(new R("最後のトゲ",126,0,2,1)); Coin(6,1.2f); Coin(135,1.2f);
            areas.Add(new A("A ふたつの足場","最初のコインでクローンを2つに。低いクローンから跳び、2つ目を高い場所に残して高さ8の壁を越えます。",-2,56));
            areas.Add(new A("B 穴と天井","壁を降りたら、穴と短いトゲを越えます。",56,89));
            areas.Add(new A("C 役割を分担","ふたつのクローンを同時に活用する、最後の課題です。",89,120));
            areas.Add(new A("D ゴールへ","最後のトゲを越えればゴールです。",120,147));
        }
        d.terrain=ground.ToArray(); d.hazards=spikes.ToArray(); d.placements=items.ToArray(); d.areas=areas.ToArray();
        d.platforms=Array.Empty<StageLayout.PlatformPlacement>(); d.switchDoors=Array.Empty<StageLayout.SwitchDoorPlacement>();
        if(phase>=2)
        {
            int branchX = n==1 ? 21 : n==2 ? 43 : n==3 ? 52 : n==4 ? 55 : 57;
            ground.RemoveAll(r=>r.purpose=="上の寄り道");
            d.terrain=ground.ToArray();
            d.platforms=new[]{new StageLayout.PlatformPlacement("OneWay_Branch",StageLayout.PlatformKind.OneWay,new Vector2(branchX+2,2.25f),new Vector2(4,.5f))};
            if(n==1) d.areas[1]=new A("B 安全な穴と一方通行足場","穴に落ちても床があります。水色の足場は下から通り抜け、上から乗れます。コインは寄り道。",13,28);
        }
        if(phase>=3 && n>=4)
        {
            if(n==4)
            {
                d.switchDoors=new[]{new StageLayout.SwitchDoorPlacement("HoldSwitch",new Vector2(84,.06f),new Vector2(94,6),new Vector2(1,12))};
                d.areas[2]=new A("C クローンで扉を開く","黄色い床スイッチの上でクローンを残します。クローンに押してもらい、青い扉を通りましょう。",80,106);
            }
            else
            {
                ground.RemoveAll(r=>r.cells.xMin==94 || r.cells.xMin==100);
                Wall(110,5); d.terrain=ground.ToArray();
                // Recess the door into the wall: a single clone on the front edge
                // must not keep the door open through its anti-crush occupancy rule.
                d.switchDoors=new[]{new StageLayout.SwitchDoorPlacement("TwoRoles",new Vector2(92,.06f),new Vector2(112,9),new Vector2(1,18))};
                d.areas[2]=new A("C ふたつの役割","1つを黄色いスイッチに残して扉を開きます。もう1つを扉の手前の空中に残し、青い扉と高さ5の壁を越えましょう。",89,124);
                d.areas[3]=new A("D ゴールへ","スイッチのクローンを消すと扉が閉じます。越えたあとは最後のトゲを跳び越えましょう。",124,147);
            }
        }
        if(phase>=4)
        {
            var platforms=new List<StageLayout.PlatformPlacement>(d.platforms);
            if(n==2)
            {
                platforms.Add(new StageLayout.PlatformPlacement("Moving_SafePractice",StageLayout.PlatformKind.Moving,new Vector2(53,1.25f),new Vector2(3,.5f),new Vector2(4,0)));
                platforms.Add(new StageLayout.PlatformPlacement("Moving_Pit",StageLayout.PlatformKind.Moving,new Vector2(85.5f,-.25f),new Vector2(3,.5f),new Vector2(5,0)));
                ground.RemoveAll(r=>r.cells.xMin==87); Base(92,117);
                spikes.RemoveAll(r=>r.cells.xMin==84); spikes.Add(new R("移動足場の下のトゲ",84,-2,8,1));
                d.areas[2]=new A("C 緑の移動足場","まず地面の上で緑の足場に乗ってみましょう。次の穴は、足場が近づいたときに乗り、向こう岸で降ります。",50,94);
            }
            if(n==3)
            {
                platforms.Add(new StageLayout.PlatformPlacement("Collapse_SafePractice",StageLayout.PlatformKind.Collapsing,new Vector2(57,1.25f),new Vector2(2,.5f)));
                platforms.Add(new StageLayout.PlatformPlacement("Collapse_Branch",StageLayout.PlatformKind.Collapsing,new Vector2(59,2.25f),new Vector2(2,.5f)));
                d.areas[1]=new A("B オレンジの足場","オレンジの足場は乗ると点滅し、約1秒で崩れます。下は安全な地面。歩みを止めず、上のコインへ寄り道できます。",45,63);
            }
            if(n==4)
            {
                platforms.Add(new StageLayout.PlatformPlacement("Moving_Branch",StageLayout.PlatformKind.Moving,new Vector2(59,3.25f),new Vector2(2,.5f),new Vector2(4,0)));
                platforms.Add(new StageLayout.PlatformPlacement("Collapse_Branch",StageLayout.PlatformKind.Collapsing,new Vector2(65,3.25f),new Vector2(2,.5f)));
                d.areas[1]=new A("B 動く道、崩れる道","上の道は移動足場と崩れる足場を組み合わせる寄り道。下の道も通れます。",44,80);
            }
            if(n==5)
            {
                platforms.Add(new StageLayout.PlatformPlacement("Moving_Branch",StageLayout.PlatformKind.Moving,new Vector2(58,3.25f),new Vector2(2,.5f),new Vector2(4,0)));
                platforms.Add(new StageLayout.PlatformPlacement("Collapse_Branch",StageLayout.PlatformKind.Collapsing,new Vector2(64,2.25f),new Vector2(2,.5f)));
                d.areas[1]=new A("B 動きとタイミング","上の道は緑の移動足場からオレンジの足場へ。崩れる前に進みましょう。下の道は通常のジャンプで進めます。",56,89);
            }
            d.platforms=platforms.ToArray(); d.terrain=ground.ToArray(); d.hazards=spikes.ToArray();
        }
    }
}

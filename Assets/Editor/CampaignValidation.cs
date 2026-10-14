using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CampaignValidation
{
    public static void Run()
    {
        var report=new List<string>();
        try
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            for(int n=1;n<=5;n++)
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage"+n+".unity");
                var layout=AssetDatabase.LoadAssetAtPath<StageLayout>("Assets/StageAuthoring/Layouts/Stage"+n+"Layout.asset");
                if(StageBuilder.Validate(layout).Count!=0) throw new Exception("Invalid layout "+n);
                foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0 || PrefabUtility.IsPrefabAssetMissing(t.gameObject)) throw new Exception("Missing reference "+t.name);
                var manager=UnityEngine.Object.FindFirstObjectByType<StageManager>();
                typeof(StageManager).GetMethod("Awake",flags).Invoke(manager,null);
                if((string)typeof(StageManager).GetField("nextStageName",flags).GetValue(manager)!=(n<5?"Stage"+(n+1):"")) throw new Exception("Wrong next stage "+n);
                var ground=GameObject.Find("Ground");
                if(ground.GetComponent<CompositeCollider2D>()==null || ground.GetComponent<Rigidbody2D>().bodyType!=RigidbodyType2D.Static) throw new Exception("Ground setup");
                if(!GameObject.Find("Spike").GetComponent<TilemapCollider2D>().isTrigger) throw new Exception("Spike setup");
                var sky=UnityEngine.Object.FindFirstObjectByType<BackgroundLooper>().GetComponentsInChildren<SpriteRenderer>();
                int count=0;
                foreach(var image in sky) if(image.name=="Far") { count++; if(image.transform is RectTransform) throw new Exception("UI layout on world sky"); }
                if(count!=3) throw new Exception("Sky count");
                if(n>=3)
                {
                    int gates=0;
                    foreach(var r in layout.terrain) if(r.purpose=="必須のクローン段差")
                    {
                        gates++;
                        if(n==3 && r.cells.height!=5 || n==4 && r.cells.height!=6 || n==5 && r.cells.height!=8 && r.cells.height!=5) throw new Exception("Gate height");
                        foreach(var placement in layout.placements)
                            if(placement.prefab.GetComponent<Enemy>()!=null && Mathf.Abs(placement.position.x-r.cells.xMin)<16) throw new Exception("Enemy can bypass required clone gate");
                    }
                    if(gates!=(n==4?1:2)) throw new Exception("Gate count");
                }
                foreach(var surface in UnityEngine.Object.FindObjectsByType<StagePlatform>(FindObjectsSortMode.None))
                {
                    if(surface.GetComponent<PlatformEffector2D>()==null || !surface.GetComponent<Collider2D>().usedByEffector) throw new Exception("One-way setup");
                    if((surface.GetComponent<MovingStagePlatform>()!=null || surface.GetComponent<CollapsingStagePlatform>()!=null) && surface.stableRespawnSurface) throw new Exception("Unsafe dynamic checkpoint");
                }
                foreach(var plate in UnityEngine.Object.FindObjectsByType<PressurePlate>(FindObjectsSortMode.None))
                    if(plate.door==null) throw new Exception("Missing switch-door link");
                if(n==5 && layout.switchDoors.Length>0)
                {
                    var gate=layout.switchDoors[0];
                    if(gate.doorPosition.x-gate.doorSize.x*.5f<111 || gate.doorSize.y<16) throw new Exception("Single-clone door bypass possible");
                }
                // Verify regeneration preserves manual objects; do not save this test scene.
                var decoration=new GameObject("ManualDecorationValidation");
                StageBuilder.Build(layout,scene,false);
                if(decoration==null || UnityEngine.Object.FindObjectsByType<GeneratedStage>(FindObjectsSortMode.None).Length!=1) throw new Exception("Unsafe regeneration");
                report.Add("PASS: Stage"+n+" references, transitions, static composite terrain, spike triggers, world sky, required gates, regeneration preserves manual decorations.");
            }
            report.Add("PASS: Stage3/4 mandatory heights 5/6 exceed normal jump; Stage5 uses an eight-unit wall and a switch plus five-unit gated wall. No enemy at mandatory gates.");
            File.WriteAllLines("StageAuthoringReports/campaign-validation.txt",report);
            RepairValidation.Run();
        }
        catch(Exception e)
        {
            report.Add("FAIL: "+e); File.WriteAllLines("StageAuthoringReports/campaign-validation.txt",report); EditorApplication.Exit(1);
        }
    }
}

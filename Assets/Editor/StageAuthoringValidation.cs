using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class StageAuthoringValidation
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(StageAuthoringBootstrap.ScenePath);
            var data = AssetDatabase.LoadAssetAtPath<StageLayout>(StageAuthoringBootstrap.LayoutPath);
            if (StageBuilder.Validate(data).Count != 0) throw new Exception("Invalid stage data");
            var external = new GameObject("ManualDecorationValidation");
            StageBuilder.Build(data, scene, false);
            if (external == null) throw new Exception("Manual decoration was removed");
            if (UnityEngine.Object.FindObjectsByType<GeneratedStage>(FindObjectsSortMode.None).Length != 1)
                throw new Exception("Duplicate generated root");
            var ground = GameObject.Find("Ground");
            if (ground.GetComponent<CompositeCollider2D>() == null || ground.GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Static)
                throw new Exception("Ground is not static/composite");
            if (!GameObject.Find("Spike").GetComponent<TilemapCollider2D>().isTrigger) throw new Exception("Spike is not a trigger");
            if (GameObject.Find("AudioManager").transform.parent != null) throw new Exception("AudioManager is not a root");
            if (GameObject.Find("Goal").GetComponent<Rigidbody2D>().bodyType != RigidbodyType2D.Static) throw new Exception("Goal is not static");
            File.WriteAllText("StageAuthoringReports/validation.txt", "PASS: references, static composite terrain, spike trigger, root AudioManager, static goal, regeneration preserves manual decoration and creates exactly one generated root.\n");
            // The existing gameplay/camera regression suite also runs on the rebuilt Stage1.
            RepairValidation.Run();
        }
        catch (Exception e)
        {
            File.WriteAllText("StageAuthoringReports/validation.txt", "FAIL: " + e);
            EditorApplication.Exit(1);
        }
    }
}

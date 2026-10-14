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
                var scene = File.Exists(path) ? EditorSceneManager.OpenScene(path) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
    static void Configure(StageLayout d, int n) => StageImprovementBuilder.Configure(d, n, 4);
}

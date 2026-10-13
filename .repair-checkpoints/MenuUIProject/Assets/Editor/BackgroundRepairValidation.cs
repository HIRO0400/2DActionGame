using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

public static class BackgroundRepairValidation
{
    public static void RepairStage1()
    {
        // Update only the existing background instance; preserve stage edits.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage1.unity");
        var loop = UnityEngine.Object.FindFirstObjectByType<BackgroundLooper>();
        var settings = new SerializedObject(loop);
        StageBackgroundConfiguration.Configure(loop.gameObject, (Transform)settings.FindProperty("cameraTransform").objectReferenceValue);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Run();
    }
    public static void Run()
    {
        var results = new List<string>();
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Stage/Stage1.unity");
            var loop = UnityEngine.Object.FindFirstObjectByType<BackgroundLooper>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var settings = new SerializedObject(loop);
            var camera = (Transform)settings.FindProperty("cameraTransform").objectReferenceValue;
            var brain = camera.GetComponent<CinemachineBrain>();
            var panels = loop.GetComponentsInChildren<ParallaxBackground>();
            var callback = typeof(BackgroundLooper).GetMethod("OnCameraUpdated", flags);
            foreach (var panel in panels)
            {
                typeof(ParallaxBackground).GetMethod("Awake", flags).Invoke(panel, null);
                foreach (var sprite in panel.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.name == "Far") results.Add(panel.name + " Far width=" + sprite.bounds.size.x + ", x=" + panel.transform.position.x);
            }
            float span = settings.FindProperty("backgroundWidth").floatValue * panels.Length;
            int frames = 0;
            foreach (float target in new[] { 2.5f, 8f, 20f, 45f, 70f, 150f, -100f, 3f })
            {
                camera.position = new Vector3(target, 3f, camera.position.z);
                callback.Invoke(loop, new object[] { brain });
                var positions = Array.ConvertAll(panels, p => p.transform.position);
                for (int i = 0; i < 120; i++)
                {
                    callback.Invoke(loop, new object[] { brain });
                    // Exercise either listener order; delegated parallax must not undo wrapping.
                    foreach (var p in panels)
                        typeof(ParallaxBackground).GetMethod("OnCameraUpdated", flags).Invoke(p, new object[] { brain });
                    for (int j = 0; j < panels.Length; j++)
                        if ((panels[j].transform.position - positions[j]).sqrMagnitude > 0.00001f)
                            throw new Exception("Stationary camera causes panel movement at camera x=" + target);
                    frames++;
                }
            }
            for (int i = 0; i < 2400; i++)
            {
                camera.position = new Vector3(-20f + (i <= 1200 ? i : 2400 - i) * 0.1f, 2f, camera.position.z);
                callback.Invoke(loop, new object[] { brain });
                foreach (var panel in panels)
                    if (Mathf.Abs(camera.position.x - panel.transform.position.x) > span * 0.5f + 0.001f)
                        throw new Exception("Panel outside stable wrap range");
                frames++;
            }
            results.Add("PASS: " + frames + " continuous callback frames; stationary camera, left/right travel, teleports, listener order; no repeated reverse wrapping.");
            File.WriteAllLines("StageAuthoringReports/background-validation.txt", results);
            RepairValidation.Run();
        }
        catch (Exception e)
        {
            results.Add("FAIL: " + e);
            File.WriteAllLines("StageAuthoringReports/background-validation.txt", results);
            EditorApplication.Exit(1);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class StageAuthoringWindow : EditorWindow
{
    private StageLayout layout;
    private Vector2 scroll;
    private bool scenePreview = true;
    [MenuItem("Tools/Stage Authoring/ステージ制作")]
    public static void Open() => GetWindow<StageAuthoringWindow>("ステージ制作");
    private void OnEnable() => SceneView.duringSceneGui += DrawScenePreview;
    private void OnDisable() => SceneView.duringSceneGui -= DrawScenePreview;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("配置データからステージを制作", EditorStyles.boldLabel);
        layout = (StageLayout)EditorGUILayout.ObjectField("配置データ", layout, typeof(StageLayout), false);
        scenePreview = EditorGUILayout.Toggle("Sceneに配置とジャンプ目安を表示", scenePreview);
        EditorGUILayout.HelpBox("1マス＝1ユニット。生成ルートの外に置いた装飾は再生成しても残ります。配置の編集はデータのInspectorで行います。", MessageType.Info);
        if (GUILayout.Button("Stage1の配置データを開く"))
        {
            layout = AssetDatabase.LoadAssetAtPath<StageLayout>("Assets/StageAuthoring/Layouts/Stage1Layout.asset");
            Selection.activeObject = layout;
        }
        if (layout != null)
        {
            EditorGUILayout.LabelField(layout.title + "  / 難易度 " + layout.difficulty + "/5");
            EditorGUILayout.LabelField(layout.concept, EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("配置と参照をチェック"))
            {
                var issues = StageBuilder.Validate(layout);
                EditorUtility.DisplayDialog("配置チェック", issues.Count == 0 ? "必須参照・開始地点・ゴールを確認しました。到達性はPlay Modeで確認してください。" : string.Join("\n", issues), "閉じる");
            }
            if (GUILayout.Button("新規Sceneを作成"))
            {
                string path = EditorUtility.SaveFilePanelInProject("ステージ保存先", layout.sceneName, "unity", "既存Sceneへの上書きは行いません。", "Assets/Scenes/Stage");
                if (!string.IsNullOrEmpty(path) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    try { StageBuilder.CreateScene(layout, path); }
                    catch (Exception e) { EditorUtility.DisplayDialog("作成できません", e.Message, "閉じる"); }
                }
            }
            if (GUILayout.Button("現在のSceneの生成配置だけを更新"))
            {
                var root = UnityEngine.Object.FindFirstObjectByType<GeneratedStage>();
                if (root == null || root.layout != layout)
                    EditorUtility.DisplayDialog("更新できません", "選択した配置データで生成したSceneを開いてください。", "閉じる");
                else if (EditorUtility.DisplayDialog("生成配置を更新", "生成ルート内の手修正は置き換わります。ルート外の装飾は残り、Undoでも戻せます。", "更新", "キャンセル"))
                {
                    Undo.IncrementCurrentGroup();
                    int group = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("ステージ再生成");
                    try
                    {
                        StageBuilder.Build(layout, root.gameObject.scene, true);
                        Undo.CollapseUndoOperations(group);
                    }
                    catch (Exception e)
                    {
                        Undo.RevertAllDownToGroup(group);
                        EditorUtility.DisplayDialog("更新できません", e.Message, "閉じる");
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawScenePreview(SceneView view)
    {
        if (!scenePreview || layout == null) return;
        DrawRegions(layout.terrain, new Color(0.2f, 0.8f, 0.4f, 0.16f));
        DrawRegions(layout.hazards, new Color(1f, 0.2f, 0.2f, 0.25f));
        foreach (var area in layout.areas)
            Handles.Label(new Vector3(area.fromX, 7f), area.title + "\n" + area.instruction);
        foreach (var obj in layout.placements)
            Handles.Label(obj.position, obj.label);
        Handles.color = Color.cyan;
        Vector3 origin = new Vector3(layout.playerStart.x, 0f);
        // Approximation only: full hold, speed 3.6, initial vertical velocity 6.
        var points = new List<Vector3>();
        float y = 0f, vy = 6f;
        for (float t = 0; t < 1.6f; t += 0.02f)
        {
            points.Add(origin + new Vector3(t * 3.6f, y));
            vy += ((t < 0.3f ? 10f : 0f) - (vy < 0 ? 24.525f : 9.81f)) * 0.02f;
            y += vy * 0.02f;
            if (t > 0 && y < 0) break;
        }
        Handles.DrawAAPolyLine(3f, points.ToArray());
        Handles.Label(origin + Vector3.up * 4f, "標準Playerの最大長押しジャンプ概算（実測で調整）");
    }
    private static void DrawRegions(StageLayout.GroundRegion[] regions, Color color)
    {
        foreach (var region in regions)
        {
            RectInt r = region.cells;
            Vector3[] corners = { new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax) };
            Handles.DrawSolidRectangleWithOutline(corners, color, color * 2f);
        }
    }
}

public static class StageBuilder
{
    public static List<string> Validate(StageLayout data)
    {
        var errors = new List<string>();
        if (data == null) { errors.Add("配置データがありません。"); return errors; }
        if (data.playerPrefab == null || data.playerPrefab.GetComponent<Player>() == null) errors.Add("Player Prefabを指定してください。");
        if (data.goalPrefab == null || data.goalPrefab.GetComponent<Goal>() == null) errors.Add("Goal Prefabを指定してください。");
        if (data.stageManagerPrefab == null || data.stageManagerPrefab.GetComponent<StageManager>() == null) errors.Add("StageManager Prefabを指定してください。");
        if (data.audioManagerPrefab == null) errors.Add("AudioManager Prefabを指定してください。");
        if (data.groundTile == null || data.spikeTile == null) errors.Add("GroundとSpikeのTileを指定してください。");
        foreach (string layer in new[] { "Ground", "Spike", "Clone", "Player" })
            if (LayerMask.NameToLayer(layer) < 0) errors.Add(layer + "レイヤーがありません。");
        if (!HasGroundBelow(data, data.playerStart)) errors.Add("開始位置の直下に地面がありません。");
        if (!HasGroundBelow(data, data.goalPosition)) errors.Add("ゴール直下に地面がありません。");
        foreach (var placement in data.placements)
            if (placement.prefab == null) errors.Add(placement.label + "のPrefab参照がありません。");
        foreach (var region in data.terrain)
            if (region.cells.width <= 0 || region.cells.height <= 0) errors.Add("地形範囲の幅・高さは正数にしてください。");
        return errors;
    }
    private static bool HasGroundBelow(StageLayout data, Vector2 p)
    {
        foreach (var r in data.terrain)
            if (p.x >= r.cells.xMin && p.x < r.cells.xMax && p.y >= r.cells.yMax && p.y - r.cells.yMax < 2f) return true;
        return false;
    }

    public static Scene CreateScene(StageLayout data, string path)
    {
        if (File.Exists(path)) throw new InvalidOperationException("既存Sceneへの上書きはできません：" + path);
        if (data != null && Path.GetFileNameWithoutExtension(path) != data.sceneName)
            throw new InvalidOperationException("Sceneの保存名を配置データのsceneNameに合わせてください。");
        var errors = Validate(data);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        string dataPath = AssetDatabase.GetAssetPath(data);
        if (string.IsNullOrEmpty(dataPath)) throw new InvalidOperationException("配置データをアセットとして保存してください。");
        AssetDatabase.SaveAssetIfDirty(data);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        data = AssetDatabase.LoadAssetAtPath<StageLayout>(dataPath);
        Build(data, scene, false);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Sceneを保存できませんでした。");
        return scene;
    }

    public static GeneratedStage Build(StageLayout data, Scene scene, bool undo)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play Modeを終了してから生成してください。");
        var errors = Validate(data);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        foreach (var old in scene.GetRootGameObjects())
        {
            if (old == null) continue;
            var generated = old.GetComponent<GeneratedStage>();
            if (generated == null) continue;
            foreach (var external in generated.externalRoots ?? Array.Empty<GameObject>())
                if (external != null) { if (undo) Undo.DestroyObjectImmediate(external); else UnityEngine.Object.DestroyImmediate(external); }
            if (undo) Undo.DestroyObjectImmediate(old); else UnityEngine.Object.DestroyImmediate(old);
        }
        var root = new GameObject("Generated_" + data.sceneName);
        SceneManager.MoveGameObjectToScene(root, scene);
        var marker = root.AddComponent<GeneratedStage>();
        marker.layout = data;
        var gridObject = new GameObject("Grid", typeof(Grid));
        gridObject.transform.SetParent(root.transform, false);
        gridObject.GetComponent<Grid>().cellSize = Vector3.one;
        var ground = CreateTilemap("Ground", gridObject.transform, "Ground", data.groundTile, data.terrain, false, data.groundMaterial);
        CreateTilemap("Spike", gridObject.transform, "Spike", data.spikeTile, data.hazards, true, null);
        var managerObject = Place(data.stageManagerPrefab, "StageManager", Vector2.zero, root.transform, scene);
        var manager = managerObject.GetComponent<StageManager>();
        var managerSettings = new SerializedObject(manager);
        int number;
        managerSettings.FindProperty("stageNumber").intValue = int.TryParse(data.sceneName.Replace("Stage", ""), out number) ? number : 1;
        managerSettings.FindProperty("nextStageName").stringValue = "";
        managerSettings.ApplyModifiedPropertiesWithoutUndo();
        var playerObject = Place(data.playerPrefab, "Player", data.playerStart, root.transform, scene);
        var goalObject = Place(data.goalPrefab, "Goal", data.goalPosition, root.transform, scene);
        var goalBody = goalObject.GetComponent<Rigidbody2D>();
        if (goalBody != null) goalBody.bodyType = RigidbodyType2D.Static;
        var goalSettings = new SerializedObject(goalObject.GetComponent<Goal>());
        goalSettings.FindProperty("stageManager").objectReferenceValue = manager;
        goalSettings.ApplyModifiedPropertiesWithoutUndo();
        // AudioManager's existing DontDestroyOnLoad requires an actual Scene root.
        var audio = Place(data.audioManagerPrefab, "AudioManager", Vector2.zero, null, scene);
        marker.externalRoots = new[] { audio };
        if (data.bgmPrefab != null) Place(data.bgmPrefab, "BGM", Vector2.zero, root.transform, scene);
        foreach (var obj in data.placements) Place(obj.prefab, obj.label, obj.position, root.transform, scene);
        var guide = root.AddComponent<StageGuide>();
        guide.layout = data;
        guide.player = playerObject.transform;
        var camera = playerObject.GetComponentInChildren<Camera>();
        if (camera != null) camera.backgroundColor = new Color(0.12f, 0.19f, 0.27f);
        if (data.backgroundPrefab != null && camera != null)
        {
            var background = (GameObject)PrefabUtility.InstantiatePrefab(data.backgroundPrefab, scene);
            background.transform.SetParent(root.transform, true);
            StageBackgroundConfiguration.Configure(background, camera.transform);
        }
        if (undo)
        {
            Undo.RegisterCreatedObjectUndo(root, "ステージ生成");
            Undo.RegisterCreatedObjectUndo(audio, "音声配置");
        }
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        return marker;
    }

    private static Tilemap CreateTilemap(string name, Transform parent, string layer, TileBase tile,
        StageLayout.GroundRegion[] regions, bool trigger, PhysicsMaterial2D material)
    {
        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(parent, false);
        obj.layer = LayerMask.NameToLayer(layer);
        var map = obj.GetComponent<Tilemap>();
        foreach (var region in regions)
            foreach (var cell in region.cells.allPositionsWithin)
                map.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
        var collider = obj.AddComponent<TilemapCollider2D>();
        collider.isTrigger = trigger;
        collider.sharedMaterial = material;
        if (!trigger)
        {
            obj.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = obj.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.sharedMaterial = material;
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            collider.extrusionFactor = 0.0001f;
        }
        collider.ProcessTilemapChanges();
        map.CompressBounds();
        return map;
    }
    private static GameObject Place(GameObject prefab, string name, Vector2 p, Transform parent, Scene scene)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.name = name;
        obj.transform.position = new Vector3(p.x, p.y, 0);
        return obj;
    }
}

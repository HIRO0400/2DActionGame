using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class GimmickPrefabLibrary
{
    public const string Folder = "Assets/Prefab/Gimmicks";
    public const string Tiles = "Assets/TilePalettes/Gimmicks";

    [MenuItem("Tools/Stage Authoring/Create Missing Gimmick Prefabs")]
    public static void Create()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Tiles);
        AssetDatabase.Refresh();
        var source = AssetDatabase.LoadAssetAtPath<StageLayout>(StageAuthoringBootstrap.LayoutPath);
        if (source == null || !(source.groundTile is Tile ground) || ground.sprite == null)
            throw new InvalidOperationException("Stage1Layout ground tile is required.");
        var scene = EditorSceneManager.NewPreviewScene();
        var layout = ScriptableObject.CreateInstance<StageLayout>();
        try
        {
            var root = new GameObject("GimmickLibrary");
            SceneManager.MoveGameObjectToScene(root, scene);
            layout.groundTile = source.groundTile;
            layout.groundMaterial = source.groundMaterial;
            layout.platforms = new[] {
                new StageLayout.PlatformPlacement("OneWayPlatform", StageLayout.PlatformKind.OneWay, Vector2.zero, new Vector2(3,.4f)),
                new StageLayout.PlatformPlacement("MovingPlatform", StageLayout.PlatformKind.Moving, Vector2.zero, new Vector2(3,.4f), new Vector2(5,0)),
                new StageLayout.PlatformPlacement("CollapsingPlatform", StageLayout.PlatformKind.Collapsing, Vector2.zero, new Vector2(3,.4f))
            };
            layout.switchDoors = new[] { new StageLayout.SwitchDoorPlacement("SwitchDoor", new Vector2(-4,.06f), new Vector2(0,2), new Vector2(1,4)) };
            StageGimmickBuilder.Build(layout, root.transform);
            foreach (var label in new[] { "OneWayPlatform", "MovingPlatform", "CollapsingPlatform" })
                Save(root.transform.Find(label).gameObject, label);
            var pair = new GameObject("PressureSwitchDoor");
            pair.transform.SetParent(root.transform, false);
            root.transform.Find("SwitchDoor_Plate").SetParent(pair.transform, true);
            root.transform.Find("SwitchDoor_Door").SetParent(pair.transform, true);
            Save(pair, pair.name);

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(Tiles + "/OneWayFloor.asset");
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = ground.sprite;
                tile.color = new Color(.3f,.85f,.95f);
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, Tiles + "/OneWayFloor.asset");
            }
            var grid = new GameObject("OneWayTilemap", typeof(Grid));
            grid.transform.SetParent(root.transform, false);
            var mapObject = new GameObject("OneWayFloor", typeof(Tilemap), typeof(TilemapRenderer));
            mapObject.transform.SetParent(grid.transform, false);
            mapObject.layer = LayerMask.NameToLayer("Ground");
            mapObject.GetComponent<TilemapRenderer>().sortingOrder = 2;
            var body = mapObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tileCollider = mapObject.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = mapObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.sharedMaterial = source.groundMaterial;
            composite.usedByEffector = true;
            var effector = mapObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = effector.useOneWayGrouping = true;
            effector.surfaceArc = 180;
            effector.useSideFriction = effector.useSideBounce = false;
            mapObject.AddComponent<StagePlatform>();
            var map = mapObject.GetComponent<Tilemap>();
            for (int x = 0; x < 3; x++) map.SetTile(new Vector3Int(x,0,0), tile);
            Save(grid, grid.name);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(Tiles + "/GimmickPalette.prefab") == null)
            {
                var palette = new GameObject("GimmickPalette", typeof(Grid));
                palette.transform.SetParent(root.transform, false);
                var paletteLayer = new GameObject("OneWayFloor", typeof(Tilemap), typeof(TilemapRenderer));
                paletteLayer.transform.SetParent(palette.transform, false);
                paletteLayer.GetComponent<Tilemap>().SetTile(Vector3Int.zero, tile);
                var asset = PrefabUtility.SaveAsPrefabAsset(palette, Tiles + "/GimmickPalette.prefab");
                var settings = ScriptableObject.CreateInstance<GridPalette>();
                settings.name = "Palette Settings";
                settings.cellSizing = GridPalette.CellSizing.Manual;
                AssetDatabase.AddObjectToAsset(settings, asset);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Gimmick prefabs and tile palette are ready: " + Folder);
        }
        finally { UnityEngine.Object.DestroyImmediate(layout); EditorSceneManager.ClosePreviewScene(scene); }
    }
    static void Save(GameObject instance, string name)
    {
        string path = Folder + "/" + name + ".prefab";
        // Preserve edits made by the level designer when this command is run again.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            PrefabUtility.SaveAsPrefabAsset(instance, path);
    }
    public static void BatchCreate()
    {
        try { Create(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}

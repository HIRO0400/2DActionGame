using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class GimmickPrefabValidation
{
    static GimmickPrefabValidation()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("GimmickPrefabTest", false)) {
                SessionState.SetBool("GimmickPrefabTest", false);
                new GameObject("PrefabTests").AddComponent<GimmickPrefabTestRunner>();
            }
        };
    }
    public static void Run()
    {
        SessionState.SetString("GimmickPrefabPrefs", JsonUtility.ToJson(ReleaseUIPrefs.Capture()));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool("GimmickPrefabTest", true);
        EditorApplication.EnterPlaymode();
    }
}
public class GimmickPrefabTestRunner : MonoBehaviour
{
    readonly List<string> report = new List<string>();
    Player player; Keyboard keyboard; bool finished;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
    InputSettings.UpdateMode updateMode;
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    T Field<T>(string name) => (T)typeof(Player).GetField(name, Flags).GetValue(player);
    void Check(bool ok, string label)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + label);
        Directory.CreateDirectory("StageAuthoringReports/GimmickPrefabs");
        File.WriteAllLines("StageAuthoringReports/GimmickPrefabs/playtest.txt", report);
        if (!ok) { Finish(false); throw new Exception(label); }
    }
    void Finish(bool ok)
    {
        if (finished) return; finished = true;
        JsonUtility.FromJson<ReleaseUIPrefs>(SessionState.GetString("GimmickPrefabPrefs", "{}")).Restore();
        InputSystem.settings.backgroundBehavior = background;
        InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        InputSystem.settings.updateMode = updateMode;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        EditorApplication.Exit(ok ? 0 : 1);
    }
    void Update() { if (!finished && Time.realtimeSinceStartup > 150) Check(false,"Timeout"); }
    GameObject Spawn(string name, Vector2 position)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(GimmickPrefabLibrary.Folder + "/" + name + ".prefab");
        Check(asset != null, name + " asset exists");
        return Instantiate(asset, position, Quaternion.identity);
    }
    void Place(float x, float footY)
    {
        var body = player.GetComponent<Rigidbody2D>();
        float offset = Field<Transform>("footCheck").position.y - player.transform.position.y;
        body.position = new Vector2(x, footY - offset);
        body.linearVelocity = Vector2.zero;
        typeof(Player).GetField("lastGroundPosition", Flags).SetValue(player, new Vector3(x,0,0));
        Physics2D.SyncTransforms();
    }
    IEnumerator Start()
    {
        background = InputSystem.settings.backgroundBehavior;
        editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        updateMode = InputSystem.settings.updateMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>();
        var floor = new GameObject("TestGround", typeof(BoxCollider2D));
        floor.layer = LayerMask.NameToLayer("Ground");
        floor.transform.position = new Vector3(50,-.5f);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(110,1);
        player = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab"),new Vector3(0,.5f),Quaternion.identity).GetComponent<Player>();
        player.GetComponent<PlayerInput>().actions.devices = new InputDevice[] { keyboard };
        var pair = Spawn("PressureSwitchDoor",new Vector2(20,0));
        var plate = pair.GetComponentInChildren<PressurePlate>();
        var door = pair.GetComponentInChildren<PressureDoor>();
        Check(plate.door == door, "Switch and door reference survives prefab instantiation");
        var moving = Spawn("MovingPlatform",new Vector2(40,2)).GetComponent<MovingStagePlatform>();
        var collapse = Spawn("CollapsingPlatform",new Vector2(60,2)).GetComponent<CollapsingStagePlatform>();
        var oneWay = Spawn("OneWayPlatform",new Vector2(10,2));
        var grid = Spawn("OneWayTilemap",new Vector2(80,1));
        var map = grid.GetComponentInChildren<Tilemap>();
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(GimmickPrefabLibrary.Tiles + "/OneWayFloor.asset");
        for (int x = 6; x < 9; x++) map.SetTile(new Vector3Int(x,3,0),tile);
        map.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        map.GetComponent<CompositeCollider2D>().GenerateGeometry();
        Check(Array.Exists(AssetDatabase.LoadAllAssetsAtPath(GimmickPrefabLibrary.Tiles + "/GimmickPalette.prefab"),a => a is GridPalette),"Tile palette has recognised palette settings");
        yield return new WaitForSecondsRealtime(.3f);
        Place(plate.transform.position.x,.01f); yield return new WaitForSecondsRealtime(.15f);
        Check(plate.IsPressed && door.IsOpen && !door.GetComponent<BoxCollider2D>().enabled,"Player opens door by standing on prefab switch");
        Place(0,.01f); yield return new WaitForSecondsRealtime(.1f);
        Check(!plate.IsPressed && !door.IsOpen,"Door closes after player leaves switch");
        var clone = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/クローン_0.prefab"),new Vector3(plate.transform.position.x,.46f),Quaternion.identity);
        yield return new WaitForSecondsRealtime(.15f);
        Check(plate.IsPressed && door.IsOpen,"Static clone holds prefab switch open");
        Place(door.transform.position.x,.01f); Destroy(clone); yield return new WaitForSecondsRealtime(.15f);
        Check(door.IsOpen,"Door does not close inside player");
        Place(0,.01f); yield return new WaitForSecondsRealtime(.15f);
        Check(!door.IsOpen,"Door closes when doorway becomes clear");
        Place(moving.transform.position.x,moving.GetComponent<Collider2D>().bounds.max.y+.02f);
        yield return new WaitForSecondsRealtime(.25f);
        Check(Field<bool>("isGrounded"),"Player boards moved prefab platform");
        float offsetX = player.transform.position.x - moving.transform.position.x;
        yield return new WaitForSecondsRealtime(.5f);
        Check(Mathf.Abs(player.transform.position.x-moving.transform.position.x-offsetX)<.2f,"Moving prefab carries player");
        Vector3 stopped = moving.transform.position;
        GameMenus.Instance.Pause(); yield return new WaitForSecondsRealtime(.3f);
        Check(Vector3.Distance(stopped,moving.transform.position)<.001f,"Pause freezes moving prefab");
        GameMenus.Instance.Resume();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return new WaitForSecondsRealtime(.2f);
        Check(player.GetComponent<Rigidbody2D>().linearVelocity.y>1,"Player jumps off moving prefab");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Place(60,collapse.GetComponent<Collider2D>().bounds.max.y+.02f); yield return new WaitForSecondsRealtime(.15f);
        Check(collapse.IsCountingDown,"Standing starts prefab collapse warning");
        yield return new WaitForSecondsRealtime(1f);
        Check(collapse.IsCollapsed && !collapse.GetComponent<Collider2D>().enabled,"Collapse prefab removes collision");
        Place(65,.01f); yield return new WaitForSecondsRealtime(3.2f);
        Check(!collapse.IsCollapsed && collapse.GetComponent<Collider2D>().enabled,"Collapse prefab restores collision");
        foreach (var position in new[] { new Vector2(80.5f,2),new Vector2(86.5f,5) })
        {
            Place(position.x,position.y+.02f); yield return new WaitForSecondsRealtime(.15f);
            Check(Field<bool>("isGrounded") && Field<Collider2D>("currentGround").gameObject == map.gameObject,"Same tilemap supports local height " + position.y);
        }
        Place(80.5f,.01f); yield return new WaitForSecondsRealtime(.15f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
        float start = Time.time; bool crossed = false, falseGround = false;
        while (Time.time-start<1.8f)
        {
            float foot = Field<Transform>("footCheck").position.y;
            if (foot>2.1f) crossed = true;
            if (foot>1.05f && foot<1.85f && Field<bool>("isGrounded")) falseGround = true;
            if (Time.time-start>.9f) InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return null;
        }
        Check(crossed && !falseGround,"Painted tilemap allows jumping through from below without false grounding");
        Check(Field<bool>("isGrounded") && Field<Collider2D>("currentGround").gameObject == map.gameObject,"Player lands on painted lower row despite higher row in same tilemap");
        Finish(true);
    }
}

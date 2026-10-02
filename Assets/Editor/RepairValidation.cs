using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Run with Unity -batchmode -executeMethod RepairValidation.Run.
public static class RepairValidation
{
    private static readonly List<string> Results = new List<string>();
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        Results.Add("PASS: " + label);
    }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Flags).SetValue(target, value);
    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, Flags).GetValue(target);
    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, Flags).Invoke(target, null);

    public static void Run()
    {
        int exitCode = 0;
        try
        {
            foreach (string name in new[] { "Stage1", "Stage2" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage/" + name + ".unity");
                var manager = UnityEngine.Object.FindFirstObjectByType<StageManager>();
                Check(manager != null, name + " has a stage manager");
                Call(manager, "Awake");
                Check(Get<int>(manager, "stageNumber") == (name == "Stage1" ? 1 : 2), name + " stage number");
                Check(Get<string>(manager, "nextStageName") == (name == "Stage1" ? "Stage2" : ""), name + " transition");
            }
            foreach (string path in new[] { "Assets/Scenes/Terrain.unity", "Assets/Scenes/SampleU.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        Check(!PrefabUtility.IsPrefabAssetMissing(t.gameObject), path + " prefab " + t.name);
                var player = UnityEngine.Object.FindFirstObjectByType<Player>();
                Check(player != null && Get<GameObject>(player, "playerPrefab") != null, path + " clone reference");
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = new GameObject("ValidationGround");
            ground.layer = LayerMask.NameToLayer("Ground");
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(20f, 1f);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var p = playerObject.GetComponent<Player>();
            playerObject.transform.position = new Vector3(0, 0.46f, 0);
            Call(p, "Awake");
            Call(p, "Start");
            var rb = p.GetComponent<Rigidbody2D>();
            Physics2D.SyncTransforms();
            Call(p, "CheckGround");
            Check(Get<bool>(p, "isGrounded"), "flat ground detected");
            Set(p, "moveInput", new Vector2(1, 0));
            Call(p, "Move");
            Check(Mathf.Approximately(rb.linearVelocity.x, 6f), "ground movement speed preserved");
            playerObject.transform.position = new Vector3(3, 0.46f, 0);
            Physics2D.SyncTransforms();
            Call(p, "CheckGround");
            Check(Mathf.Approximately(Get<Vector3>(p, "lastGroundPosition").x, 3f), "walking updates respawn location");
            Set(p, "jumpPressed", true);
            Set(p, "jumpHeld", false);
            Call(p, "HandleJump");
            Check(Mathf.Approximately(rb.linearVelocity.y, 6f), "jump initial speed preserved");
            Check(Get<float>(p, "coyoteCounter") == 0f, "jump consumes coyote time");
            Call(p, "CheckGround");
            Check(!Get<bool>(p, "isGrounded"), "ascending player cannot reset jump grace");
            rb.linearVelocity = Vector2.zero;
            playerObject.transform.position = new Vector3(3, 3, 0);
            Physics2D.SyncTransforms();
            Call(p, "CheckGround");
            Set(p, "moveInput", Vector2.right);
            Call(p, "Move");
            Check(Mathf.Approximately(rb.linearVelocity.x, 3.6f), "air control preserved");
            var clonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/クローン_0.prefab");
            var cloneObject = (GameObject)PrefabUtility.InstantiatePrefab(clonePrefab);
            cloneObject.transform.position = new Vector3(3, 3, 0);
            Physics2D.SyncTransforms();
            float cloneTop = cloneObject.GetComponent<Collider2D>().bounds.max.y;
            playerObject.transform.position = new Vector3(3, cloneTop + 0.46f, 0);
            rb.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Call(p, "CheckGround");
            Check(Get<bool>(p, "isGrounded"), "clone supports player landing");
            Check((bool)typeof(Player).GetMethod("IsStandingOnClone", Flags).Invoke(p, null), "clone standing restriction preserved");
            UnityEngine.Object.DestroyImmediate(cloneObject);
            var trigger = new GameObject("ValidationTrigger");
            trigger.layer = LayerMask.NameToLayer("Ground");
            trigger.transform.position = new Vector3(3, cloneTop, 0);
            var triggerCollider = trigger.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            Physics2D.SyncTransforms();
            Call(p, "CheckGround");
            Check(!Get<bool>(p, "isGrounded"), "trigger cannot act as solid ground");
            UnityEngine.Object.DestroyImmediate(trigger);
            var invincible = typeof(Player).GetProperty("isInvincible", Flags);
            foreach (string field in new[] { "isRespawning", "isGroundLocked", "isBlinkInvincible" })
            {
                Set(p, field, true);
                Check((bool)invincible.GetValue(p), field + " protects player independently");
                Set(p, field, false);
            }
            Check(!(bool)invincible.GetValue(p), "invincibility ends when all protections end");
            p.Bounce(10f);
            Check(rb.linearVelocity.y == 10f, "enemy bounce preserved");
            int capacity = Get<int>(p, "maxClones");
            p.AddCloneCapacity(1);
            Check(Get<int>(p, "maxClones") == capacity + 1, "clone capacity growth preserved");

            var coinObject = new GameObject("ValidationCoin");
            var coin = coinObject.AddComponent<Coin>();
            var collect = typeof(Coin).GetMethod("OnTriggerEnter2D", Flags);
            int beforeCapacity = Get<int>(p, "maxClones");
            var playerColliders = p.GetComponents<Collider2D>();
            // Destroy is a Play Mode API; test the duplicate-notification guard without
            // invoking object destruction from an Edit Mode validation run.
            Set(coin, "collected", true);
            collect.Invoke(coin, new object[] { playerColliders[0] });
            collect.Invoke(coin, new object[] { playerColliders[1] });
            Check(Get<int>(p, "maxClones") == beforeCapacity, "collected coin ignores both player colliders");

            Set(p, "isGroundLocked", true);
            var routine = (IEnumerator)typeof(Player).GetMethod("RespawnRoutine", Flags).Invoke(p, null);
            Check(routine.MoveNext(), "respawn starts");
            Vector3 safeGround = Get<Vector3>(p, "lastGroundPosition");
            Check(Mathf.Abs(rb.position.x - safeGround.x) < 0.001f &&
                Mathf.Abs(rb.position.y - safeGround.y - 3f) < 0.001f, "respawn position and height preserved");
            Check(rb.linearVelocity == Vector2.zero, "respawn clears velocity");
            while (routine.MoveNext()) { }
            Check(!Get<bool>(p, "isRespawning") && (bool)invincible.GetValue(p), "respawn cannot cancel landing protection");
            Set(p, "isGroundLocked", false);
            var blink = (IEnumerator)typeof(Player).GetMethod("InvincibleRoutine", Flags).Invoke(p, null);
            blink.MoveNext();
            Check((bool)invincible.GetValue(p), "death blink protects player");
            int steps = 0;
            while (blink.MoveNext() && ++steps < 200) { }
            Check(steps < 200 && !(bool)invincible.GetValue(p), "death blink completes and releases protection");

            foreach (string path in new[] { "Assets/Scenes/Stage/Stage1.unity", "Assets/Scenes/Stage/撮影用.unity" })
            {
                EditorSceneManager.OpenScene(path);
                var backgrounds = UnityEngine.Object.FindObjectsByType<ParallaxBackground>(FindObjectsSortMode.None);
                Check(backgrounds.Length == 3, path + " three backgrounds");
                foreach (var bg in backgrounds)
                {
                    var camera = Get<Transform>(bg, "cameraTransform");
                    Check(camera != null && camera.GetComponent<Camera>() != null, "rendering camera reference");
                    Check(Get<float>(bg, "parallaxFactor") == 0.5f, "parallax factor preserved");
                    Call(bg, "Awake");
                    var before = bg.transform.position;
                    typeof(ParallaxBackground).GetMethod("OnCameraUpdated", Flags).Invoke(bg, new object[] { camera.GetComponent<CinemachineBrain>() });
                    Check((bg.transform.position - before).sqrMagnitude < 0.000001f, "no initial background displacement");
                    bg.ShiftLoop(Vector3.right * 10f);
                    typeof(ParallaxBackground).GetMethod("OnCameraUpdated", Flags).Invoke(bg, new object[] { camera.GetComponent<CinemachineBrain>() });
                    Check((bg.transform.position - before - Vector3.right * 10f).sqrMagnitude < 0.000001f, "loop offset survives parallax update");
                }
            }
        }
        catch (Exception e)
        {
            Results.Add("FAIL: " + e);
            exitCode = 1;
        }
        finally
        {
            Directory.CreateDirectory(".repair-checkpoints");
            File.WriteAllLines(".repair-checkpoints/unity-validation.txt", Results);
            EditorApplication.Exit(exitCode);
        }
    }
}

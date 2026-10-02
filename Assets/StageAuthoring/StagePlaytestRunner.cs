#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

// Editor-only integration test: operates the existing actions through virtual devices.
public class StagePlaytestRunner : MonoBehaviour
{
    Keyboard keyboard;
    Mouse mouse;
    Player player;
    readonly List<string> results = new List<string>();
    bool failed;
    bool hadPreference;
    int originalPreference;
    InputSettings.BackgroundBehavior originalBackground;
    InputSettings.EditorInputBehaviorInPlayMode originalEditorInput;
    InputSettings.UpdateMode originalUpdateMode;
    ParallaxBackground[] backgroundPanels;
    Transform backgroundCamera;
    float backgroundSpan;
    Vector3 previousCamera;
    Vector3[] previousPanels;
    int backgroundFrames;
    SpriteRenderer[] backgroundSkies;
    void CheckBackgroundFrame(CinemachineBrain brain)
    {
        if (backgroundCamera == null || brain.OutputCamera == null || brain.OutputCamera.transform != backgroundCamera) return;
        Vector3 cameraPosition = backgroundCamera.position;
        var output = brain.OutputCamera;
        float left = cameraPosition.x - output.orthographicSize * output.aspect;
        float right = cameraPosition.x + output.orthographicSize * output.aspect;
        float coveredTo = left;
        Array.Sort(backgroundSkies, (a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
        foreach (var sky in backgroundSkies)
        {
            Bounds bounds = sky.bounds;
            if (bounds.max.x <= coveredTo) continue;
            if (bounds.min.x > coveredTo + 0.001f) break;
            if (bounds.min.y > cameraPosition.y - output.orthographicSize || bounds.max.y < cameraPosition.y + output.orthographicSize)
                Check(false, "Sky does not cover camera vertically");
            coveredTo = bounds.max.x;
            if (coveredTo >= right) break;
        }
        if (coveredTo < right - 0.001f) Check(false, "Clear-color gap inside camera viewport");
        for (int i = 0; i < backgroundPanels.Length; i++)
        {
            var panel = backgroundPanels[i];
            if (panel == null) return;
            Vector3 position = panel.transform.position;
            if (Mathf.Abs(cameraPosition.x - position.x) > backgroundSpan * 0.5f + 0.005f)
                Check(false, "Background escaped stable loop range");
            if (previousPanels != null)
            {
                Vector3 residual = position - previousPanels[i] - (cameraPosition - previousCamera) * 0.5f;
                float cycles = Mathf.Round(residual.x / backgroundSpan);
                if (Mathf.Abs(residual.x - cycles * backgroundSpan) > 0.01f || Mathf.Abs(residual.y) > 0.01f)
                    Check(false, "Background parallax disagrees with final rendering camera");
                if ((cameraPosition - previousCamera).sqrMagnitude < 0.0000001f && cycles != 0)
                    Check(false, "Stationary camera caused background wrapping");
            }
        }
        previousPanels = Array.ConvertAll(backgroundPanels, p => p.transform.position);
        previousCamera = cameraPosition;
        backgroundFrames++;
    }
    void Update()
    {
        if (Time.realtimeSinceStartup > 120f && !failed)
        {
            failed = true;
            results.Add("FAIL: playtest timed out");
            Finish();
        }
    }
    T Field<T>(string name) => (T)typeof(Player).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
    void Input(float direction = 0, bool jump = false, bool clone = false)
    {
        var keys = new List<Key>();
        if (direction > 0) keys.Add(Key.D);
        if (direction < 0) keys.Add(Key.A);
        if (jump) keys.Add(Key.Space);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, clone));
    }
    void Check(bool ok, string label)
    {
        results.Add((ok ? "PASS: " : "FAIL: ") + label);
        if (!ok) { failed = true; Finish(); }
    }
    void Finish()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(CheckBackgroundFrame);
        InputSystem.settings.backgroundBehavior = originalBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
        InputSystem.settings.updateMode = originalUpdateMode;
        if (hadPreference) PlayerPrefs.SetInt("UnlockedStage", originalPreference);
        else PlayerPrefs.DeleteKey("UnlockedStage");
        File.WriteAllLines("StageAuthoringReports/playtest.txt", results);
        EditorApplication.Exit(failed ? 1 : 0);
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        hadPreference = PlayerPrefs.HasKey("UnlockedStage");
        originalPreference = PlayerPrefs.GetInt("UnlockedStage", 1);
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        Application.runInBackground = true;
        originalBackground = InputSystem.settings.backgroundBehavior;
        originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        originalUpdateMode = InputSystem.settings.updateMode;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        player = FindFirstObjectByType<Player>();
        var loop = FindFirstObjectByType<BackgroundLooper>();
        if (loop != null)
        {
            backgroundPanels = loop.GetComponentsInChildren<ParallaxBackground>();
            var skies = new List<SpriteRenderer>();
            foreach (var panel in backgroundPanels)
                foreach (var image in panel.GetComponentsInChildren<SpriteRenderer>()) if (image.name == "Far") skies.Add(image);
            backgroundSkies = skies.ToArray();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            backgroundCamera = (Transform)typeof(BackgroundLooper).GetField("cameraTransform", flags).GetValue(loop);
            backgroundSpan = (float)typeof(BackgroundLooper).GetField("backgroundWidth", flags).GetValue(loop) * backgroundPanels.Length;
            CinemachineCore.CameraUpdatedEvent.AddListener(CheckBackgroundFrame);
        }
        var input = player.GetComponent<PlayerInput>();
        input.actions.devices = new InputDevice[] { keyboard, mouse };
        input.actions.Enable();
        yield return new WaitForSeconds(0.6f);
        Check(Field<bool>("isGrounded"), "Start lands on generated ground");
        yield return Drive(7f);
        yield return JumpTo(12f);
        yield return Drive(18.5f);
        yield return Clone();
        Check(player.transform.position.y > 1.2f, "Clone creates a usable elevated foothold");
        yield return JumpTo(22f);
        Check(player.transform.position.y > 4.2f, "First four-unit wall reached using clone and held jump");
        yield return Drive(28.5f);
        yield return new WaitForSeconds(0.6f);
        Check(Field<int>("maxClones") >= 2, "First coin increases clone capacity");
        yield return Drive(30f);
        yield return JumpTo(32.5f);
        yield return JumpTo(36f);
        Check(Field<int>("maxClones") >= 3, "Optional high coin can be collected");
        yield return Drive(42f);
        yield return new WaitForSeconds(0.5f);
        var body = player.GetComponent<Rigidbody2D>();
        var safe = Field<Vector3>("lastGroundPosition");
        int beforeDeath = Field<int>("currentClones");
        body.position = new Vector2(44.5f, -0.8f);
        player.transform.position = body.position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(1.8f);
        Check(Field<int>("currentClones") == beforeDeath + 1 && Mathf.Abs(player.transform.position.x - safe.x) < 0.3f && Field<bool>("isGrounded"), "Actual spike contact causes one death and returns to safe ground");
        yield return JumpTo(47f);
        Check(player.transform.position.x > 46f && player.transform.position.y > 0.2f, "Three-unit pit crossed using ordinary held jump");
        yield return Drive(58.5f);
        yield return Clone();
        yield return JumpTo(62f);
        Check(player.transform.position.y > 4.2f, "Final wall reached using existing clone ability");
        yield return Drive(69f);
        yield return Drive(70.5f);
        Input();
        yield return new WaitForSeconds(1.8f);
        Check(SceneManager.GetActiveScene().name == "Stage2", "Goal transitions from Stage1 to existing Stage2");
        Check(backgroundFrames > 200, "Background camera/parallax/loop and full viewport sky coverage stable during " + backgroundFrames + " Play Mode camera updates, including clone return and spike respawn");
        Finish();
    }
    IEnumerator Drive(float x)
    {
        float end = Time.time + 6f;
        while (player != null && player.enabled && Mathf.Abs(player.transform.position.x - x) > 0.15f && Time.time < end)
        {
            Input(Mathf.Sign(x - player.transform.position.x));
            yield return null;
        }
        Input();
        if (player != null && Mathf.Abs(player.transform.position.x - x) >= 0.3f && player.enabled)
            results.Add("INPUT DIAGNOSTIC: keyboard=" + keyboard.enabled + ", D=" + keyboard.dKey.isPressed + ", action=" + player.GetComponent<PlayerInput>().actions["Move"].ReadValue<Vector2>() + ", playerInput=" + Field<Vector2>("moveInput") + ", playerEnabled=" + player.enabled);
        Check(player != null && (!player.enabled || Mathf.Abs(player.transform.position.x - x) < 0.3f), "Walk to x=" + x + ", position=" + (player == null ? "null" : player.transform.position.ToString()));
        yield return new WaitForSeconds(0.15f);
    }
    IEnumerator JumpTo(float x)
    {
        Input();
        yield return new WaitForSeconds(0.25f);
        float start = Time.time;
        while (Time.time - start < 1.8f)
        {
            float delta = x - player.transform.position.x;
            Input(Mathf.Abs(delta) > 0.12f ? Mathf.Sign(delta) : 0f, Time.time - start < 0.9f);
            yield return null;
        }
        Input();
        Check(Mathf.Abs(player.transform.position.x - x) < 0.4f && Field<bool>("isGrounded"), "Jump to x=" + x + ", landing=" + player.transform.position);
    }
    IEnumerator Clone()
    {
        Input(0, false, true);
        yield return new WaitForSeconds(0.1f);
        Input();
        yield return new WaitForSeconds(1f);
    }
}
#endif

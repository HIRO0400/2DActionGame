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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CampaignPlaytestRunner : MonoBehaviour
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> report = new List<string>();
    Keyboard keyboard; Mouse mouse; Player player;
    Collider2D latestClone;
    int stage, cameraUpdates;
    bool finished, hadPreference; int preference;
    InputSettings.BackgroundBehavior previousBackground;
    InputSettings.EditorInputBehaviorInPlayMode previousEditor;
    InputSettings.UpdateMode previousUpdate;
    Transform cameraTransform;
    ParallaxBackground[] panels;
    SpriteRenderer[] skies;
    Vector3[] lastPanels;
    Vector3 lastCamera;
    float span;
    T Field<T>(string name) => (T)typeof(Player).GetField(name,Flags).GetValue(player);
    void Check(bool ok,string text)
    {
        report.Add((ok ? "PASS: " : "FAIL: ") + "Stage" + stage + " " + text);
        File.WriteAllLines("StageAuthoringReports/campaign-playtest.txt",report);
        if (!ok) { Finish(false); throw new Exception(text); }
    }
    void Finish(bool ok)
    {
        if (finished) return;
        finished = true;
        CinemachineCore.CameraUpdatedEvent.RemoveListener(WatchBackground);
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
        InputSystem.settings.updateMode = previousUpdate;
        if (hadPreference) PlayerPrefs.SetInt("UnlockedStage",preference); else PlayerPrefs.DeleteKey("UnlockedStage");
        JsonUtility.FromJson<ProgressTestSnapshot>(SessionState.GetString("Campaign.OriginalProgress", "")).Restore();
        report.Add(ok ? "PASS: complete Stage"+SessionState.GetInt("Campaign.StartStage",1)+"-5 campaign route" : "FAIL: campaign route incomplete");
        File.WriteAllLines("StageAuthoringReports/campaign-playtest.txt",report);
        EditorApplication.Exit(ok ? 0 : 1);
    }
    void Update() { if (!finished && Time.realtimeSinceStartup > 600) { report.Add("FAIL: timeout"); Finish(false); } }
    void Keys(float x=0,bool jump=false,bool clone=false)
    {
        var keys = new List<Key>();
        if (x>0) keys.Add(Key.D); if (x<0) keys.Add(Key.A); if (jump) keys.Add(Key.Space);
        if(clone) keys.Add(Key.Enter);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
        InputSystem.QueueStateEvent(mouse,new MouseState());
    }
    void InitializePlayer()
    {
        player = FindFirstObjectByType<Player>();
        Check(player != null,"player exists");
        player.GetComponent<PlayerInput>().actions.devices = new InputDevice[] {keyboard,mouse};
        player.GetComponent<PlayerInput>().actions.Enable();
        Keys();
        var loop = FindFirstObjectByType<BackgroundLooper>();
        cameraTransform = (Transform)typeof(BackgroundLooper).GetField("cameraTransform",Flags).GetValue(loop);
        panels = loop.GetComponentsInChildren<ParallaxBackground>();
        var images = new List<SpriteRenderer>();
        foreach(var panel in panels) foreach(var image in panel.GetComponentsInChildren<SpriteRenderer>()) if (image.name=="Far") images.Add(image);
        skies = images.ToArray();
        span = (float)typeof(BackgroundLooper).GetField("backgroundWidth",Flags).GetValue(loop)*panels.Length;
        lastPanels = null; cameraUpdates = 0;
        // After scene changes the persistent test listener precedes newly enabled
        // backgrounds. Re-register so observations see the final camera callback pose.
        CinemachineCore.CameraUpdatedEvent.RemoveListener(WatchBackground);
        CinemachineCore.CameraUpdatedEvent.AddListener(WatchBackground);
    }
    void WatchBackground(CinemachineBrain brain)
    {
        if (cameraTransform==null || brain.OutputCamera==null || brain.OutputCamera.transform!=cameraTransform) return;
        Vector3 position = cameraTransform.position;
        float edge = position.x-brain.OutputCamera.orthographicSize*brain.OutputCamera.aspect;
        float right = position.x+brain.OutputCamera.orthographicSize*brain.OutputCamera.aspect;
        Array.Sort(skies,(a,b)=>a.bounds.min.x.CompareTo(b.bounds.min.x));
        foreach(var sky in skies)
        {
            var bounds=sky.bounds;
            if (bounds.max.x<=edge) continue;
            if (bounds.min.x>edge+0.002f) break;
            if (bounds.min.y>position.y-brain.OutputCamera.orthographicSize || bounds.max.y<position.y+brain.OutputCamera.orthographicSize) Check(false,"vertical sky gap");
            edge=bounds.max.x;
            if(edge>=right) break;
        }
        if(edge<right-0.002f) Check(false,"horizontal sky gap");
        for(int i=0;i<panels.Length;i++)
        {
            if(panels[i]==null) return;
            if(Mathf.Abs(position.x-panels[i].transform.position.x)>span*0.5f+0.005f) Check(false,"loop range");
            if(lastPanels!=null)
            {
                var residual=panels[i].transform.position-lastPanels[i]-(position-lastCamera)*0.5f;
                if(Mathf.Abs(residual.x-Mathf.Round(residual.x/span)*span)>0.02f || Mathf.Abs(residual.y)>0.02f) Check(false,"parallax drift");
            }
        }
        lastPanels=Array.ConvertAll(panels,p=>p.transform.position); lastCamera=position; cameraUpdates++;
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
        hadPreference=PlayerPrefs.HasKey("UnlockedStage"); preference=PlayerPrefs.GetInt("UnlockedStage",1);
        previousBackground=InputSystem.settings.backgroundBehavior;
        previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        previousUpdate=InputSystem.settings.updateMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        Application.runInBackground=true;
        keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
        CinemachineCore.CameraUpdatedEvent.AddListener(WatchBackground);
        for(stage=SessionState.GetInt("Campaign.StartStage",1);stage<=5;stage++)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            Check(SceneManager.GetActiveScene().name=="Stage"+stage,"scene starts in sequence");
            InitializePlayer();
            yield return new WaitForSecondsRealtime(0.6f);
            Check(Field<bool>("isGrounded"),"spawn grounded");
            yield return OneWayTest();
            yield return DynamicPlatformTests();
            if(stage==1) yield return Stage1();
            if(stage==2) yield return Stage2();
            if(stage==3) yield return Stage3();
            if(stage==4) yield return Stage4();
            if(stage==5) yield return Stage5();
            Check(cameraUpdates>200,"background and viewport stable for "+cameraUpdates+" camera updates");
            Check(GameMenus.Instance.IsClearVisible,"goal displays clear screen");
            GameMenus.Instance.ContinueAfterClear();
            yield return new WaitForSecondsRealtime(1.6f);
            Check(SceneManager.GetActiveScene().name==(stage==5?"StageSelectScene":"Stage"+(stage+1)),"goal transition");
        }
        Finish(true);
    }
    IEnumerator Drive(float target)
    {
        float end=Time.time+12;
        while(player.enabled && Mathf.Abs(player.transform.position.x-target)>0.15f && Time.time<end)
        { Keys(Mathf.Sign(target-player.transform.position.x)); yield return null; }
        Keys();
        Check(!player.enabled || Mathf.Abs(player.transform.position.x-target)<0.3f,"walk x="+target+" at "+player.transform.position);
        yield return new WaitForSecondsRealtime(0.2f);
    }
    void PlacePlayer(Vector2 point)
    {
        Keys(); var body=player.GetComponent<Rigidbody2D>();
        body.position=point; player.transform.position=point; body.linearVelocity=Vector2.zero; Physics2D.SyncTransforms();
    }
    IEnumerator Capture(string name)
    {
        yield return null;
        var camera=Camera.main; var target=new RenderTexture(1280,720,24); target.Create();
        var previous=RenderTexture.active;
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
        RenderTexture.active=target;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
        Directory.CreateDirectory("StageAuthoringReports/Improvements");
        File.WriteAllBytes("StageAuthoringReports/Improvements/"+name+".png",texture.EncodeToPNG());
        RenderTexture.active=previous; target.Release(); Destroy(target); Destroy(texture);
    }
    IEnumerator OneWayTest()
    {
        StagePlatform platform=null;
        foreach(var candidate in FindObjectsByType<StagePlatform>(FindObjectsSortMode.None))
            if(candidate.GetComponent<MovingStagePlatform>()==null && candidate.GetComponent<CollapsingStagePlatform>()==null) { platform=candidate; break; }
        if(platform==null) yield break;
        // Nearby moving platforms can legitimately catch the same jump. Isolate
        // this contact test; normal routes below run with every collider enabled.
        var suspended=new List<Collider2D>();
        foreach(var other in FindObjectsByType<StagePlatform>(FindObjectsSortMode.None))
            if(other!=platform && other.GetComponent<Collider2D>().enabled)
            { var collider=other.GetComponent<Collider2D>(); suspended.Add(collider); collider.enabled=false; }
        Vector2 original=player.transform.position; Vector3 safe=Field<Vector3>("lastGroundPosition");
        var box=platform.GetComponent<Collider2D>();
        PlacePlayer(new Vector2(box.bounds.center.x,.5f)); yield return new WaitForSecondsRealtime(.3f);
        bool passed=false, falseGround=false; float start=Time.time;
        while(Time.time-start<1.8f)
        {
            Keys(0,Time.time-start<.9f); yield return null;
            float foot=player.GetComponent<Collider2D>().bounds.min.y;
            if(foot>box.bounds.max.y+.1f) passed=true;
            if(foot>box.bounds.min.y-.2f && foot<box.bounds.max.y-.12f && Field<bool>("isGrounded")) falseGround=true;
        }
        Keys();
        Check(passed && !falseGround,"one-way passes upward without false grounding");
        Check(Field<bool>("isGrounded") && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-box.bounds.max.y)<.2f,"one-way supports landing from above");
        foreach(var collider in suspended) collider.enabled=true;
        PlacePlayer(original); typeof(Player).GetField("lastGroundPosition",Flags).SetValue(player,safe);
        yield return new WaitForSecondsRealtime(.4f);
    }
    IEnumerator DynamicPlatformTests()
    {
        Vector2 original=player.transform.position; Vector3 originalSafe=Field<Vector3>("lastGroundPosition");
        MovingStagePlatform moving=null;
        foreach(var candidate in FindObjectsByType<MovingStagePlatform>(FindObjectsSortMode.None)) if(candidate.name=="Moving_SafePractice") moving=candidate;
        if(moving!=null)
        {
            var box=moving.GetComponent<Collider2D>();
            PlacePlayer(new Vector2(moving.transform.position.x,.5f)); yield return new WaitForSecondsRealtime(.3f);
            Vector3 safe=Field<Vector3>("lastGroundPosition");
            float start=Time.time;
            while(Time.time-start<1.8f)
            {
                float delta=moving.transform.position.x-player.transform.position.x;
                Keys(Mathf.Abs(delta)>.1f?Mathf.Sign(delta):0,Time.time-start<.9f); yield return null;
            }
            Keys(); yield return new WaitForSecondsRealtime(.1f);
            Check(Field<bool>("isGrounded") && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-box.bounds.max.y)<.2f,"lands on safe moving platform");
            float offset=player.transform.position.x-moving.transform.position.x;
            yield return new WaitForSecondsRealtime(.6f);
            Check(Mathf.Abs(player.transform.position.x-moving.transform.position.x-offset)<.2f,"moving platform carries idle player without sliding");
            Check(Vector3.Distance(Field<Vector3>("lastGroundPosition"),safe)<.2f,"moving platform preserves last stable respawn ground");
            yield return Capture("stage2-moving-practice");
            Vector3 stopped=moving.transform.position; GameMenus.Instance.Pause(); yield return new WaitForSecondsRealtime(.4f);
            Check(Vector3.Distance(stopped,moving.transform.position)<.001f,"pause freezes moving platform"); GameMenus.Instance.Resume();
            Keys(0,true); yield return new WaitForSecondsRealtime(.25f);
            Check(player.GetComponent<Rigidbody2D>().linearVelocity.y>1 && player.GetComponent<Collider2D>().bounds.min.y>box.bounds.max.y+.5f,"jump input leaves moving platform");
            Keys();
            PlacePlayer(original); typeof(Player).GetField("lastGroundPosition",Flags).SetValue(player,originalSafe); yield return new WaitForSecondsRealtime(.3f);
        }
        var collapsing=FindFirstObjectByType<CollapsingStagePlatform>();
        if(collapsing!=null)
        {
            var box=collapsing.GetComponent<BoxCollider2D>();
            // Establish a stable checkpoint below the optional route first.
            PlacePlayer(new Vector2(collapsing.transform.position.x,.5f)); yield return new WaitForSecondsRealtime(.3f);
            Vector3 safe=Field<Vector3>("lastGroundPosition");
            PlacePlayer(new Vector2(collapsing.transform.position.x,box.bounds.max.y+.48f)); yield return new WaitForSecondsRealtime(.15f);
            Check(collapsing.IsCountingDown,"standing starts visible collapse countdown");
            yield return Capture("stage"+stage+"-collapse-warning");
            Check(Vector3.Distance(Field<Vector3>("lastGroundPosition"),safe)<.2f,"collapsing platform does not become a respawn checkpoint");
            GameMenus.Instance.Pause(); yield return new WaitForSecondsRealtime(1.1f);
            Check(!collapsing.IsCollapsed,"pause freezes collapse countdown"); GameMenus.Instance.Resume();
            yield return new WaitForSecondsRealtime(1.2f);
            Check(collapsing.IsCollapsed && !box.enabled,"platform collapses after warning");
            player.Die(); yield return new WaitForSecondsRealtime(1.5f);
            Check(Field<bool>("isGrounded") && !Field<bool>("isGroundLocked") && Mathf.Abs(player.transform.position.x-safe.x)<.3f,
                "death after collapse recovers to stable ground; position="+player.transform.position+", safe="+safe+", ground="+Field<bool>("isGrounded")+", lock="+Field<bool>("isGroundLocked")+", velocity="+player.GetComponent<Rigidbody2D>().linearVelocity);
            PlacePlayer(new Vector2(collapsing.transform.position.x+3,.5f)); yield return new WaitForSecondsRealtime(3.2f);
            Check(!collapsing.IsCollapsed && box.enabled,"collapsed platform restores for another attempt");
            PlacePlayer(original); typeof(Player).GetField("lastGroundPosition",Flags).SetValue(player,originalSafe); yield return new WaitForSecondsRealtime(.3f);
        }
    }
    IEnumerator Jump(float target,float verticalDelay=0,bool required=true)
    {
        Keys(); yield return new WaitForSecondsRealtime(0.25f);
        float start=Time.time;
        while(Time.time-start<1.8f)
        {
            float delta=target-player.transform.position.x;
            Keys(Time.time-start>=verticalDelay && Mathf.Abs(delta)>0.12f?Mathf.Sign(delta):0,Time.time-start<0.92f);
            yield return null;
        }
        Keys();
        float end=Time.time+1f;
        while(!Field<bool>("isGrounded") && Time.time<end) yield return null;
        if(required) Check(Mathf.Abs(player.transform.position.x-target)<0.4f && Field<bool>("isGrounded"),"jump x="+target+" lands at "+player.transform.position);
    }
    IEnumerator Clone(float airTime=0)
    {
        int before=Field<int>("currentClones");
        Keys(); yield return new WaitForSecondsRealtime(0.08f);
        if(airTime>0)
        {
            float start=Time.time;
            while(Time.time-start<airTime) { Keys(1,true); yield return null; }
        }
        Keys(0,false,true); yield return new WaitForSecondsRealtime(.08f);
        Keys(); yield return new WaitForSecondsRealtime(1.2f);
        Check(Field<int>("currentClones")==before+1,"clone generated from "+(airTime==0?"ground":"air"));
        var clones=Field<GameObject[]>("clones");
        latestClone=clones[(Field<int>("currentClones")-1)%clones.Length].GetComponent<Collider2D>();
        Check(Field<bool>("isGrounded") && !Field<bool>("isGroundLocked"),"clone return releases landing lock; clone top="+latestClone.bounds.max.y);
    }
    IEnumerator OneCloneWall(int wall,int height,bool proveNoClone=false)
    {
        yield return Drive(wall-3.5f);
        if(proveNoClone)
        {
            yield return Jump(wall+2,0,false);
            Check(player.transform.position.x<wall-0.3f,"ordinary jump cannot clear "+height+"-unit wall");
            yield return Drive(wall-3.5f);
        }
        yield return Clone(0.34f);
        float cloneX=latestClone.transform.position.x;
        yield return Jump(cloneX,0.58f);
        Check(Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-latestClone.bounds.max.y)<0.2f,"uses elevated clone foothold");
        yield return Jump(wall+2);
        Check(player.transform.position.y>height+0.2f,"clears wall using one clone");
        yield return Drive(wall+13);
        yield return new WaitForSecondsRealtime(0.7f);
    }
    IEnumerator SpikeRecovery(float pitX)
    {
        Keys(); yield return new WaitForSecondsRealtime(0.3f);
        Vector3 safe=Field<Vector3>("lastGroundPosition");
        int before=Field<int>("currentClones");
        var body=player.GetComponent<Rigidbody2D>(); body.position=new Vector2(pitX,-0.8f); player.transform.position=body.position; body.linearVelocity=Vector2.zero;
        Physics2D.SyncTransforms(); yield return new WaitForSecondsRealtime(1.8f);
        Check(Field<int>("currentClones")==before+1 && Mathf.Abs(player.transform.position.x-safe.x)<0.3f && Field<bool>("isGrounded"),"spike contact recovers once to last ground");
    }
    IEnumerator Stage1()
    {
        yield return Drive(6); yield return Jump(9.5f); yield return Drive(12);
        yield return Jump(18); yield return Drive(28);
        yield return Clone(); yield return Jump(latestClone.transform.position.x,.2f);
        yield return Jump(32); Check(player.transform.position.y>4.2f,"tutorial clone foothold clears wall");
        yield return Drive(43); yield return new WaitForSecondsRealtime(.7f); yield return Drive(44.5f);
    }
    IEnumerator Stage2()
    {
        yield return Drive(10); yield return Jump(15); yield return Drive(22);
        yield return SpikeRecovery(25.5f); yield return Jump(28);
        yield return Drive(34); yield return Jump(37.5f); yield return Drive(53);
        yield return Drive(82);
        if(FindFirstObjectByType<MovingStagePlatform>()!=null) { yield return SpikeRecovery(87.5f); yield return RidePit(); }
        else yield return Jump(88);
        yield return Drive(94); yield return Jump(99);
        yield return Jump(104.5f); yield return Drive(112.5f);
    }
    IEnumerator RidePit()
    {
        MovingStagePlatform moving=null;
        foreach(var candidate in FindObjectsByType<MovingStagePlatform>(FindObjectsSortMode.None)) if(candidate.name=="Moving_Pit") moving=candidate;
        Check(moving!=null,"pit has moving platform");
        var box=moving.GetComponent<Collider2D>();
        float deadline=Time.time+8;
        while(moving.transform.position.x>88 || moving.Velocity.x>=0) { Keys(); yield return null; if(Time.time>deadline) { Check(false,"platform returns to takeoff bank"); yield break; } }
        float start=Time.time;
        while(Time.time-start<1.8f)
        {
            float delta=moving.transform.position.x-player.transform.position.x;
            Keys(Mathf.Abs(delta)>.13f?Mathf.Sign(delta):0,Time.time-start<.8f); yield return null;
        }
        Keys();
        Check(Field<bool>("isGrounded") && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-box.bounds.max.y)<.2f,"boards moving platform over pit");
        deadline=Time.time+8;
        while(moving.transform.position.x<89.5f) { Keys(); yield return null; if(Time.time>deadline) { Check(false,"moving platform reaches exit bank"); yield break; } }
        yield return Jump(93.5f);
        Check(Field<bool>("isGrounded"),"jumps off moving platform onto stable exit bank");
    }
    IEnumerator Stage3()
    {
        yield return Drive(18); yield return SpikeRecovery(21.5f); yield return Jump(24);
        yield return OneCloneWall(30,5,true); yield return Drive(61); yield return Jump(66);
        yield return Drive(73); yield return Jump(79); yield return OneCloneWall(86,5);
        yield return Drive(107); yield return Jump(110.5f); yield return Drive(119);
        yield return new WaitForSecondsRealtime(.5f); yield return Drive(122.5f);
    }
    IEnumerator Stage4()
    {
        yield return Drive(18); yield return SpikeRecovery(21.5f); yield return Jump(24);
        yield return OneCloneWall(28,6,true); yield return Drive(60); yield return Jump(65);
        if(FindFirstObjectByType<PressurePlate>()!=null) yield return HoldDoor();
        yield return Drive(106); yield return Jump(112); yield return Drive(121);
        yield return Drive(134.5f);
    }
    IEnumerator HoldDoor()
    {
        var plate=FindFirstObjectByType<PressurePlate>(); var door=plate.door;
        yield return Drive(84); yield return new WaitForSecondsRealtime(.2f);
        Check(plate.IsPressed && door.IsOpen,"player opens linked door by standing on plate");
        yield return Drive(87); yield return new WaitForSecondsRealtime(.2f);
        Check(!plate.IsPressed && !door.IsOpen,"door closes when plate is released");
        yield return Drive(84); yield return Clone();
        yield return Drive(101);
        Check(plate.IsPressed && door.IsOpen,"stationary clone holds switch while player crosses door");
        yield return Capture("stage4-switch-door");
        Vector2 original=player.transform.position;
        PlacePlayer(new Vector2(door.transform.position.x,.5f));
        UnityEngine.Object.Destroy(latestClone.gameObject); yield return new WaitForSecondsRealtime(.3f);
        Check(!plate.IsPressed && door.IsOpen,"door waits for player to exit rather than crushing player");
        PlacePlayer(original); yield return new WaitForSecondsRealtime(.3f);
        Check(!door.IsOpen,"door closes after doorway is clear");
    }
    IEnumerator TwoRoles()
    {
        var plate=FindFirstObjectByType<PressurePlate>();
        yield return Drive(92); yield return Clone(); var weight=latestClone;
        yield return Drive(106.5f); yield return Clone(.34f);
        Check(weight!=null && plate.IsPressed && plate.door.IsOpen && latestClone!=weight,"two simultaneous clones: switch weight and elevated foothold");
        yield return Capture("stage5-two-roles");
        yield return Jump(latestClone.transform.position.x,.58f); yield return Jump(112);
        Check(player.transform.position.y>5.2f && plate.IsPressed,"switch clone remains while second clone clears gated wall");
        yield return Drive(123); yield return new WaitForSecondsRealtime(.6f);
    }
    IEnumerator OneCloneDoorTest()
    {
        // Controlled negative test: temporarily restrict the inventory to one slot.
        int capacity=Field<int>("maxClones");
        foreach(var clone in Field<GameObject[]>("clones")) if(clone!=null) Destroy(clone);
        typeof(Player).GetField("maxClones",Flags).SetValue(player,1);
        typeof(Player).GetField("clones",Flags).SetValue(player,new GameObject[1]);
        typeof(Player).GetField("currentClones",Flags).SetValue(player,0);
        yield return Drive(92); yield return Clone();
        yield return Drive(106.5f); yield return Clone(.34f);
        var plate=FindFirstObjectByType<PressurePlate>();
        Check(!plate.IsPressed && !plate.door.IsOpen,"one-slot air clone replaces switch weight and closes door");
        yield return Jump(latestClone.transform.position.x,.58f);
        yield return Jump(113,0,false);
        Check(player.transform.position.x<111.3f && !plate.door.IsOpen,"one clone cannot bypass recessed door or hold it open at wall edge");
        foreach(var clone in Field<GameObject[]>("clones")) if(clone!=null) Destroy(clone);
        typeof(Player).GetField("maxClones",Flags).SetValue(player,capacity);
        typeof(Player).GetField("clones",Flags).SetValue(player,new GameObject[capacity]);
        typeof(Player).GetField("currentClones",Flags).SetValue(player,0);
        PlacePlayer(new Vector2(90,.5f)); yield return new WaitForSecondsRealtime(.5f);
    }
    IEnumerator TwoCloneWall(int wall)
    {
        yield return Drive(wall-4);
        yield return Clone();
        var low=latestClone;
        yield return Clone(0.45f);
        var high=latestClone;
        Check(low!=null && high!=null && high.bounds.max.y-low.bounds.max.y>3,"two simultaneous low/high footholds");
        Check(player.transform.position.y<2,"returns onto low foothold before using high foothold");
        yield return Jump(high.transform.position.x,0.72f);
        Check(Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-high.bounds.max.y)<0.2f,"lands on second clone");
        yield return Jump(wall+2,0.2f);
        Check(player.transform.position.y>8.2f,"two clones clear eight-unit wall");
        yield return Drive(wall+13); yield return new WaitForSecondsRealtime(0.7f);
    }
    IEnumerator Stage5()
    {
        yield return Drive(6); Check(Field<int>("maxClones")>=2,"safe coin enables two slots");
        yield return Drive(37); yield return Clone(0.34f);
        yield return Jump(latestClone.transform.position.x,0.58f);
        yield return Jump(43,0,false);
        Check(player.transform.position.x<40.7f,"one reachable air clone cannot clear eight-unit wall");
        Keys(); FindFirstObjectByType<StageManager>().RestartStage();
        yield return new WaitForSecondsRealtime(0.7f); InitializePlayer(); yield return new WaitForSecondsRealtime(0.5f);
        yield return Drive(6); Check(Field<int>("maxClones")==2,"restart restores exactly two slots after coin");
        yield return TwoCloneWall(41);
        yield return Drive(64); yield return SpikeRecovery(67.5f); yield return Jump(70);
        yield return Drive(77); yield return Jump(83);
        if(FindFirstObjectByType<PressurePlate>()!=null) { yield return OneCloneDoorTest(); yield return TwoRoles(); }
        else yield return TwoCloneWall(94);
        yield return Drive(124); yield return Jump(130); yield return Drive(142.5f);
    }
}
#endif

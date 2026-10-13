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

public class CampaignPlaytestRunner : MonoBehaviour
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> report = new List<string>();
    Keyboard keyboard; Mouse mouse; Player player;
    Collider2D latestClone;
    int stage, cameraUpdates;
    bool finished, hadPreference; int preference;
    ProgressTestSnapshot progressSnapshot;
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
        progressSnapshot?.Restore();
        report.Add(ok ? "PASS: complete Stage2-5 campaign route" : "FAIL: campaign route incomplete");
        File.WriteAllLines("StageAuthoringReports/campaign-playtest.txt",report);
        EditorApplication.Exit(ok ? 0 : 1);
    }
    void Update() { if (!finished && Time.realtimeSinceStartup > 600) { report.Add("FAIL: timeout"); Finish(false); } }
    void Keys(float x=0,bool jump=false,bool clone=false)
    {
        var keys = new List<Key>();
        if (x>0) keys.Add(Key.D); if (x<0) keys.Add(Key.A); if (jump) keys.Add(Key.Space);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,clone));
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
        hadPreference=PlayerPrefs.HasKey("UnlockedStage"); preference=PlayerPrefs.GetInt("UnlockedStage",1);
        progressSnapshot = new ProgressTestSnapshot();
        previousBackground=InputSystem.settings.backgroundBehavior;
        previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        previousUpdate=InputSystem.settings.updateMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        Application.runInBackground=true;
        keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
        CinemachineCore.CameraUpdatedEvent.AddListener(WatchBackground);
        for(stage=2;stage<=5;stage++)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            Check(SceneManager.GetActiveScene().name=="Stage"+stage,"scene starts in sequence");
            InitializePlayer();
            yield return new WaitForSecondsRealtime(0.6f);
            Check(Field<bool>("isGrounded"),"spawn grounded");
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
        Keys(0,false,true); yield return new WaitForSecondsRealtime(0.08f);
        if(airTime>0)
        {
            float start=Time.time;
            while(Time.time-start<airTime) { Keys(1,true,true); yield return null; }
        }
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
    IEnumerator Stage2()
    {
        yield return Drive(10); yield return Jump(15); yield return Drive(17);
        yield return Drive(22); yield return SpikeRecovery(25.5f); yield return Jump(28);
        yield return Drive(34); yield return Jump(40.5f);
        yield return Drive(43); yield return Jump(49);
        yield return Drive(58); yield return Jump(64);
        yield return Drive(76); yield return Jump(83.5f);
        yield return Drive(86); yield return Jump(92);
        yield return Drive(95); yield return Jump(101);
        yield return Drive(108); yield return Jump(114);
        yield return Drive(138); yield return Jump(143.5f);
        yield return Drive(158.5f);
    }
    IEnumerator Stage3()
    {
        yield return Drive(18); yield return SpikeRecovery(21.5f); yield return Jump(24);
        yield return OneCloneWall(30,5,true);
        yield return Drive(51); yield return Drive(61); yield return Jump(66);
        yield return Drive(73); yield return Jump(79);
        yield return OneCloneWall(86,5);
        yield return Drive(109); yield return Drive(111); yield return Jump(117);
        yield return Drive(119); yield return Jump(125);
        yield return OneCloneWall(138,5);
        yield return Drive(154); yield return Jump(160);
        yield return Drive(167.5f);
    }
    IEnumerator Stage4()
    {
        yield return Drive(18); yield return SpikeRecovery(21.5f); yield return Jump(24);
        yield return OneCloneWall(28,6,true);
        yield return Drive(47); yield return Drive(52); yield return Jump(58);
        yield return Drive(65); yield return Jump(71);
        yield return OneCloneWall(80,6);
        yield return Drive(97); yield return Jump(103);
        yield return Drive(110); yield return Jump(116);
        yield return Drive(130); yield return Jump(136);
        yield return OneCloneWall(146,6);
        yield return Drive(160); yield return Jump(165.5f);
        yield return Drive(174.5f);
    }
    IEnumerator TwoCloneWall(int wall)
    {
        yield return Drive(wall-4);
        yield return Clone();
        var low=latestClone;
        yield return Clone(0.46f);
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
        yield return TwoCloneWall(94);
        yield return Drive(113); yield return Jump(119);
        yield return Drive(124); yield return Jump(130);
        yield return TwoCloneWall(137);
        yield return Drive(155); yield return Jump(161);
        yield return Drive(169.5f);
    }
}
#endif

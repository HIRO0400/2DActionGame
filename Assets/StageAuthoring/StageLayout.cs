using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Stage Authoring/Stage Layout", fileName = "StageLayout")]
public class StageLayout : ScriptableObject
{
    public string sceneName = "Stage1";
    public string title = "足場を残して、もう一度";
    [TextArea] public string concept;
    [Range(1, 5)] public int difficulty = 2;
    public Vector2 playerStart = new Vector2(2f, 0.5f);
    public Vector2 goalPosition = new Vector2(88f, 0.5f);
    public GameObject playerPrefab;
    public GameObject goalPrefab;
    public GameObject stageManagerPrefab;
    public GameObject audioManagerPrefab;
    public GameObject bgmPrefab;
    public GameObject backgroundPrefab;
    public TileBase groundTile;
    public TileBase spikeTile;
    public PhysicsMaterial2D groundMaterial;
    public GroundRegion[] terrain = Array.Empty<GroundRegion>();
    public GroundRegion[] hazards = Array.Empty<GroundRegion>();
    public PrefabPlacement[] placements = Array.Empty<PrefabPlacement>();
    public LearningArea[] areas = Array.Empty<LearningArea>();
    public PlatformPlacement[] platforms = Array.Empty<PlatformPlacement>();
    public SwitchDoorPlacement[] switchDoors = Array.Empty<SwitchDoorPlacement>();

    public enum PlatformKind { OneWay, Moving, Collapsing }
    [Serializable] public struct PlatformPlacement
    {
        public string label;
        public PlatformKind kind;
        public Vector2 position, size, travel;
        public float period, collapseDelay, restoreDelay;
        public PlatformPlacement(string label, PlatformKind kind, Vector2 position, Vector2 size, Vector2 travel = default)
        {
            this.label=label; this.kind=kind; this.position=position; this.size=size; this.travel=travel;
            period=5f; collapseDelay=.85f; restoreDelay=3f;
        }
    }
    [Serializable] public struct SwitchDoorPlacement
    {
        public string label;
        public Vector2 switchPosition, doorPosition, doorSize;
        public SwitchDoorPlacement(string label, Vector2 plate, Vector2 door, Vector2 size)
        { this.label=label; switchPosition=plate; doorPosition=door; doorSize=size; }
    }

    [Serializable] public struct GroundRegion
    {
        public string purpose;
        public RectInt cells;
        public GroundRegion(string purpose, int x, int y, int width, int height)
        { this.purpose = purpose; cells = new RectInt(x, y, width, height); }
    }
    [Serializable] public struct PrefabPlacement
    {
        public string label;
        public GameObject prefab;
        public Vector2 position;
        public PrefabPlacement(string label, GameObject prefab, float x, float y)
        { this.label = label; this.prefab = prefab; position = new Vector2(x, y); }
    }
    [Serializable] public struct LearningArea
    {
        public string title;
        [TextArea] public string instruction;
        public float fromX;
        public float toX;
        public LearningArea(string title, string instruction, float fromX, float toX)
        { this.title = title; this.instruction = instruction; this.fromX = fromX; this.toX = toX; }
    }
}

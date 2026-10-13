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

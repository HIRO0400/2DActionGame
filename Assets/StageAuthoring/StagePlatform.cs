using UnityEngine;

// Queries do not apply PlatformEffector's one-way filtering: Player uses this marker.
public class StagePlatform : MonoBehaviour
{
    public bool stableRespawnSurface = true;
    public bool oneWay = true;
    public bool CanStand(float footY) => !oneWay || footY >= GetComponent<Collider2D>().bounds.max.y - .08f;
}

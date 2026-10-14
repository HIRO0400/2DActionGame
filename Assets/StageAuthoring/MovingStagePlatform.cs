using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MovingStagePlatform : MonoBehaviour
{
    public Vector2 travel = new Vector2(5,0);
    [Min(1)] public float period=5;
    public Vector2 Velocity { get; private set; }
    Rigidbody2D body; Vector2 origin; float elapsed;
    void Awake() { body=GetComponent<Rigidbody2D>(); origin=body.position; }
    void FixedUpdate()
    {
        elapsed+=Time.fixedDeltaTime;
        Vector2 next=origin+travel*Mathf.PingPong(elapsed*2f/Mathf.Max(1,period),1);
        Velocity=(next-body.position)/Time.fixedDeltaTime;
        body.MovePosition(next);
    }
}

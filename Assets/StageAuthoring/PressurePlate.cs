using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public PressureDoor door;
    public Vector2 sensingSize = new Vector2(1.6f, .7f);
    public bool IsPressed { get; private set; }
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    SpriteRenderer image;
    LineRenderer signal;
    void Awake() { image=GetComponent<SpriteRenderer>(); signal=GetComponentInChildren<LineRenderer>(); }
    void FixedUpdate()
    {
        // Clones have static colliders; static/static trigger messages are unreliable.
        var filter=new ContactFilter2D(); filter.SetLayerMask((1<<LayerMask.NameToLayer("Player")) | (1<<LayerMask.NameToLayer("Clone"))); filter.useTriggers=false;
        Physics2D.OverlapBox((Vector2)transform.position+Vector2.up*.45f,sensingSize,0,filter,overlaps);
        IsPressed=false;
        foreach(var c in overlaps)
        {
            if(c.bounds.min.y<transform.position.y-.14f) continue;
            if(c.gameObject.layer==LayerMask.NameToLayer("Clone") || c.GetComponentInParent<Player>()!=null) { IsPressed=true; break; }
        }
        if(image!=null) image.color=IsPressed ? Color.green : new Color(1f,.75f,.15f);
        if(signal!=null) signal.startColor=signal.endColor=image.color;
        if(door!=null) door.SetRequestedOpen(IsPressed);
    }
}

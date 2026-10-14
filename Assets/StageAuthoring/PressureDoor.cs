using System.Collections.Generic;
using UnityEngine;

public class PressureDoor : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    bool requested;
    BoxCollider2D box; SpriteRenderer image;
    LineRenderer signal;
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    void Awake() { box=GetComponent<BoxCollider2D>(); image=GetComponent<SpriteRenderer>(); signal=GetComponentInChildren<LineRenderer>(); }
    public void SetRequestedOpen(bool value) => requested=value;
    void FixedUpdate()
    {
        bool occupied=false;
        if(!requested && IsOpen)
        {
            var filter=new ContactFilter2D(); filter.SetLayerMask((1<<LayerMask.NameToLayer("Player")) | (1<<LayerMask.NameToLayer("Clone"))); filter.useTriggers=false;
            Physics2D.OverlapBox((Vector2)transform.position+box.offset,box.size+Vector2.one*.08f,0,filter,overlaps);
            occupied=overlaps.Count>0;
        }
        // Never materialize a collider inside a player or clone.
        IsOpen=requested || occupied; box.enabled=!IsOpen;
        image.color=IsOpen ? new Color(.25f,1f,.6f,.18f) : new Color(.55f,.65f,1f,1f);
        if(signal!=null) signal.startColor=signal.endColor=IsOpen ? new Color(.25f,1f,.6f,.4f) : new Color(.55f,.65f,1f);
    }
}

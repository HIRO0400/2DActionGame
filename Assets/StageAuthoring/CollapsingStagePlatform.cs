using System.Collections.Generic;
using UnityEngine;

public class CollapsingStagePlatform : MonoBehaviour
{
    [Min(.3f)] public float collapseDelay=.85f;
    [Min(1)] public float restoreDelay=3;
    public bool IsCollapsed { get; private set; }
    public bool IsCountingDown => countdown>=0 && !IsCollapsed;
    float countdown=-1, restore;
    BoxCollider2D box; SpriteRenderer image;
    LineRenderer signal;
    readonly List<Collider2D> overlaps=new List<Collider2D>();
    void Awake() { box=GetComponent<BoxCollider2D>(); image=GetComponent<SpriteRenderer>(); signal=GetComponentInChildren<LineRenderer>(); }
    void Paint(Color color,bool visible)
    { image.color=color; image.enabled=visible; if(signal!=null) { signal.startColor=signal.endColor=color; signal.enabled=visible; } }
    bool PlayerOverlaps(Vector2 center,Vector2 size,bool standingOnly)
    {
        var filter=new ContactFilter2D(); filter.SetLayerMask(1<<LayerMask.NameToLayer("Player")); filter.useTriggers=false;
        Physics2D.OverlapBox(center,size,0,filter,overlaps);
        foreach(var c in overlaps)
        {
            var player=c.GetComponentInParent<Player>();
            if(player==null || !player.enabled) continue;
            if(!standingOnly || (c.bounds.min.y>=box.bounds.max.y-.1f && player.GetComponent<Rigidbody2D>().linearVelocity.y<=.15f)) return true;
        }
        return false;
    }
    void FixedUpdate()
    {
        if(IsCollapsed)
        {
            restore-=Time.fixedDeltaTime;
            if(restore<=0 && !PlayerOverlaps((Vector2)transform.position+box.offset,box.size+Vector2.one*.15f,false))
            { IsCollapsed=false; countdown=-1; box.enabled=true; Paint(new Color(1,.55f,.2f),true); }
            return;
        }
        if(countdown<0 && PlayerOverlaps(new Vector2(box.bounds.center.x,box.bounds.max.y+.07f),new Vector2(box.size.x-.1f,.18f),true)) countdown=collapseDelay;
        if(countdown<0) return;
        countdown-=Time.fixedDeltaTime;
        Paint(new Color(1,.3f,.08f,Mathf.PingPong(countdown*8,1)>.5f?1:.4f),true);
        if(countdown<=0) { IsCollapsed=true; restore=restoreDelay; box.enabled=false; Paint(image.color,false); }
    }
}

using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class StageGimmickBuilder
{
    public static void Build(StageLayout data, Transform parent)
    {
        Sprite sprite = (data.groundTile as Tile)?.sprite;
        foreach(var p in data.platforms ?? Array.Empty<StageLayout.PlatformPlacement>())
        {
            var go=Block(p.label,parent,p.position,p.size,sprite,new Color(.3f,.85f,.95f),data.groundMaterial);
            var surface=go.AddComponent<StagePlatform>();
            var effect=go.AddComponent<PlatformEffector2D>();
            effect.useOneWay=true; effect.useOneWayGrouping=true; effect.surfaceArc=180;
            effect.useSideFriction=false; effect.useSideBounce=false;
            go.GetComponent<BoxCollider2D>().usedByEffector=true;
            var signal=Signal(go,new[]{new Vector3(-p.size.x*.5f,p.size.y*.5f),new Vector3(p.size.x*.5f,p.size.y*.5f)},Color.cyan);
            if(p.kind==StageLayout.PlatformKind.Moving)
            {
                surface.stableRespawnSurface=false;
                go.GetComponent<SpriteRenderer>().color=new Color(.3f,1f,.5f);
                signal.startColor=signal.endColor=new Color(.3f,1f,.5f);
                var body=go.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic;
                body.interpolation=RigidbodyInterpolation2D.Interpolate;
                var moving=go.AddComponent<MovingStagePlatform>(); moving.travel=p.travel; moving.period=p.period;
            }
            if(p.kind==StageLayout.PlatformKind.Collapsing)
            {
                surface.stableRespawnSurface=false;
                go.GetComponent<SpriteRenderer>().color=new Color(1,.55f,.2f);
                signal.startColor=signal.endColor=new Color(1,.55f,.2f);
                var collapse=go.AddComponent<CollapsingStagePlatform>(); collapse.collapseDelay=p.collapseDelay; collapse.restoreDelay=p.restoreDelay;
            }
        }
        foreach(var p in data.switchDoors ?? Array.Empty<StageLayout.SwitchDoorPlacement>())
        {
            var door=Block(p.label+"_Door",parent,p.doorPosition,p.doorSize,sprite,new Color(.55f,.65f,1f),data.groundMaterial).AddComponent<PressureDoor>();
            float x=p.doorSize.x*.5f,y=p.doorSize.y*.5f;
            Signal(door.gameObject,new[]{new Vector3(-x,-y),new Vector3(-x,y),new Vector3(x,y),new Vector3(x,-y),new Vector3(-x,-y)},new Color(.55f,.65f,1));
            var plate=Block(p.label+"_Plate",parent,p.switchPosition,new Vector2(1.6f,.12f),sprite,new Color(1f,.75f,.15f),null);
            plate.layer=0; plate.GetComponent<BoxCollider2D>().isTrigger=true;
            Signal(plate,new[]{new Vector3(-.8f,.06f),new Vector3(.8f,.06f)},new Color(1,.75f,.15f));
            plate.AddComponent<PressurePlate>().door=door;
            // Visual cable shows which distant door a switch controls; never collides.
            var cable=new GameObject(p.label+"_Link"); cable.transform.SetParent(parent,false);
            var line=cable.AddComponent<LineRenderer>(); line.useWorldSpace=true; line.positionCount=3;
            line.SetPositions(new[]{new Vector3(p.switchPosition.x,-.18f),new Vector3(p.doorPosition.x,-.18f),new Vector3(p.doorPosition.x,p.doorPosition.y)});
            line.startWidth=line.endWidth=.035f; line.startColor=line.endColor=new Color(.9f,.75f,.25f,.6f);
            line.sharedMaterial=plate.GetComponent<SpriteRenderer>().sharedMaterial; line.sortingOrder=3;
        }
    }
    static LineRenderer Signal(GameObject parent,Vector3[] points,Color color)
    {
        var go=new GameObject("StateIndicator"); go.transform.SetParent(parent.transform,false);
        var line=go.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=points.Length; line.SetPositions(points);
        line.startWidth=line.endWidth=.065f; line.startColor=line.endColor=color;
        line.sharedMaterial=parent.GetComponent<SpriteRenderer>().sharedMaterial; line.sortingOrder=4;
        return line;
    }
    public static GameObject Block(string name,Transform parent,Vector2 position,Vector2 size,Sprite sprite,Color color,PhysicsMaterial2D material)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.position=position;
        go.layer=LayerMask.NameToLayer("Ground");
        var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=color;
        renderer.drawMode=SpriteDrawMode.Tiled; renderer.size=size; renderer.sortingOrder=2;
        var collider=go.AddComponent<BoxCollider2D>(); collider.size=size; collider.sharedMaterial=material;
        return go;
    }
}

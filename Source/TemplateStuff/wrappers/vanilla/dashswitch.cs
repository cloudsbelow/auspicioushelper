


using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Core.Platforms;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class DashSwitchW:DashSwitch, ISimpleEnt{
  public Template parent {get;set;}
  public Template.Propagation prop => Template.Propagation.All;
  Vector2 origtoffset;

  public DashSwitchW(EntityData e, Vector2 offset, EntityID id):base(
    offset+e.Position, e.Name.Equals("dashSwitchH")?(
      e.Bool("leftSide")?Sides.Left:Sides.Right
    ):(
      e.Bool("ceiling")?Sides.Up:Sides.Down
    ),
    e.Bool("persistent"),e.Bool("allGates"),id,e.Attr("sprite", "default")
  ){
    OnDashCollide = OnDashedOverride;
    lpos = Position;
  }
  DashCollisionResults OnDashedOverride(Player p, Vector2 dir){
    pressedTarget = Position+pressDirection*8;
    return OnDashed(p,dir);
  }
  public void setOffset(Vector2 ppos){
    toffset = Position-ppos;
    origtoffset = toffset;
    Depth+=parent.depthoffset;
  }
  public override void Awake(Scene scene){
    pressedTarget = Position+pressDirection*8;
    base.Awake(scene);
  }
  public override void Update(){
    startY = parent.roundLoc.Y+origtoffset.Y;
    base.Update();
  }
  Vector2 lpos {get;set;}
  public Vector2 toffset {get;set;}
  
  public virtual void relposTo(Vector2 loc, Vector2 liftspeed){
    if(Scene==null)return;
    if(lpos!=Position){
      toffset+=Position-lpos;
    }
    MoveTo(loc+toffset, liftspeed);
    lpos = Position;
  }
  public bool hasRiders<T>() where T:Actor{
    if(typeof(T) == typeof(Player)) return HasPlayerRider();
    if(typeof(T) == typeof(Actor)) return HasRider();
    return false;
  }
  public bool hasInside(Actor a){
    if(Scene==null)return false;
    return Collider.Collide(a.Collider);
  }
  public void AddAllChildren(List<Entity> l){
    l.Add(this);
  }
  public void parentChangeStat(int vis, int col,int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0)Collidable = col>0&&!pressed;
    if(act!=0)Active = act>0;
    if(col>0) EnableStaticMovers();
    else if(col<0) DisableStaticMovers();
  }
}
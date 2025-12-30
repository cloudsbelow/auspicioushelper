

using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[ResetEvents.LazyLoadDuration(ResetEvents.RunTimes.OnNewScreen)]
[Tracked]
public class PortalFaceH:Entity{
  public bool facingRight = false;
  public bool flipped = false;
  public float height;
  PortalFaceH other;
  public PortalFaceH(Vector2 pos, float height, bool facingRight, bool vflip):base(pos){
    Collider = new Hitbox(2,height,-1,0);
    flipped=vflip;
    this.facingRight = facingRight;
    this.height=height;
  }
  public override void Render(){
    base.Render();
    Draw.Rect(Collider,Color.Red);
    Draw.Rect(new Rectangle((int)X+(facingRight?0:-4),(int)Y,4,4),Color.Red);
  }

  public Vector2 getSpeed()=>Vector2.Zero;


  [ResetEvents.OnHook(typeof(Actor),nameof(Actor.MoveHExact))]
  static bool Hook(On.Celeste.Actor.orig_MoveHExact orig, Actor s, int m, Collision col, Solid pusher){ 
    int dir = Math.Sign(m);
    Vector2 eloc = s.Position;
    if(s.Collider is Hitbox h){
      bool hitRight = m<0;
      float absTop = eloc.Y+h.Position.Y;
      float frontEdge = eloc.X+h.Position.X+(hitRight?0:h.width);
      float minDist=float.PositiveInfinity;
      PortalFaceH closest=null;
      foreach(PortalFaceH p in s.Scene.Tracker.GetEntities<PortalFaceH>()) {
        if(hitRight != p.facingRight) continue;
        Vector2 ploc = p.Position;
        if(ploc.Y>absTop || ploc.Y+p.height<absTop+h.height) continue;
        float dist = (ploc.X-frontEdge)*dir;
        if(dist<minDist && dist*2+h.width>=0){
          minDist = dist;
          closest = p;
        }
      }
      if(minDist>=m*dir+PColliderH.margin) return orig(s,m,col,pusher);
      s.Collider = new PColliderH(s,closest,closest.other);
    }
    if(s.Collider is PColliderH pch){
      bool res = orig(s,m,col,pusher);
      pch.Done();
      return res;
    }
    return orig(s,m,col,pusher);
  }
  public override string ToString()=>$"portalFace:{{{(facingRight?"Right":"Left")} {Position} {height}}}";



  [CustomEntity("auspicioushelper/PortalGateH")]
  [CustomloadEntity(nameof(Load))]
  public static class Pair{
    static void Load(Level l, LevelData ld, Vector2 o, EntityData d){
      var e1 = new PortalFaceH(o+d.Position, d.Height, d.Bool("right_facing_f0"), false);
      var e2 = new PortalFaceH(o+d.Nodes[0], d.Height, d.Bool("right_facing_f1"), false);
      e1.other = e2;
      e2.other = e1;
      l.Add([e1,e2]);
      ResetEvents.LazyEnable(typeof(PortalFaceH));
      ResetEvents.LazyEnable(typeof(PColliderH));
    }
  }
}
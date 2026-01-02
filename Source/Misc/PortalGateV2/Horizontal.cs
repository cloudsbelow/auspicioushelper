

using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[ResetEvents.LazyLoadDuration(ResetEvents.RunTimes.OnEnter)]
[Tracked]
public class PortalFaceH:Entity{
  public bool facingRight = false;
  public bool flipped = false;
  public float height;
  PortalFaceH other;
  MTexture texture = GFX.Game["util/lightbeam"];
  NoiseSamplerOS2_2DLoop ogen = new NoiseSamplerOS2_2DLoop(20, 70, 100);
  List<uint> handles = new();
  [Import.SpeedrunToolIop.Static]
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnReload)]
  static Solid fakeSolid;
  StaticMover sm;
  Vector2 movementCounter;
  public PortalFaceH(Vector2 pos, float height, bool facingRight, bool vflip):base(pos){
    Collider = new Hitbox(1,height,facingRight?0:-1,0);
    flipped=vflip;
    this.facingRight = facingRight;
    this.height=height;
    for(int i=0; i<height; i+=2){
      handles.Add(ogen.getHandle());
      handles.Add(ogen.getHandle());
    }
    Add(sm = new StaticMover(){
      SolidChecker = (s)=>CollideCheckOutside(s,Position+new Vector2(facingRight?-1:1,0)),
      OnMove = (Vector2 move)=>{
        movementCounter+=move;
        int numV = (int)Math.Round(movementCounter.Y, MidpointRounding.ToEven);
        int numH = (int)Math.Round(movementCounter.X, MidpointRounding.ToEven);
        if(numH!=0) MoveHExact(numH);

        movementCounter -= new Vector2(numH,numV);
      }
    });
  }
  public override void Update() {
    base.Update();
    ogen.update(Engine.DeltaTime);
  }
  public override void Render(){
    base.Render();
    float wrec1 = (facingRight?1f:-1f) / (float)texture.Width;
    for(int i=4; i<=height-4; i+=4){
      float alpha = Math.Min(1,Math.Max(0,ogen.sample(handles[i]))+0.2f);
      if(alpha<0) continue;
      float length = ogen.sample(handles[i+1])*10+20;
      texture.Draw(Position+new Vector2(0,i), new Vector2(0,0.5f), Color.White*alpha, new Vector2(wrec1 * length, 8), 0);
    }
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
  ref struct BlockPoint:IDisposable{
    Vector2 oldPos;
    bool oldCollidable;
    public BlockPoint(Vector2 Pos){
      oldPos = fakeSolid.Position;
      oldCollidable = fakeSolid.Collidable;
      fakeSolid.Position = Pos;
      fakeSolid.Collidable = true;
    }
    void IDisposable.Dispose() {
      fakeSolid.Collidable = oldCollidable;
      if(oldCollidable){
        fakeSolid.Position = oldPos;
      } else {
        var levelrect = (Engine.Instance.scene as Level)?.Bounds??new Rectangle(0,0,0,0);
        fakeSolid.Position = new(levelrect.X-100,levelrect.Y-100);
      }
    }
  }
  [ResetEvents.OnHook(typeof(Actor),nameof(Actor.MoveVExact))]
  static bool Hook(On.Celeste.Actor.orig_MoveVExact orig, Actor s, int m, Collision c, Solid pusher){
    if(s.Collider is PColliderH pch){
      bool res;
      if(m<0 && pch.distToTop<-m) using(new BlockPoint(pch.f1.Position)) res=orig(s,m,c,pusher);
      else if(m>0 && pch.distToBottom<m) using(new BlockPoint(pch.f1.Position+pch.f1.height*Vector2.UnitY)) res=orig(s,m,c,pusher);
      else return orig(s,m,c,pusher);
      pch.Done();
      return res;
    } else return orig(s,m,c,pusher);
  }
  bool deadly;
  void MoveHExact(int amt){
    if(amt == 0) return;
    bool? oldCollidable = (sm?.Platform as Solid)?.Collidable;
    if(oldCollidable is not null)sm.Platform.Collidable=false;
    int dir = Math.Sign(amt);
    if(facingRight != amt>0){
      Position.X+=amt;
      foreach(Actor a in Scene.Tracker.GetEntities<Actor>()){
        if(a.Collider is not PColliderH pch || (pch.f1 != this && pch.f2 != this)) continue;
        pch.Done();
        if(deadly && a.CollideFirst<Solid>() is Solid s) a.SquishCallback(new(){
          Hit=s, Direction=Vector2.UnitX*amt, Moved=Vector2.UnitX*amt, TargetPosition=a.Position, Pusher=null
        });
      }
    } else {
      Vector2 orig = Position;
      foreach(Actor act in Scene.Tracker.GetEntities<Actor>()){
        if(act.Collider is Hitbox h){
          Vector2 abspos = act.Position+h.Position;
          if(Position.Y>abspos.Y || Position.Y+height<abspos.Y+h.height) continue;
          float frontEdge = abspos.X+(amt>0?0:h.width);
          float dist = (frontEdge-Position.X)*amt;
          if(dist>=Math.Abs(amt)+PColliderH.margin || dist*2+h.width<=0) continue;
          act.Collider = new PColliderH(act, this, other);
        }
        if(act.Collider is PColliderH pch && (pch.f1 == this || pch.f2 == this)){
          Position = orig;
          for(int i=0; i!=amt; i+=dir){
            Position.X = orig.X+i;
            Solid s = act.CollideFirst<Solid>();
            if(s!=null){
              s.Collidable = false;
              if(pch.f1 == this){ //center is on other side; "slamming" case
                act.LiftSpeed = pch.calcspeed(s.LiftSpeed);
                act.MoveHExact(-amt*(pch.flipH?-1:1),act.SquishCallback,s);
              } else { //center is on this side; "extruding" case
                act.MoveHExact(amt,act.SquishCallback,s);
              }
              s.Collidable = true;
            }
          }
          pch.Done();
        }
      }
      Position.X = orig.X+amt;
    }
    if(oldCollidable is {} val) sm.Platform.Collidable=val;
  }
  void moveVExact(int amt){
    if(amt==0) return;
    bool? oldCollidable = (sm?.Platform as Solid)?.Collidable;
    if(oldCollidable is not null)sm.Platform.Collidable=false;
    int dir = Math.Sign(amt);
    List<Actor> inf1 = new();
    List<Actor> inf2 = new();
    HashSet<Entity> removed = Scene.Entities.removing;
    foreach(Actor act in Scene.Tracker.GetEntities<Actor>()){
      if(act.Collider is not PColliderH pch)continue;
      if(pch.f1 == this) inf1.Add(act);
      if(pch.f2 == this) inf2.Add(act);
    }
    using(new BlockPoint(Position+(amt<0?new Vector2(0,height):Vector2.Zero))){
      for(int i=0; i!=amt; i+=dir){
        Position.Y+=dir;
        fakeSolid.MoveVExact(dir);
        // foreach(var a in inf1) if(!removed.Contains(a) && a.Collider is PColliderH pch1){
        //   Solid s = a.CollideFirst<Solid>();
        //   if(s != null) using(Util.WithRestore(ref s.Collidable, false)) a.MoveVExact(dir, a.SquishCallback, s);
        // }
        // foreach(var a in inf2) if(!removed.Contains(a) && a.Collider is PColliderH pch2){
        //   Solid s = a.CollideFirst<Solid>();
        //   if(s != null) using(Util.WithRestore(ref s.Collidable, false)) a.MoveVExact(-dir*pch., a.SquishCallback, s);
        // }
      }
    }
    if(oldCollidable is {} val) sm.Platform.Collidable=val; 
  }

  public override string ToString()=>$"portalFace:{{{(facingRight?"Right":"Left")} {Position} {height}}}";



  [CustomEntity("auspicioushelper/PortalGateH")]
  [CustomloadEntity(nameof(Load))]
  public static class Pair{
    static void Load(Level l, LevelData ld, Vector2 o, EntityData d){
      var e1 = new PortalFaceH(o+d.Position, d.Height, d.Bool("right_facing_f0"), false);
      var e2 = new PortalFaceH(o+d.Nodes[0], d.Height, d.Bool("right_facing_f1"), d.Bool("flipGravity"));
      e1.other = e2;
      e2.other = e1;
      l.Add([e1,e2]);
      ResetEvents.LazyEnable(typeof(PortalFaceH));
      ResetEvents.LazyEnable(typeof(PColliderH));

      if(fakeSolid==null){
        l.Add(fakeSolid = new(new Vector2(-1000,-1000),0,0,false));
        UpdateHook.EnsureUpdateAny();
      }
      fakeSolid.Collider = new DelegatingPointcollider();
    }
  }
}
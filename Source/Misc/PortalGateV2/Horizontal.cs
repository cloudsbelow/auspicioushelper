

using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class PortalFaceH:Entity{
  public bool facingRight = false;
  public bool flipped = false;
  public float height;
  PortalFaceH other;
  MTexture texture = GFX.Game["util/lightbeam"];
  NoiseSamplerOS2_2DLoop ogen = new NoiseSamplerOS2_2DLoop(20, 70, 100);
  Color color;
  List<uint> handles = new();
  [Import.SpeedrunToolIop.Static]
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnReload)]
  static Solid fakeSolid;
  Vector2 renderOffset;
  LiftspeedSm sm;
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
    Add(sm = new LiftspeedSm(){
      SolidChecker = (s)=>CollideCheckOutside(s,Position+new Vector2(facingRight?-1:1,0)),
      OnMoveOther = (Vector2 move)=>{
        movementCounter+=move;
        int numV = (int)Math.Round(movementCounter.Y, MidpointRounding.ToEven);
        int numH = (int)Math.Round(movementCounter.X, MidpointRounding.ToEven);
        if(numH!=0) MoveHExact(numH);
        if(numV!=0) moveVExact(numV);

        movementCounter -= new Vector2(numH,numV);
      },
      OnShake = (Vector2 amount)=>renderOffset+=amount
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
      texture.Draw(Position+renderOffset+new Vector2(0,i), new Vector2(0,0.5f), color*alpha, new Vector2(wrec1 * length, 8), 0);
    }
  }

  public Vector2 getSpeed()=>sm.getLiftspeed();


  [ResetEvents.OnHook(typeof(Actor),nameof(Actor.MoveHExact))]
  static bool Hook(On.Celeste.Actor.orig_MoveHExact orig, Actor s, int m, Collision col, Solid pusher){ 
    var portals = s.Scene.Tracker.GetEntities<PortalFaceH>();
    if(portals.Count==0) return orig(s,m,col,pusher);

    if(pusher != null && s.Collider is PColliderH pch_ && !pch_.fixing &&
      pch_.GetPrimaryRect(out var prim, out var alw) && (alw||!pusher.Collider.Collide(prim.munane()))
    ){
      if(pusher.Collider is Hitbox){
        var f = pch_.GetSecondaryRect();
        m=m+(int)(m<0? s.Right-f.right:s.Left-f.x);
      }
      if(pch_.flipH) m*=-1;
      s.LiftSpeed = pch_.calcspeed(pusher.LiftSpeed,true);
    }

    int dir = Math.Sign(m);
    Vector2 eloc = s.Position;
    if(s.Collider is Hitbox h){
      bool hitRight = m<0;
      float absTop = eloc.Y+h.Position.Y;
      float frontEdge = eloc.X+h.Position.X+(hitRight?0:h.width);
      float minDist=float.PositiveInfinity;
      PortalFaceH closest=null;
      foreach(PortalFaceH p in portals) {
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
      if(s.Collider == pch) pch.Done();
      else if(s.Collider is PColliderH pcho) pcho.Done();
      return res;
    }
    return orig(s,m,col,pusher);
  }
  public static bool fish;
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
      if(s.Collider == pch) pch.Done();
      else if(s.Collider is PColliderH pcho) pcho.Done();
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
      float orig = Position.X;
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
          Position.X = orig;
          for(int i=0; i!=amt; i+=dir){
            Position.X = orig+i+dir;
            Solid s = act.CollideFirst<Solid>();
            if(s!=null){
              s.Collidable = false;
              pch.fixing = true;
              if(pch.f2 == this){ //center is on other side; "slamming" case
                act.LiftSpeed = pch.calcspeed(s.LiftSpeed,true);
                act.MoveHExact(-dir*(pch.flipH?-1:1),act.SquishCallback,s);
              } else { //center is on this side; "extruding" case
                act.LiftSpeed = pch.calcspeed(s.LiftSpeed,false);
                act.MoveHExact(dir,act.SquishCallback,s);
              }
              pch.fixing = false;
              s.Collidable = true;
            }
          }
          pch.Done();
        }
      }
      Position.X = orig+amt;
    }
    if(oldCollidable is {} val) sm.Platform.Collidable=val;
  }
  void moveVExact(int amt){
    if(amt==0) return;
    bool? oldCollidable = (sm?.Platform as Solid)?.Collidable;
    if(oldCollidable is not null)sm.Platform.Collidable=false;
    int dir = Math.Sign(amt);
    float orig = Position.Y;
    List<Entity> toClean = new();
    foreach(Actor act in Scene.Tracker.GetEntities<Actor>()){
      if(act.Collider is not PColliderH pch || !(pch.f1==this || pch.f2==this))continue;
      toClean.Add(act);
      Position.Y=orig;
      for(int i=0; i!=amt; i+=dir){
        Position.Y = orig+i+dir;
        Solid s = act.CollideFirst<Solid>();
        if(s!=null){
          s.Collidable = false;
          pch.fixing = true;
          if(pch.f2 == this){ 
            act.LiftSpeed = pch.calcspeed(s.LiftSpeed,true);
            act.MoveVExact(-dir*(pch.flipV?-1:1),act.SquishCallback,s);
          } else { 
            act.LiftSpeed = pch.calcspeed(s.LiftSpeed,false);
            act.MoveVExact(dir,act.SquishCallback,s);
          }
          pch.fixing = false;
          s.Collidable = true;
        }
      }
    }
    Position.Y = orig+amt;
    using(var bp = new BlockPoint(new(Position.X,amt>0?orig:orig+height)))fakeSolid.MoveVExact(amt);
    foreach(var e in toClean) if(e.Collider is PColliderH pch_) pch_.Done(); 
    if(oldCollidable is {} val) sm.Platform.Collidable=val; 
  }

  public override string ToString()=>$"portalFace:{{{(facingRight?"Right":"Left")} {Position} {height}}}";



  [CustomEntity("auspicioushelper/PortalGateH")]
  [CustomloadEntity(nameof(Load))]
  public static class Pair{
    static void Load(Level l, LevelData ld, Vector2 o, EntityData d){
      var c = Util.hexToColor(d.Attr("color_hex","ffffffaa"));
      var e1 = new PortalFaceH(o+d.Position, d.Height, d.Bool("right_facing_f0"), false){color=c};
      var e2 = new PortalFaceH(o+d.Nodes[0], d.Height, d.Bool("right_facing_f1"), d.Bool("flipGravity")){color=c};
      e1.other = e2;
      e2.other = e1;
      l.Add([e1,e2]);
      ResetEvents.LazyEnable(typeof(PortalFaceH),typeof(PColliderH));

      if(fakeSolid?.Scene != null && fakeSolid.Scene!=l){
        fakeSolid.RemoveSelf();
        fakeSolid=null;
      } 
      if(fakeSolid==null){
        l.Add(fakeSolid = new(new Vector2(-1000,-1000),0,0,false));
        fakeSolid.AddTag(Tags.Global);
        UpdateHook.EnsureUpdateAny();
      }
      fakeSolid.Collider = new DelegatingPointcollider();
      DebugConsole.Write("blah",Util.ColorRemap.Get("#fff").remapRgb(new(0.5,0.3,0,0.5)), new Util.Double4(0.5,0.3,0,0.5).Unpremultiply().Premultiply());
    }
  }
}
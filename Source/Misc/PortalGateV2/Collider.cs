

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

public class PColliderH:ColliderList,DelegatingPointcollider.CustomPointCollision{
  public PortalFaceH f1;
  public PortalFaceH f2;
  public bool flipV;
  public bool fixing = false;
  bool doCamera = false;
  public bool flipH=>f1.facingRight==f2.facingRight;
  public Vector2 flipMult=>new(flipH?-1:1,flipV?-1:1);
  Hitbox orig;
  //prevent wallbounces
  public const float margin = 5;
  public PColliderH(Entity wrap, PortalFaceH f1, PortalFaceH f2){
    this.f1=f1; this.f2=f2;
    flipV = f1.flipped!=f2.flipped;
    if(wrap.Collider is not Hitbox h) throw new Exception("Collider is not hitbox.");
    orig = h;
    #pragma warning disable CL013
    wrap.Scene.Add(new Othersider(wrap, OthersidePos(wrap.Position)));
    #pragma warning restore CL013
  }
  public PColliderH(PColliderH copy, Hitbox n){
    f1=copy.f1; f2=copy.f2; flipV=copy.flipV;
    orig=n; doCamera = copy.doCamera;
  }
  public override void Added(Entity entity) {
    base.Added(entity);
    orig.Added(entity);
  }
  public override void Removed() {
    base.Removed();
    orig.Removed();
  }
  void End(){
    using(new CollideDetourLock()) Entity.Collider = orig;
  }
  public Vector2 calcspeed(Vector2 speed, bool fromOtherside=false){
    Vector2 rel = speed-(fromOtherside?f2:f1).getSpeed();
    if(flipH) rel.X*=-1;
    if(flipV) rel.Y*=-1;
    rel+=(fromOtherside?f1:f2).getSpeed();
    return rel;
  }
  void Swap(){
    if(Entity is Player player){
      Vector2 oldBottom = player.Position - Vector2.UnitY*(GelperIop.IsFlipped(player)? -orig.height:0);
      player.Get<PlayerHair>()?.Nodes.MapInplace(x=>OffsetPosFlip(x,x-oldBottom));
      player.Speed = calcspeed(player.Speed);
      if(flipH)player.Facing = (Facings)(-(int)player.Facing);
      player.DashDir = player.DashDir*flipMult;
    } else if(DynamicData.For(Entity).TryGet("Speed", out Vector2 val)){
      DynamicData.For(Entity).Set("Speed",calcspeed(val));
    }
    
    Vector2? camoffset=null;
    if(Entity.Scene is Level lev && true && Entity is Player pla && false){
      camoffset = lev.Camera.Position-pla.Position;
    }
    Entity.Position = OthersidePos(Entity.Position);
    if(camoffset is Vector2 ncam) (Entity.Scene as Level).Camera.Position = Entity.Position+ncam; 

    if(Entity is Actor a && flipV && GelperIop.SetActorGravity!=null){
      using(new CollideDetourLock()) Entity.Collider = orig;
      GelperIop.SetActorGravity(a,2,1);
      orig = a.Collider as Hitbox;
      if(orig == null) throw new Exception("hum");
      a.Collider = this;
    }
    var temp = f1;
    f1 = f2;
    f2 = temp;
    Done(true);
  }
  public void Done(bool fromSwap = false){ //this is the paranoia of a person who is not ok (entirely redundant)
    Vector2 eloc = Entity.Position;
    float frontEdge = eloc.X+orig.Position.X+(f1.facingRight?0:orig.width);
    float dist = (f1.X-frontEdge)*(f1.facingRight?-1:1);
    if((distToBottom<0 || distToTop<0) && dist>=0) End();
    else if(dist*2+orig.width<0 && !fromSwap) Swap();
    else if(dist>=margin) End();
  }
  Vector2 GetOppositePoint(Vector2 old){
    Vector2 ret =  new(
      f2.Position.X + (old.X-f1.Position.X)*(f1.facingRight!=f2.facingRight?1:-1),
      f2.Position.Y + (flipV? f1.Position.Y+f1.height-old.Y : old.Y-f1.Position.Y)
    );
    return ret;
  }
  Vector2 OffsetPos(Vector2 old, Vector2 offset)=>GetOppositePoint(old+offset)-offset;
  Vector2 OffsetPosFlip(Vector2 old, Vector2 offset)=>GetOppositePoint(old+offset)-offset*flipMult;
  Vector2 OthersidePos(Vector2 old)=>OffsetPos(old,new(orig.Position.X+orig.width/2, orig.Position.Y+orig.height/2));
  bool GetRects(out FloatRect r1, out FloatRect r2){
    var epos = Entity.Position;
    float absleft = epos.X+orig.Position.X;
    float abstop = epos.Y+orig.Position.Y;
    float overlap;
    if(f1.facingRight){
      overlap = Math.Max(0, f1.X - absleft);
      r1 = new(absleft+overlap, abstop, orig.width-overlap, orig.height);
    } else {
      overlap = Math.Max(0, absleft+orig.width - f1.X);
      r1 = new(absleft, abstop, orig.width-overlap, orig.height);
    }
    float nrely = flipV? f1.Position.Y+f1.height-orig.height-abstop : abstop-f1.Position.Y;
    r2 = new(f2.Position.X-(f2.facingRight? 0:overlap), f2.Position.Y+nrely, overlap, orig.height);
    if(overlap<orig.width){
      return overlap>0;
    } else {
      r1 = new(r2.x,r2.y, orig.width, orig.height);
      return false;
    }
  }
  public bool GetPrimaryRect(out FloatRect r1, out bool completelySubsumed){
    var tlc = Entity.Position+orig.Position;
    float overlap = Math.Max(0, f1.facingRight? f1.X-tlc.X : tlc.X+orig.width-f1.X);
    r1 = new(f1.facingRight? tlc.X+overlap : tlc.X, tlc.Y, orig.width-overlap, orig.height);
    completelySubsumed = overlap>=orig.width;
    return overlap>0;
  }
  public FloatRect GetSecondaryRect(){
    var tlc = Entity.Position+orig.Position;
    float overlap = Math.Max(0, f1.facingRight? f1.X-tlc.X : tlc.X+orig.width-f1.X);
    float nrely = flipV? f1.Position.Y+f1.height-orig.height-tlc.Y : tlc.Y-f1.Position.Y;
    return new(f2.Position.X-(f2.facingRight? 0:overlap), f2.Position.Y+nrely, overlap, orig.height);
  }
  public float distToTop=>Entity.Position.Y+orig.Position.Y-f1.Y;
  public float distToBottom=>f1.Y+f1.height-Entity.Position.Y-orig.Position.Y-orig.height;
  public override float Top=>orig.Position.Y;
  public override float Left=>orig.Position.X;
  public override float Width=>orig.width;
  public override float Height=>orig.height;
  public override float Right=>orig.Right;
  public override float Bottom=>orig.Bottom;
  public override bool Collide(Vector2 point){
    return (GetRects(out var r1, out var r2) && r2.CollidePoint(point)) || r1.CollidePoint(point);
  }
  public override bool Collide(Circle c) {
    return (GetRects(out var r1, out var r2) && r2.CollideCircle(c.Center,c.Radius)) || r1.CollideCircle(c.Center,c.Radius);
  }
  public override bool Collide(Rectangle r) {
    return (GetRects(out var r1, out var r2) && r2.CollideExRect(r.X,r.Y,r.Width,r.Height)) || r1.CollideExRect(r.X,r.Y,r.Width,r.Height);
  }
  public override bool Collide(Hitbox h) {
    var o = h.AbsolutePosition;
    return (GetRects(out var r1, out var r2) && r2.CollideExRect(o.X,o.Y,h.width,h.height)) || r1.CollideExRect(o.X,o.Y,h.width,h.height);
  }
  public override bool Collide(ColliderList l) {
    if(l is DelegatingPointcollider upc){
      Vector2 avoid = upc.Entity.Position;
      bool split = GetRects(out var r1, out var r2);
      return split && (r1.CollidePointCompact(avoid) || r2.CollidePointCompact(avoid));
    }else return (GetRects(out var r1, out var r2) && l.Collide(r2.munane())) || l.Collide(r1.munane());
  }
  public override bool Collide(Grid g) {
    return (GetRects(out var r1, out var r2) && g.Collide(r2.munane())) || g.Collide(r1.munane());
  }
  public override bool Collide(Vector2 a, Vector2 b) {
    return (GetRects(out var r1, out var r2) && r2.CollideLine(a,b)) || r1.CollideLine(a,b);
  }
  public override void Render(Camera camera, Color color) {
    if(GetRects(out var r1, out var r2)) Draw.HollowRect(r2.x, r2.y, r2.w, r2.h, color);
    Draw.HollowRect(r1.x, r1.y, r1.w, r1.h, color);
  }

  ref struct CollideDetourLock:IDisposable{
    static bool active;
    public CollideDetourLock(){active=true;}
    void IDisposable.Dispose()=>active=false;
    public static bool IsLocked=>active;
  }
  [ResetEvents.OnHook(typeof(Entity),nameof(Entity.Collider),Util.HookTarget.PropSet)]
  static void SetColliderDetour(Action<Entity, Collider> orig, Entity e, Collider c){
    if(!CollideDetourLock.IsLocked && e.Collider is PColliderH pch && c is not PColliderH and not null){
      if(c is not Hitbox h){
        DebugConsole.Write("Wrong wrong wrong",c);
        throw new Exception("Tried to set collider to illegal value inside portal. Ask clouds about compat");
      }
      e.Collider = new PColliderH(pch, h);
    } else orig(e,c);
  }
  static Collider fixCollider(Collider toFix){
    if(toFix is not PColliderH p) return toFix;
    return p.orig;
  }
  [ResetEvents.ILHook(typeof(Player),nameof(Player.Ducking),Util.HookTarget.PropGet)]
  [ResetEvents.ILHook(typeof(Player),nameof(Player.orig_Update))]
  static void FixEqHook(ILContext ctx){
    ILCursor c = new(ctx);
    int n=0;
    while(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchLdarg0(),
      itr=>itr.MatchCall<Entity>("get_Collider"),
      itr=>itr.MatchCeq()||itr.MatchBeq(out var l)
    )){
      c.GotoNextBestFit(MoveType.After,itr=>itr.MatchLdarg0(), itr=>itr.MatchCall<Entity>("get_Collider"));
      n++;
      c.EmitDelegate(fixCollider);
    }
    if(n!=2) DebugConsole.WriteFailure("The slothful ILHook design failed. Tell clouds to be more rigorous if you encounter this",true);
  }
  [ResetEvents.OnHook(typeof(Holdable),nameof(Holdable.Update))]
  static void adjust(On.Celeste.Holdable.orig_Update orig, Holdable h){
    if(h.IsHeld && h.Holder.Collider is PColliderH pch){
      if(h.Entity.Collider is Hitbox hb) h.Entity.Collider = new PColliderH(h.Entity,pch.f1,pch.f2);
      if(h.Entity.Collider is PColliderH pch_){
        pch_.f1=pch.f1;
        pch_.f2=pch.f2;
      }
    }
    orig(h);
  }


  public class Othersider:Entity{
    Entity e;
    public Othersider(Entity copy, Vector2 loc):base(loc){
      e=copy;
      foreach(Component c in copy.Components) if(c is VertexLight v){
        Add(new VertexLight(v.Position,v.Color,v.Alpha,(int)v.startRadius,(int)v.endRadius));
      }
    }
    public override void Render() {
      if(e.Collider is not PColliderH pch) {
        RemoveSelf();
        return;
      }
      Position = pch.OthersidePos(e.Position);
      Vector2 oldpos = e.Position;
      var delta = -e.Position+Position;
      Vector2 mulMove = new Vector2(pch.flipH?-1:1,pch.flipV?-1:1);

      if(e is Actor act){
        if(e is Player p){
          PlayerHair h = p.Get<PlayerHair>();
          if(pch.flipH) p.Facing = (Facings)(0 - p.Facing);
          var oldHair = h.Nodes.Map(x=>x);
          p.Position+=delta;
          Vector2 oldBottom = oldpos - Vector2.UnitY*(GelperIop.IsFlipped(p)? -pch.orig.height:0);
          for(int i=0; i<h.Sprite.HairCount; i++){
            h.Nodes[i]=pch.OffsetPosFlip(h.Nodes[i],h.Nodes[i]-oldBottom);
          }
          using(Util.WithRestore(ref p.varJumpTimer))
          using(Util.WithRestore(ref p.jumpGraceTimer))
          using(Util.WithRestore(ref p.onGround))
          using(Util.WithRestore(ref p.Speed))
          using(Util.WithRestore(ref p.DashDir))
          do{
            if(pch.flipV) GelperIop.TryFlip(p);
            try{p.Render();}catch(Exception ex){
              DebugConsole.Write("Error in rendering player",ex);
            }
            if(pch.flipV) GelperIop.TryFlip(p);
          } while(false);
          h.Nodes = oldHair;
          if(pch.flipH) p.Facing = (Facings)(0 - p.Facing);
          p.Position=oldpos;
          return;
        }
        if(GelperIop.IsActorInverted?.Invoke(act)??false)mulMove.Y=-mulMove.Y;
        foreach(Component c in e.Components){
          if(c is Sprite s) {
            var temp = s.RenderPosition;
            s.RenderPosition = pch.OffsetPosFlip(s.RenderPosition,s.RenderPosition-Center);
            s.Scale*=mulMove;
            s.Render();
            s.Scale*=mulMove;
            s.RenderPosition = temp;
          }
        }
      }
      e.Position = oldpos;
    }
  }
  public override string ToString()=>"PortalCollider."+RuntimeHelpers.GetHashCode(this);
}

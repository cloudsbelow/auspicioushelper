


using System;
using System.Diagnostics;
using System.Resources;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;
public class PortalColliderH:ColliderList{
  public override float Top=>h.Position.Y;
  public override float Left=>h.Position.X;
  public override float Width=>h.width;
  public override float Height=>h.height;
  public override float Right=>h.Right;
  public override float Bottom=>h.Bottom;
  public override void Added(Entity entity) {
    DebugConsole.Write("Added", h);
    Entity = h.Entity = entity;
  }
  public override void Removed() {
    Entity = h.Entity = null;
  }
  public override Collider Clone() {
    return new PortalColliderH((Hitbox)h.Clone());
  }
  public override void Render(Camera camera, Color color) {
    if(!getAbsoluteRects(out var r1, out var r2)) h.Render(camera,color);
    else {
      Draw.HollowRect(r1.x, r1.y, r1.w, r1.h, color);
      Draw.HollowRect(r2.x, r2.y, r2.w, r2.h, color);
    }
  }
  public PortalColliderH(Hitbox orig){
    h=orig;
    hooks.enable();
  }
  public PortalColliderH(bool initialNode, PortalGateH portal, Entity e){
    if(e.Collider is Hitbox orig){
      ce = initialNode;
      p=portal;
      pmul = p.flipped?new Vector2(-1,1):new Vector2(1,1);
      facesign = ce?p.n2dir:p.n1dir;
      hooks.enable();
      h=orig;
      DebugConsole.Write("Constructor", h);
      e.Collider = this;
      AddOthersider(e);
    } else DebugConsole.WriteFailure("Hitbox for portalable entity must be hitbox",true);
  }
  public void AddOthersider(Entity e){
    e.Scene.Add(m=new PortalOtherOthersider(e.Position, this));
    m.Center=getOthersiderPos();
    //m.Scene = a.Scene
  }
  public Hitbox h;
  public PortalGateH p;
  public bool ce;
  bool facesign = false;
  Vector2 pmul;
  public bool end;
  public bool rectify;
  public bool swapped = false;
  public PortalOtherOthersider m;
  public Vector2 getOthersiderPos()=>pmul*(Entity.Center-p.getpos(ce))+p.getpos(!ce);
  public bool getAbsoluteRects(out FloatRect r1, out FloatRect r2){
    Vector2 ipos = ce?p.npos:p.Position;
    Vector2 opos = ce?p.Position:p.npos;
    Vector2 del = opos-ipos;
    bool inr=ce?p.n2dir:p.n1dir;
    bool outr=ce?p.n1dir:p.n2dir;
    float overlap;
    float absLeft = Entity.Position.X+h.Left;
    if(inr){
      overlap = Math.Max(0,ipos.X - absLeft);
      r1=new FloatRect(absLeft+overlap,h.AbsoluteTop,h.Width-overlap, h.height);
    } else {
      overlap = Math.Max(0,absLeft+h.width - ipos.X);
      r1=new FloatRect(absLeft,h.AbsoluteTop,h.Width-overlap,h.height);
    }
    r2 = new FloatRect(outr?opos.X:opos.X-overlap,h.AbsoluteTop+del.Y,overlap,h.height);
    return overlap>0;
  }

  public void swap(){
    swapped = true;
    m.Center = getOthersiderPos();
    ce = !ce;
    DebugConsole.Write("swap "+Entity.Position.ToString()+" "+m.Position.ToString());
    Vector2? camoffset=null;
    if(Entity.Scene is Level lev && p.instantCamera && Entity is Player pla){
      camoffset = lev.Camera.Position-pla.Position;
    }
    Vector2 temp = Entity.Center;
    Entity.Center=m.Center;
    m.Center=temp;
    Vector2 delta = Entity.Center-temp;
    facesign = ce?p.n2dir:p.n1dir;

    if(Entity is Player pl){
      pl.Speed = calcspeed(pl.Speed,ce);
      if(pl.StateMachine.state == Player.StDash && p.flipped) pl.DashDir.X*=-1;
      if(p.flipped)pl.LiftSpeed=new Vector2(-pl.LiftSpeed.X,pl.LiftSpeed.Y);
      Level l = pl.Scene as Level;
      if(l==null) return;
      if(!((IntRect)l.Bounds).CollidePoint(pl.Position)){
        if(l.Session.MapData.GetAt(pl.Position) is LevelData ld){
          l.NextTransitionDuration=0;
          l.NextLevel(pl.Position,Vector2.One);
        } else {
          l.EnforceBounds(pl);
        }
      }
      if(camoffset is Vector2 ncam) l.Camera.Position = pl.Position+ncam; 
      pl.Hair.MoveHairBy(delta);
    } else {
      Util.ValueHelper<Vector2> v = new(Entity.GetType(), "Speed");
      if(v.valid)v.set(Entity,calcspeed(v.get(Entity), ce));
    }
  }
  public Vector2 calcspeed(Vector2 speed, bool newend){
    Vector2 rel = speed-p.getspeed(!newend);
    if(p.flipped) rel.X*=-1;
    rel+=p.getspeed(newend);
    return rel;
  }
  public bool checkLeaves(int voffset=0){
    float cpos = ce?p.npos.Y:p.Position.Y;
    return Entity.Top+voffset<cpos || Entity.Bottom+voffset>cpos+p.height;
  }
  public bool checkLeavesHorizontal(){
    bool inr=ce?p.n2dir:p.n1dir;
    Vector2 ipos = ce?p.npos:p.Position;
    return inr? ipos.X-Entity.Left<0 : Entity.Right-ipos.X<0;
  }
  public void complete(){
    if(Entity==null) return;
    DebugConsole.Write("Completing");
    if(m.Scene!=null) m.RemoveSelf();
    Entity e = Entity;
    using(new CollideDetourLock()) Entity.Collider = h;
    DebugConsole.Write("After complete: ", e.Collider);
  }
  public bool finish(){
    int signedface = Math.Sign(Entity.CenterX - p.getpos(ce).X)*(facesign?1:-1);
    DebugConsole.Write("In finish", getAbsoluteRects(out var r1, out var r2), r1, r2);
    DebugConsole.Write("addendum", Entity.CenterX, p.getpos(ce), (facesign?1:-1), signedface);
    m.Center=getOthersiderPos();
    if(signedface == -1)swap();
    end = Math.Sign((facesign?Entity.Left:Entity.Right)-p.getpos(ce).X)*(facesign?1:-1) == 1;
    if(end && Entity is Player && p.giveRCB && swapped){
      bool right = ce?p.n2dir:p.n1dir;
      RcbHelper.give(right,ce?p.npos.Y:p.Position.Y);
    }
    if(end) complete();
    return end;
  }

  public override bool Collide(Vector2 p) {
    if(getAbsoluteRects(out var r1, out var r2)) return r1.CollidePoint(p) || r2.CollidePoint(p);
    return h.Collide(p);
  }
  public override bool Collide(Vector2 a, Vector2 b) {
    if(getAbsoluteRects(out var r1, out var r2)) return r1.CollideLine(a,b) || r2.CollideLine(a,b);
    return h.Collide(a,b);
  }
  public override bool Collide(Circle c) {
    if(getAbsoluteRects(out var r1, out var r2)) return r1.CollideCircle(c.Position,c.Radius) || r2.CollideCircle(c.Position,c.Radius);
    return h.Collide(c);
  }
  public override bool Collide(Rectangle r){
    if(getAbsoluteRects(out var r1, out var r2))  return r1.CollideExRect(r.X,r.Y,r.Width,r.Height) || r2.CollideExRect(r.X,r.Y,r.Width,r.Height);
    return h.Collide(r);
  }
  public override bool Collide(Hitbox o){
    if(getAbsoluteRects(out var r1, out var r2)) return o.Intersects(r1.x,r1.y,r1.w,r1.h) || o.Intersects(r2.x,r2.y,r2.w,r2.h);
    return h.Collide(o);
  }
  public override bool Collide(Grid g){
    var flag = getAbsoluteRects(out var r1, out var r2);
    //DebugConsole.Write("Grid ",flag, r1, r2);
    if(g is MiptileCollider mtc){
      return mtc.collideFr(r1) || (flag && mtc.collideFr(r2));
    }
    return g.Collide(r1.munane()) || (flag && g.Collide(r2.munane()));
  }
  bool Intersects(FloatRect r){
    bool flag = getAbsoluteRects(out var r1, out var r2);
    return r1.CollideExRect(r.x,r.y,r.w,r.h) || (flag && r2.CollideExRect(r.x,r.y,r.w,r.h));
  }
  public override bool Collide(ColliderList list) {
    bool flag = getAbsoluteRects(out var r1, out var r2);
    if(list is PortalColliderH pch){
      return pch.Intersects(r1)||(flag && pch.Intersects(r2));
    }
    return list.Collide(r1.munane()) || (flag && list.Collide(r2.munane()));
  }
  public override bool Equals(object obj) {
    DebugConsole.Write("Here", this, obj);
    if(obj is PortalColliderH pch) return this==pch;
    if(obj is Hitbox hitb) return h==hitb;
    return false;
  }
  public override int GetHashCode() {
    return HashCode.Combine(h.GetHashCode(),RuntimeHelpers.GetHashCode(this));
  }
  public static bool operator ==(PortalColliderH a, Hitbox h){
    DebugConsole.Write("Here2", a,h);
    return a.h==h;
  }
  public static bool operator !=(PortalColliderH a, Hitbox h)=>!(a==h);

  class CollideDetourLock:IDisposable{
    static int active;
    public CollideDetourLock(){
      active++;
    }
    void IDisposable.Dispose() {
      if(active>0) active--;
    }
    public static bool IsLocked=>active>0;
    static public PersistantAction reset = new(static ()=>active=0);
  }
  static void SetColliderDetour(Action<Entity, Collider> orig, Entity e, Collider c){
    //DebugConsole.Write("Setting collider",e,c);
    if(!CollideDetourLock.IsLocked && e.Collider is PortalColliderH pch){
      if(c is PortalColliderH og){
        orig(e,og);
        return;
      }
      if(c is not Hitbox and not null) throw new Exception($"Cannot set collider to non-hitbox while in portal: {c.GetType()}");
      if(pch.h==c) return;
      DebugConsole.Write("Setting new collider", c, c.Width, c.Height, c.Position);
      pch.h?.Removed();
      pch.h=(Hitbox)c;
      pch.h?.Added(e);
      return;
    }
    orig(e,c);
  }
  static Hook SetColliderHook;
  public static HookManager hooks = new(()=>{
    auspicioushelperModule.OnReset.enroll(CollideDetourLock.reset);
    SetColliderHook = new Hook(typeof(Entity).GetProperty("Collider").SetMethod,SetColliderDetour);
  },()=>{
    CollideDetourLock.reset.remove();
    SetColliderHook.Dispose();
  },auspicioushelperModule.OnEnterMap);
}



using System;
using System.Diagnostics;
using System.Resources;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;
public class PortalColliderH:ColliderList{
  public PortalColliderH(Hitbox orig){
    h=orig;
    hooks.enable();
  }
  Hitbox h;
  public PortalGateH p;
  public bool ce;
  public bool getAbsoluteRects(out FloatRect r1, out FloatRect r2){
    Vector2 ipos = ce?p.npos:p.Position;
    Vector2 opos = ce?p.Position:p.npos;
    Vector2 del = opos-ipos;
    bool inr=ce?p.n2dir:p.n1dir;
    bool outr=ce?p.n1dir:p.n2dir;
    float overlap;
    float absLeft = Entity.Position.X+h.Left;
    if(inr){
      overlap = Math.Max(0,ipos.X-h.AbsoluteLeft);
      r1=new FloatRect(h.AbsoluteLeft+overlap,h.AbsoluteTop,h.Width-overlap, h.height);
    } else {
      overlap = Math.Max(0,h.AbsoluteRight-ipos.X);
      r1=new FloatRect(h.AbsoluteLeft,h.AbsoluteTop,h.Width-overlap,h.height);
    }
    r2 = new FloatRect(outr?opos.X:opos.X-overlap,h.AbsoluteTop+del.Y,overlap,h.height);
    return overlap>0;
  }
  public override bool Collide(Vector2 p) {
    getAbsoluteRects(out var r1, out var r2);
    return r1.CollidePoint(p) || r2.CollidePoint(p);
  }
  public override bool Collide(Vector2 a, Vector2 b) {
    getAbsoluteRects(out var r1, out var r2);
    return r1.CollideLine(a,b) || r2.CollideLine(a,b);
  }
  public override bool Collide(Circle c) {
    getAbsoluteRects(out var r1, out var r2);
    return r1.CollideCircle(c.Position,c.Radius) || r2.CollideCircle(c.Position,c.Radius);
  }
  public override bool Collide(Rectangle r){
    getAbsoluteRects(out var r1, out var r2);
    return r1.CollideExRect(r.X,r.Y,r.Width,r.Height) || r2.CollideExRect(r.X,r.Y,r.Width,r.Height);
  }
  public override bool Collide(Hitbox o){
    getAbsoluteRects(out var r1, out var r2);
    return o.Intersects(r1.x,r1.y,r1.w,r1.h) || o.Intersects(r2.x,r2.y,r2.w,r2.h);
  }
  public override bool Collide(Grid g){
    getAbsoluteRects(out var r1, out var r2);
    if(g is MiptileCollider mtc){
      return mtc.collideFr(r1) || mtc.collideFr(r2);
    }
    return g.Collide(r1.munane()) || g.Collide(r2.munane());
  }
  bool Intersects(FloatRect r){
    getAbsoluteRects(out var r1, out var r2);
    return r1.CollideExRect(r.x,r.y,r.w,r.h) || r2.CollideExRect(r.x,r.y,r.w,r.h);
  }
  public override bool Collide(ColliderList list) {
    getAbsoluteRects(out var r1, out var r2);
    if(list is PortalColliderH pch){
      return pch.Intersects(r1)||pch.Intersects(r2);
    }
    return list.Collide(r1.munane()) || list.Collide(r2.munane());
  }

  ref struct CollideDetourLock:IDisposable{
    static int active;
    void IDisposable.Dispose() {
      if(active>0) active--;
    }
    public static bool IsLocked=>active>0;
    static public PersistantAction reset = new(static ()=>active=0);
  }
  static void SetColliderDetour(Action<Entity, Collider> orig, Entity e, Collider c){
    if(!CollideDetourLock.IsLocked && e.Collider is PortalColliderH pch){
      if(c is not Hitbox and not null) throw new Exception("Cannot set collider to non-hitbox while in portal");
      if(pch.h==c) return;
      pch.h?.Removed();
      pch.h=(Hitbox)c;
      pch.h?.Added(e);
      return;
    }
    orig(e,c);
  }
  static Hook SetColliderHook;
  static HookManager hooks = new(()=>{
    auspicioushelperModule.OnReset.enroll(CollideDetourLock.reset);
    SetColliderHook = new Hook(typeof(Entity).GetProperty("Collider").SetMethod,SetColliderDetour);
  },()=>{
    CollideDetourLock.reset.remove();
    SetColliderHook.Dispose();
  },auspicioushelperModule.OnEnterMap);
}
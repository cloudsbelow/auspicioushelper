


using System;
using Monocle;
using MonoMod.Cil;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public interface IColliderWrapper{
  //Lower priority colliders go on the outside
  int priority=> throw new NotImplementedException();
  Collider wrapped=>null;
  Collider interceptReplace(Collider o);
  ref struct CollideDetourLock:IDisposable{
    [ResetEvents.NullOn(ResetEvents.Times.LvlReset)]
    static int active=0;
    static int currentPrio;
    int lastPrio;
    bool initialized;
    public CollideDetourLock(int prio){
      active++;
      lastPrio = currentPrio;
      currentPrio = prio;
      initialized = true;
    }
    void IDisposable.Dispose(){
      active--;
      currentPrio = lastPrio;
      if(!initialized) throw new Exception("Throw a rock at me");
    }
    public static bool IsLocked=>active!=0;
    public static int Prio=> active!=0?currentPrio:int.MaxValue;
  }
  static Collider Swap(IColliderWrapper oldCollider, Collider newCollider){
    int num = CollideDetourLock.Prio;
    if(newCollider is IColliderWrapper ocw) num = Math.Min(num, ocw.priority);
    return progress(oldCollider, newCollider, num);
  }
  [ResetEvents.ILHook(typeof(Entity),nameof(Entity.Collider),Util.HookTarget.PropSet)]
  static void SetColliderDetourIL(ILContext ctx){
    ILCursor c = new(ctx);
    ILLabel normal = c.DefineLabel();
    ILLabel end = c.DefineLabel();
    c.EmitLdarg0();
    c.EmitLdfld(typeof(Entity).GetField("collider", Util.GoodBindingFlags));
    c.EmitIsinst(typeof(IColliderWrapper));
    c.EmitDup();
    c.EmitBrfalse(normal);
    c.EmitLdarg1();
    c.EmitDelegate(Swap);
    c.EmitStarg(1);
    c.EmitBr(end);
    c.MarkLabel(normal);
    c.EmitPop();
    c.MarkLabel(end);
  }
  static Collider progress(IColliderWrapper o, Collider c, int prio){
    if(o.priority>=prio) return c;
    else{
      Collider next=c;
      if(o.wrapped is IColliderWrapper oChild) next=progress(oChild, c, prio);
      return o.interceptReplace(next);
    } 
  }
  static T Get<T>(Entity e) where T:IColliderWrapper{
    if(e.Collider is IColliderWrapper cw){
      do{
        if(cw is T found) return found;
        if(cw.wrapped is not IColliderWrapper blah) break;
        cw=blah;
      } while(true);
    }
    return default;
  }
  static bool TryGet<T>(Entity e, out T item) where T:IColliderWrapper{
    if(e.Collider is IColliderWrapper cw){
      do{
        if(cw is T found){
          item=found;
          return true;
        }
        if(cw.wrapped is not IColliderWrapper blah) break;
        cw=blah;
      } while(true);
    }
    item = default;
    return false;
  }
  public static Collider getPastPrio(Entity e, int prio){
    Collider cur = e.Collider;
    while(cur is IColliderWrapper cw && cw.priority<=prio) cur=cw.wrapped;
    return cur;
  }
  public static void AddClass(Entity e, IColliderWrapper c){
    Collider inside = getPastPrio(e, c.priority);
    e.Collider = c.interceptReplace(inside);
  }
  public static void RemoveAtPrio(Entity e, int prio){
    var c = getPastPrio(e, prio);
    using(new CollideDetourLock(prio)) e.Collider = c;
  }
  static Collider fixCollider(Collider toFix){
    while(toFix is IColliderWrapper c) toFix = c.wrapped;
    return toFix;
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

  public abstract class SimpleWrapperclass:ColliderList,IColliderWrapper{
    public Collider wrapped {get;set;}
    Collider IColliderWrapper.interceptReplace(Collider c)=>null;
    public override void Added(Entity entity) {
      base.Added(entity);
      wrapped.Added(entity);
    }
    public override void Removed() {
      base.Removed();
      wrapped.Removed();
    }
    public override float Top=>wrapped.Top;
    public override float Left=>wrapped.Top;
    public override float Width=>wrapped.Width;
    public override float Height=>wrapped.Height;
    public override float Right=>wrapped.Right;
    public override float Bottom=>wrapped.Bottom;

    public override bool Collide(Vector2 point)=>wrapped.Collide(point);
    public override bool Collide(Circle c)=>wrapped.Collide(c);
    public override bool Collide(Rectangle r)=>wrapped.Collide(r);
    public override bool Collide(Hitbox h)=>wrapped.Collide(h);
    public override bool Collide(ColliderList l)=>l.Collide(wrapped);
    public override bool Collide(Grid g)=>wrapped.Collide(g);
    public override bool Collide(Vector2 a, Vector2 b)=>wrapped.Collide(a,b);
    public SimpleWrapperclass(Collider c)=>wrapped=c;

    public override void Render(Camera camera, Color color) {
      wrapped?.Render(camera, color*0.8f);
    }
  }
}
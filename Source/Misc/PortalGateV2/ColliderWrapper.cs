


using System;
using Monocle;
using MonoMod.Cil;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public interface IColliderWrapper{
  Collider wrapped=>null;
  Collider interceptReplace(Collider o);
  ref struct CollideDetourLock:IDisposable{
    [ResetEvents.NullOn(ResetEvents.RunTimes.OnReset)]
    static int active=0;
    public CollideDetourLock(){active++;}
    void IDisposable.Dispose()=>active--;
    public static bool IsLocked=>active!=0;
  }
  [ResetEvents.OnHook(typeof(Entity),nameof(Entity.Collider),Util.HookTarget.PropSet)]
  static void SetColliderDetour(Action<Entity, Collider> orig, Entity e, Collider c){
    if(!CollideDetourLock.IsLocked && e.Collider is IColliderWrapper cw){
      using(new CollideDetourLock())e.Collider = cw.interceptReplace(c);
    } else orig(e,c);
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
  }
}
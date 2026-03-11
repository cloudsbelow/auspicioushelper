


using System;
using Monocle;
using MonoMod.Cil;
using Celeste.Mod.Helpers;

namespace Celeste.Mod.auspicioushelper;

public interface ColliderWrapper{
  Collider wrapped=>null;
  Collider interceptReplace(Collider o);
  ref struct CollideDetourLock:IDisposable{
    static bool active;
    public CollideDetourLock(){active=true;}
    void IDisposable.Dispose()=>active=false;
    public static bool IsLocked=>active;
  }
  [ResetEvents.OnHook(typeof(Entity),nameof(Entity.Collider),Util.HookTarget.PropSet)]
  static void SetColliderDetour(Action<Entity, Collider> orig, Entity e, Collider c){
    if(!CollideDetourLock.IsLocked && e.Collider is ColliderWrapper cw){
      e.Collider = cw.interceptReplace(c);
    } else orig(e,c);
  }
  static Collider fixCollider(Collider toFix){
    if(toFix is not ColliderWrapper p) return toFix;
    return p.wrapped;
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
}
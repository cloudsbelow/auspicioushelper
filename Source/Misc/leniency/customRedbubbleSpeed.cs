


using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/RedbubbleSpeed")]
[Tracked]
public class RedbubbleSpeed:Trigger{
  float nspeed;
  public RedbubbleSpeed(EntityData d, Vector2 o):base(d,o){
    hooks.enable();
    nspeed = d.Float("newSpeed",240f);
  }
  static float getSpeed(float oldspeed, Player p){
    if(p.CollideFirst<RedbubbleSpeed>() is {} t) return t.nspeed;
    return oldspeed;
  }
  static void CoroutineHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<Player>(nameof(Player.CorrectDashPrecision)),
      itr=>itr.MatchLdcR4(240)
    )){
      c.EmitLdloc1();
      c.EmitDelegate(getSpeed);
    } else DebugConsole.WriteFailure("Failed to make redbubble speed hook",true);
  }
  static ILHook coroutinehook;
  static HookManager hooks = new(()=>{
    MethodInfo dc = typeof(Player).GetMethod("RedDashCoroutine",BindingFlags.Instance | BindingFlags.NonPublic);
    MethodInfo dc2 = dc.GetStateMachineTarget();
    coroutinehook = new(dc2, CoroutineHook);
  },()=>{

  }, auspicioushelperModule.OnEnterMap);
}
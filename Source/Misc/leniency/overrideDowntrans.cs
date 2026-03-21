


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/Downtrans")]
[Tracked]
public class DowntransOverride:Trigger{
  bool disabled;
  public DowntransOverride(EntityData d, Vector2 o):base(d,o){
    disabled = d.Bool("disabled",false);
    ResetEvents.LazyEnable(typeof(DowntransOverride));
  }
  static bool tryReplace(bool orig, Player p){
    float minWidth = float.PositiveInfinity;
    foreach(DowntransOverride t in p.Scene.Tracker.GetEntities<DowntransOverride>()){
      if(t.Width<minWidth && p.CollideCheck(t)){
        orig = t.disabled;
        minWidth = t.Width;
      }
    }
    return orig;
  }
  [ResetEvents.ILHook(typeof(Level),nameof(Level.EnforceBounds))]
  static void Hook(ILContext ctx){
    ILCursor c = new(ctx);
    int ctr=0;
    while(c.TryGotoNext(MoveType.After,itr=>itr.MatchLdfld<LevelData>(nameof(LevelData.DisableDownTransition)))){
      c.EmitLdarg1();
      c.EmitDelegate(tryReplace);
      ctr++;
    }
    if(ctr==0) DebugConsole.WriteFailure("Could not apply downtransition override hook",true);
  }
}
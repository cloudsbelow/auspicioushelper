


using System;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class SpringTracker:Component{
  public Spring Spring=>Entity as Spring;
  public SpringTracker():base(false,false){
  }
  [OnLoad.OnHook(typeof(Spring),"",Util.HookTarget.Normal)]
  static void SpringCtorHook(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 pos, Spring.Orientations o, bool b){
    orig(self, pos, o, b);
    self.Add(new SpringTracker());
  }
  [ModImportName("FrostHelper")]
  public static class FrosthelperSpring{
    public static Func<Spring,bool> IsCeilingSpring;
    public static Func<Spring,Vector2> GetSpringSpeedMultiplier;
  }
  public static bool DownSpring(Spring s)=>FrosthelperSpring.IsCeilingSpring is {} a? a(s):false;
  public static Vector2 Multiplier(Spring s)=>FrosthelperSpring.GetSpringSpeedMultiplier is {} a?a(s):Vector2.One;
}
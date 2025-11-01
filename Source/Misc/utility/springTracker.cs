


using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class SpringTracker:Component{
  public Spring Spring=>Entity as Spring;
  public SpringTracker():base(false,false){
  }
  static void SpringCtorHook(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 pos, Spring.Orientations o, bool b){
    orig(self, pos, o, b);
    self.Add(new SpringTracker());
  }
  [OnLoad]
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.Spring.ctor_Vector2_Orientations_bool+=SpringCtorHook;
  },()=>{
    On.Celeste.Spring.ctor_Vector2_Orientations_bool-=SpringCtorHook;
  });
}
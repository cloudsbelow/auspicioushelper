



using System;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/lavasandwichAligner")]
public class lavasandwichAligner:Entity{
  static float ncentery;
  static Scene usedinscene;
  public lavasandwichAligner(EntityData d, Vector2 offset):base(d.Position+offset){
    ncentery = d.Position.Y;
    usedinscene = null;
    hooks.enable();
    Depth=1;
  }
  public override void Render(){
    base.Render();
    Atlasifyer.DebugRenderAt(Position);
  }
  private static Hook centerYhook;
  static float centerYdetour(Func<SandwichLava,float> orig, SandwichLava self){
    float f = orig(self);
    if(usedinscene == null) usedinscene = self.Scene;
    if(usedinscene != self.Scene) return f;
    return ncentery-90;
  }
  static HookManager hooks = new HookManager(()=>{
    MethodInfo lavacentermethod = typeof(SandwichLava).GetMethod(
        "get_centerY",
        BindingFlags.Instance | BindingFlags.NonPublic
    );
    if(lavacentermethod == null){
      DebugConsole.Write("Could not find thing to hook");
    }else
    centerYhook = new Hook(lavacentermethod, centerYdetour);
  }, void ()=>{
    centerYhook.Dispose();
  },auspicioushelperModule.OnEnterMap);
}
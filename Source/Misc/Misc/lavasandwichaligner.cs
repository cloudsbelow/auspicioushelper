



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
    ResetEvents.Hooks<lavasandwichAligner>.enable();
    Depth=1;
  }
  public override void Render(){
    base.Render();
    Atlasifyer.DebugRenderAt(Position);
  }
  [ResetEvents.OnHook(typeof(SandwichLava), nameof(SandwichLava.centerY), Util.HookTarget.PropGet)]
  static float centerYdetour(Func<SandwichLava,float> orig, SandwichLava self){
    float f = orig(self);
    if(usedinscene == null) usedinscene = self.Scene;
    if(usedinscene != self.Scene) return f;
    return ncentery-90;
  }
}
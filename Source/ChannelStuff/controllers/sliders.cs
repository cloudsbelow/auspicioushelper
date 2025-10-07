

using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.auspicioushelper.Import;
using System.Collections.Generic;

namespace Celeste.Mod.auspicioushelper;

public class ChannelSliderThing:Entity{
  [SpeedrunToolIop.Static]
  static Dictionary<string,List<ChannelSliderThing>> list = new();
  public ChannelSliderThing(EntityData d, Vector2 o):base(d.Position+o){
  }
  static HookManager hooks = new(()=>{

  },()=>{

  });
}
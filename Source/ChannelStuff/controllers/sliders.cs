

using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.auspicioushelper.Import;
using System.Collections.Generic;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

// public class ChannelSliderThing:Entity{
//   [SpeedrunToolIop.Static]
//   static Dictionary<string,List<ChannelSliderThing>> list = new();
//   public ChannelSliderThing(EntityData d, Vector2 o):base(d.Position+o){
//   }
//   static HookManager hooks = new(()=>{

//   },()=>{

//   });
// }

[CustomEntity("auspicioushelper/ChannelToFlag")]
public class ChannelFlagThing:Entity{
  string channel;
  string flag;
  public ChannelFlagThing(EntityData d, Vector2 o):base(d.Position+o){
    channel=d.Attr("channel");
    if(!d.tryGetStr("flag", out flag)) flag='@'+channel;
  }
  public override void Added(Scene scene) {
    Add(new ChannelTracker(channel,(int val)=>(scene as Level).Session.SetFlag(flag,val!=0)));
    base.Added(scene);
  }
}
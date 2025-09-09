using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelClearController")]
public class ChannelClearController:Entity {
  public ChannelClearController(EntityData data, Vector2 offset):base(new Vector2(0,0)){
    if(data.Bool("clear_all",false)){
      ChannelState.clearChannels("");
    }
    string s=data.Attr("clear_prefix","");
    if(s!=""){
      //List<string> toRemove = new List<string>();
      ChannelState.clearChannels(s);
    }
    ChannelState.SetChannel(data.Attr("channel",""),data.Int("value",0));
    if(data.tryGetStr("advanced",out var str))new ChannelState.AdvancedSetter(str).Apply();
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    RemoveSelf();
  }
}
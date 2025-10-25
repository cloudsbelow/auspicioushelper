


using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelHotColdWatcher")]
public class ChannelCoreWatcher:Entity {

  public int hotset;
  public int coldset;
  public string channel;
  public ChannelCoreWatcher(EntityData data, Vector2 offset):base(new Vector2(0,0)){
    hotset = data.Int("Hot_value",0);
    coldset = data.Int("Cold_value",1);
    channel = data.Attr("channel","");
    Add(new CoreModeListener(OnChangeMode));
  }
  public override void Added(Scene scene){
    base.Added(scene);
    OnChangeMode(SceneAs<Level>().coreMode);
  }

  public void OnChangeMode(Session.CoreModes mode){
    ChannelState.SetChannel(channel, mode==Session.CoreModes.Cold? coldset:hotset);
  }
}
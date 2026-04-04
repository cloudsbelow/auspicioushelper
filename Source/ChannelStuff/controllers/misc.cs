


using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelHotColdWatcher")]
public class ChannelCoreWatcher:Entity {
  public float hotset;
  public float coldset;
  public string channel;
  public ChannelCoreWatcher(EntityData data, Vector2 offset):base(new Vector2(0,0)){
    hotset = data.Float("Hot_value",0);
    coldset = data.Float("Cold_value",1);
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


[CustomEntity("auspicioushelper/ChannelToFlag")]
public class ChannelFlagThing:Entity{
  string channel;
  string flag;
  public ChannelFlagThing(EntityData d, Vector2 o):base(d.Position+o){
    channel=d.Attr("channel");
    if(!d.tryGetStr("flag", out flag)) flag='@'+channel;
  }
  public override void Added(Scene scene) {
    Add(new ChannelTracker(channel,(double val)=>(scene as Level).Session.SetFlag(flag,val!=0),true));
    base.Added(scene);
  }
}
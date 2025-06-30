
using System;
using Celeste.Mod.auspicioushelper;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[Tracked]
public class ChannelTracker : Component, IChannelUser{
  public string channel {get; set;}
  public int value;
  Action<int> onChannelChange;
  public ChannelTracker(string channel, Action<int> onChannelChange, bool immediateInvoke = false):base(false, false){
    this.channel=channel;
    this.onChannelChange=onChannelChange;
    value = ChannelState.watch(this);
    if(immediateInvoke) onChannelChange(value);
  }
  public void setChVal(int val){
    value = val;
    onChannelChange(val);
  }
  public override void Removed(Entity entity){
    base.Removed(entity);
    ChannelState.unwatchNow(this);
  }
  public override void EntityRemoved(Scene scene){
    base.EntityRemoved(scene);
    ChannelState.unwatchNow(this);
  }
}
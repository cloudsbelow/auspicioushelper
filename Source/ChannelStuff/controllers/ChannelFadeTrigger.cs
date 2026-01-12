


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/FadeChannelTrigger")]
public class ChannelFadeTrigger:Trigger{
  string channel;
  string activeChannel;
  FloatChannel from;
  FloatChannel to;
  private PositionModes positionMode;
  bool active=true;
  byte onlyOnce;
  public ChannelFadeTrigger(EntityData d, Vector2 o):base(d,o){
    from = new(d.Attr("from","0"));
    to = new(d.Attr("to","1"));
    channel = d.Attr("channel","fade");
    positionMode = d.Enum("positionMode", PositionModes.NoEffect);
    activeChannel = d.Attr("activeChannel","");
    onlyOnce = d.Bool("onlyOnce")?(byte)2:(byte)0;
  }
  public override void Added(Scene scene) {
    if(!string.IsNullOrWhiteSpace(activeChannel)) Add(new ChannelTracker(activeChannel, (v)=>active=v!=0, true)); 
  }
  public override void OnStay(Player player) {
    base.OnStay(player);
    if(active){
      float val = GetPositionLerp(player, positionMode);
      ChannelState.SetChannel(channel,to*val+from*(1-val));
      onlyOnce |= 1;
    }
  }
  public override void OnLeave(Player player) {
    base.OnLeave(player);
    if(onlyOnce == 3) RemoveSelf();
  }
}
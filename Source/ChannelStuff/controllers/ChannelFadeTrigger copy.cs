


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelPosition")]
public class ChannelPositionTrigger:Trigger{
  string xchannel;
  string ychannel;
  string activeChannel;
  string path;
  bool active = true;
  bool onlyInside = false;
  public ChannelPositionTrigger(EntityData d, Vector2 o):base(d,o){
    xchannel = d.Attr("xChannel","x");
    ychannel = d.Attr("yChannel","y");
    activeChannel = d.Attr("activeChannel","");
    onlyInside = d.Bool("onlyWhenInside",false);
    path = d.Attr("path","");
    Depth = d.Int("depth",-1);
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    if(!string.IsNullOrWhiteSpace(activeChannel)) Add(new ChannelTracker(activeChannel, (v)=>active=v!=0, true)); 
  }
  public override void Update() {
    base.Update();
    if(FoundEntity.findEnt(path) is {} ent && active){
      Vector2 center = ent.Center;
      if(onlyInside && !CollidePoint(center)) return;
      Vector2 delta = center - new Vector2(Left,Top);
      Vector2 size = new(Width,Height);
      ChannelState.SetChannel(xchannel, Util.Saturate(delta.X/size.X));
      ChannelState.SetChannel(ychannel, Util.Saturate(delta.Y/size.Y));
    }
  }
}
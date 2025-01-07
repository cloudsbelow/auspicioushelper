using Celeste.Mods.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
public interface IChannelUser{
  public int channel {get; set;}
  public void setChVal(int val);
}
[Tracked(true)]
public class ChannelBaseEntity:Entity, IChannelUser{
  public int channel {get; set;}
  public static ChannelMaterialsA layerA;
  public ChannelBaseEntity(Vector2 pos, int _channel):base(pos){
    channel = _channel;
  }
  public ChannelBaseEntity(Vector2 pos):base(pos){
    channel = 0;
  }
  public virtual void setChVal(int val){

  }
  public override void Added(Scene scene)
  {
      base.Added(scene);
      ChannelState.watch(this);
  }
}



using Celeste;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Celeste.Mods.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeleste.Mods.auspicioushelper;


[CustomEntity("auspicioushelper/ChannelJelly")]
public class ChannelJelly : Glider, IChannelUser {

  JumpThru platform;
  public enum JellyState {
    normal,
    platform,
    fallable,
    falling,
    normalWithPlatform,
  }
  public JellyState[] state = new JellyState[2];
  public int csidx;
  public JellyState cs;
  public int channel {get; set;}
  const int platformWidth = 24;
  
  public ChannelJelly(EntityData data, Vector2 offset): base(data.Position+offset, false, false){
    channel = data.Int("channel",0);
    for(int i=0; i<2; i++){
      state[i] = data.Attr("state"+i.ToString(),"normal") switch {
        "normal"=>JellyState.normal,
        "platform"=>JellyState.platform,
        "fallable"=>JellyState.fallable,
        "falling"=>JellyState.falling,
        "withplatform"=>JellyState.normalWithPlatform,
        _=>JellyState.normal,
      };
    }
  }
  public void setChVal(int val){
    csidx = ChannelState.readChannel(channel) & 1;
    if(cs==state[csidx]) return;
    cs = state[csidx];
    
    //Grabability
    if(cs == JellyState.normal || cs == JellyState.normalWithPlatform){
      Hold.PickupCollider = new Hitbox(20f, 22f, -10f, -16f);
      sprite.Color = Color.White;
    } else {
      Hold.PickupCollider = null;
      //dDebugConsole.Write($"{Scene.Tracker.GetEntities<Player>().Count}");
      foreach(Player p in Scene.Tracker.GetEntities<Player>()){
        if(p.Holding == Hold){
          p.Drop();
          sprite.Play("idle");
          sprite.Update();
        };
      }
      sprite.Color = new Color(200,200,150,255);
    }

    if(cs != JellyState.normal){
      platform.Position = Position - new Vector2(platformWidth/2, 14);
      platform.Collidable = true;
    } else {
      platform.Collidable = false;
    }
  }
  public override void Added(Scene scene){
    base.Added(scene);
    scene.Add(platform = new JumpThru(Position, platformWidth, false));
    ChannelState.watch(this);
    setChVal(ChannelState.readChannel(channel) & 1);
  }
  public override void Update(){
    if(cs == JellyState.normal){
      base.Update();
      return;
    }
    sprite.Rotation = Calc.Approach(sprite.Rotation, 0, 3.14f * Engine.DeltaTime);
    if(cs  == JellyState.fallable){
      if(platform.HasPlayerRider()) cs=JellyState.falling;
    }
    if(cs == JellyState.falling || cs == JellyState.normalWithPlatform){
      base.Update();
      platform.Collidable = true;
      foreach(Player p in Scene.Tracker.GetEntities<Player>()){
        if(p.Holding == Hold){
          platform.Collidable = false;
        }
      }
      platform.MoveTo(Position - new Vector2(platformWidth/2, 14));
    }
  }
}
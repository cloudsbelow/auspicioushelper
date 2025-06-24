


using System;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


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
  public string channel {get; set;}
  const int platformWidth = 24;
  
  public ChannelJelly(EntityData data, Vector2 offset): base(data.Position+offset, false, false){
    channel = data.Attr("channel","");
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
    Hold.OnPickup = OnPickupHook;
    Hold.OnRelease = OnReleaseHook;
  }
  public void setChVal(int val){
    csidx = ChannelState.readChannel(channel) & 1;
    //if(cs==state[csidx]) return;
    cs = state[csidx];
    
    //Grabability
    try{
      if(cs == JellyState.normal || cs == JellyState.normalWithPlatform){
        Hold.PickupCollider = new Hitbox(20f, 22f, -10f, -16f);
        sprite.Color = Color.White;
      } else {
        Hold.PickupCollider = new Hitbox(-999f,-999f,0,0);
        fallingSfx.Stop();
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
    } catch(Exception ex){
      DebugConsole.Write(ex.ToString());
    }
  }
  public override void Added(Scene scene){
    base.Added(scene);
    scene.Add(platform = new JumpThru(Position, platformWidth, false));
    ChannelState.watch(this);
    setChVal(ChannelState.readChannel(channel) & 1);
    //DebugConsole.Write("called added");
  }
  public override void Update(){
    if(cs == JellyState.normal){
      base.Update();
      return;
    }
    sprite.Rotation = Calc.Approach(sprite.Rotation, 0, 3.14f * Engine.DeltaTime);
    sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, 1, Engine.DeltaTime * 2f);
    sprite.Scale.X = Calc.Approach(sprite.Scale.X, (float)Math.Sign(sprite.Scale.X) * 1, Engine.DeltaTime * 2f);
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
  private void OnPickupHook(){
    OnPickup();
    platform.AddTag(Tags.Persistent);
  }
  private void OnReleaseHook(Vector2 force){
    OnRelease(force);
    platform.RemoveTag(Tags.Persistent);
  }
  public override void Removed(Scene scene){
      base.Removed(scene);
      platform.RemoveSelf();
  }
}
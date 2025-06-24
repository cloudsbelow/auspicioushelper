using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

//I implemented this entirely on a plane in notepad. I don't even know at this point.

namespace Celeste.Mod.auspicioushelper;

[Tracked]
[CustomEntity("auspicioushelper/ChannelSwitch")]
public class ChannelSwitch:ChannelBaseEntity {
  public bool onOnly;
  public bool offOnly;
  public int onVal;
  public int offVal;
  public bool on;
  public float cooldown;
  public float maxcd;
  Sprite sprite;
  public bool usable(){
    if(!onOnly || on){
      return !offOnly || !on;
    }
    return false;
  }

  public ChannelSwitch(EntityData data, Vector2 offset):base(data.Position+offset){
    Depth=5;
    Collider = new Hitbox(16f, 24f, -8f, -12f);
    if(data.Bool("player_toggle",true)){
      Add(new PlayerCollider(OnPlayer));
    }
    if(data.Bool("throwable_toggle",false)){
      Add(new HoldableCollider(OnHoldable, new Hitbox(20f, 28f, -10f, -14f)));
    }
    if(data.Bool("seeker_toggle",false)){
      
      Add(new SeekerCollider(OnSeeker, new Hitbox(24f, 32f, -12f, -16f)));
    }
    Add(sprite = GFX.SpriteBank.Create("coreFlipSwitch"));
    channel = data.Attr("channel","");
    onOnly = data.Bool("on_only",false);
    offOnly = data.Bool("off_only",false);
    onVal = data.Int("on_value",1);
    offVal = data.Int("off_value",0);
    maxcd = data.Float("cooldown",1f);
    //DebugConsole.Write("Constructed switch");
  }
  public override void Added(Scene scene){
    base.Added(scene);
    //DebugConsole.Write("Added switch");
    on = (ChannelState.readChannel(channel)&1)==1;
    if(usable()){
      sprite.Play(on?"iceLoop":"hotLoop");
    } else {
      sprite.Play(on? "iceOffLoop":"hotOffLoop");
    }
  }
  public override void setChVal(int val){
    bool nval = (val &1)==1;
    if(nval == on) return;
    on=nval;
    Audio.Play(on ? "event:/game/09_core/switch_to_cold" : "event:/game/09_core/switch_to_hot", Position);
    if (usable()){
      sprite.Play(on ? "ice" : "hot");
    } else{
      Audio.Play("event:/game/09_core/switch_dies", Position);
      sprite.Play(on ? "iceOff" : "hotOff");
    }
  }
  
  public void hit(){
    if(usable() && cooldown<=0f){
      //DebugConsole.Write("Hit switch!");
      ChannelState.SetChannel(channel,on?offVal:onVal);
      Level level = SceneAs<Level>();
      Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
      level.Flash(Color.White * 0.15f, drawPlayerOver: false);
      Celeste.Freeze(0.05f);
      cooldown = maxcd;
    }
  }
  public void OnPlayer(Player player){
    hit();
  }
  public void OnHoldable(Holdable h){
    hit();
  }
  public void OnSeeker(Seeker s){
    hit();
  }

  public override void Update(){
    base.Update();
    if(cooldown>0) cooldown-=Engine.DeltaTime;
  }
}



using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelPlayerTrigger")]
public class ChannelPlayerTrigger:Trigger{
  string channel {get; set;}
  public enum Op{
    xor, and, or,
    set, max, min,
    add,
  }
  public Op op;
  public double value;
  bool activateOnEnter=false;
  bool activateOnleave=false;
  bool activateOnStay=false;
  bool restore=false;
  bool onlyOnce; 
  ChannelState.AdvancedSetter adv=null;

  public ChannelPlayerTrigger(EntityData data, Vector2 offset):base(data, offset){
    onlyOnce = data.Bool("only_once",false);
    channel = data.Attr("channel","");
    value = data.Float("value",1);
    if(!string.IsNullOrWhiteSpace(data.Attr("advanced"))){
      adv = new(data.Attr("advanced"));
    }
    if(data.Bool("everywhere",false)){
      Collider = new Hitbox(int.MaxValue,int.MaxValue,-int.MaxValue/2,-int.MaxValue/2);
    }
    switch(data.Attr("action")){
      case "dash":
        Add(new DashListener((Vector2 d)=>{
          if(PlayerIsInside)activate();
        }));
        break;
      case "jump":
        Add(new JumpListener((int t)=>{
          if(PlayerIsInside)activate();
        }));
        break;
      case "enter":
        activateOnEnter=true; break;
      case "leave":
        activateOnleave=true; break;
      case "stay":
        activateOnStay=true; break;
      case "enter (reset on leave)":
        activateOnEnter=true;
        restore=true;
        break;

      default: DebugConsole.Write("Unknown action"+data.Attr("action")); break;
    }
    op = data.Attr("op","") switch {
      "xor"=>Op.xor,
      "and"=>Op.and,
      "or"=>Op.or,
      "max"=>Op.max,
      "min"=>Op.min,
      "add"=>Op.add,
      _=>Op.set
    };
  }
  double? restoreTo = null;
  public void activate(){
    double oldval = ChannelState.readChannel(channel);
    if(restoreTo == null) restoreTo = oldval;
    //DebugConsole.Write(op.ToString());
    ChannelState.SetChannel(channel, op switch {
      Op.set => value,
      Op.xor => (int)oldval ^ (int)value,
      Op.and => (int)value & (int)oldval,
      Op.or => (int)value | (int)oldval,
      Op.add => oldval+value,
      Op.max => Math.Max(oldval,value),
      Op.min => Math.Min(value, oldval),
      _=>oldval
    });
    adv?.Apply();
    if(onlyOnce) RemoveSelf();
  }
  public override void OnEnter(Player player){
    base.OnEnter(player);
    if(activateOnEnter) activate();
  }
  public override void OnLeave(Player player){
    base.OnLeave(player);
    if(activateOnleave) activate();
    if(restore && restoreTo is {} oldval)ChannelState.SetChannel(channel,oldval);
    restoreTo = null;
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    if(restore && restoreTo is {} oldval)ChannelState.SetChannel(channel,oldval);
    restoreTo = null;
  }
  public override void SceneEnd(Scene scene) {
    base.SceneEnd(scene);
    if(restore && restoreTo is {} oldval)ChannelState.SetChannel(channel,oldval);
    restoreTo = null;
  }
  public override void OnStay(Player player) {
    base.OnStay(player);
    if(activateOnStay) activate();
  }
}
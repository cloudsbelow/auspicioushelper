


using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.auspicioushelper.Wrappers;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelPlayerTrigger")]
[Tracked]
public class ChannelPlayerTrigger:Trigger{
  string channel {get; set;}
  public enum Op{
    xor, and, or,
    set, max, min,
    add,
  }
  public Op op;
  ChannelState.FloatCh value;
  string valuestr;
  bool activateOnEnter=false, activateOnleave=false,  activateOnStay=false;
  bool onlyOnce, everywhere, restore=false, triggerParent; 
  ChannelState.AdvancedSetter adv=null;
  ChannelState.BoolCh onlywhen;

  public ChannelPlayerTrigger(EntityData data, Vector2 offset):base(data, offset){
    onlyOnce = data.Bool("only_once",false);
    onlywhen = data.ChannelBool("OnlyWhenChannel",true);
    channel = data.Attr("channel","");
    value = data.ChannelFloat("value",1);
    valuestr = data.Attr("value","");
    triggerParent = data.Bool("triggerParent",false);
    if(!string.IsNullOrWhiteSpace(data.Attr("advanced"))){
      adv = new(data.Attr("advanced"));
    }
    everywhere = data.Bool("everywhere",false);
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
      case "stay (reset on leave)":
        activateOnStay=true;
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
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(everywhere) Collider = new Hitbox(200_000_000,200_000_000,-100_000_000,-100_000_000);
  }
  double? restoreTo = null;
  public void activate(){
    if(!onlywhen) return;
    double oldval = ChannelState.readChannel(channel);
    if(restoreTo == null) restoreTo = oldval;
    if(triggerParent) Get<ChildMarker>()?.Trigger(null);

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
  bool isLike(ChannelPlayerTrigger other)=> other.restore && other.channel==channel && other.op==op && other.valuestr==valuestr;
  public override void OnEnter(Player player){
    base.OnEnter(player);
    if(restore){
      foreach(ChannelPlayerTrigger t in Scene.Tracker.GetEntities<ChannelPlayerTrigger>()){
        if(t!=this && t.Triggered && this.isLike(t)){
          restoreTo = t.restoreTo;
          return;
        }
      }
    }
    if(activateOnEnter) activate();
  }
  public override void OnLeave(Player player){
    base.OnLeave(player);
    if(activateOnleave) activate();
    if(restore && restoreTo is {} oldval){
      bool flag=false;
      foreach(ChannelPlayerTrigger t in Scene.Tracker.GetEntities<ChannelPlayerTrigger>()){
        if(t!=this && this.isLike(t) && player.CollideCheck(t)){
          flag = true;
          if(!t.Triggered){
            t.Triggered = true;
            player.triggersInside.Add(t);
            t.restoreTo = oldval;
          }
        }
      }
      if(!flag) ChannelState.SetChannel(channel,oldval);
    }
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
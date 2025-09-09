


using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;

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
  public int value;
  bool activateOnEnter=false;
  bool activateOnleave=false;
  bool onlyOnce;
  ChannelState.AdvancedSetter adv=null;

  public ChannelPlayerTrigger(EntityData data, Vector2 offset):base(data, offset){
    onlyOnce = data.Bool("only_once",false);
    channel = data.Attr("channel","");
    value = data.Int("value",1);
    if(!string.IsNullOrWhiteSpace(data.Attr("advanced"))){
      adv = new(data.Attr("advanced"));
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
  public void activate(){
    int oldval = ChannelState.readChannel(channel);
    //DebugConsole.Write(op.ToString());
    ChannelState.SetChannel(channel, op switch {
      Op.set => value,
      Op.xor => oldval ^ value,
      Op.and => value & oldval,
      Op.or => value | oldval,
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
  }
}
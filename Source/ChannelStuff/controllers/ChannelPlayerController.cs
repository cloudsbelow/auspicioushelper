


using System;
using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelPlayerWatcher")]
public class ChannelPlayerWatcher:Entity{
  string channel {get; set;}
  public float valueWhenMissing;
  enum Preset{
    custom, dashAttacking, grounded, ducking, state, dead, speed, holding
  }
  static Dictionary<Preset, Func<Player, double>> acts= new(){
    {Preset.dashAttacking, static (Player p)=>p.DashAttacking?1:0},
    {Preset.grounded, static (Player p)=>p.onGround?1:0},
    {Preset.ducking, static (Player p)=>p.Ducking?1:0},
    {Preset.state, static (Player p)=>p.StateMachine.State},
    {Preset.dead, static (Player p)=>p.Dead?1:0},
    {Preset.speed, static (Player p)=>(int)Math.Round(p.Speed.Length())},
    {Preset.holding, static (Player p)=>p.Holding!=null?1:0}
  };
  Func<Player, double> action = null;
  List<string> customlist;
  public ChannelPlayerWatcher(EntityData data, Vector2 offset):base(new Vector2(0,0)){
    channel = data.Attr("channel","");
    valueWhenMissing = data.Float("valueWhenMissing",0);
    if(acts.TryGetValue(data.Enum<Preset>("mode", Preset.custom), out var fn)) action = fn;
    else {
      action = customAction;
      customlist = Util.listparseflat(data.Attr("custom"));
    }
  }
  double customAction(Player p){
    object o = p;
    foreach(var s in customlist){
      var d = new DynamicData(o);
      object n = d.Get(s);
      if(o==null){
        DebugConsole.Write($"In channel player watcher, value {s} cannot be gotten from null");
        return valueWhenMissing;
      } else o=n;
    }
    try{
      return Convert.ToDouble(o);
    } catch(Exception){
      return o==null?0:1;
    }
  }
  public override void Update() {
    base.Update();
    Player p = UpdateHook.cachedPlayer??Scene.Tracker.GetEntity<Player>();
    ChannelState.SetChannel(channel,p==null?valueWhenMissing:action(p));
  }
}
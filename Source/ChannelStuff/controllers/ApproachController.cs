

using System;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelApproacher")]
public class ChannelApproachController:Entity{
  ChannelTracker to;
  ChannelTracker amount;
  ChannelTracker ownval;
  bool useDt;
  bool instant;
  string toCh;
  string amountCh;
  string outChannel;
  static Regex numberRe = new Regex(@"^(?:\s*(?:\d+\.\d*)|(?:(0x|0b)?\d+)|(?:\.\d*)\s*)$",RegexOptions.Compiled);
  public ChannelApproachController(EntityData d, Vector2 o):base(d.Position+o){
    toCh = d.Attr("towardsChannel","1");
    if(numberRe.Match(toCh).Success) toCh=$"({toCh})";
    amountCh = d.Attr("amount","");
    instant = string.IsNullOrWhiteSpace(amountCh);
    if(numberRe.Match(amountCh).Success) amountCh=$"({amountCh})";
    useDt = d.Bool("useDt",true);
    outChannel = d.Attr("outChannel","");
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    Add(to = new(toCh,instant? (double nv)=>{
      if(!instant) return;
      DebugConsole.Write("instant set", nv);
      using(Util.WithRestore(ref instant,false)) ChannelState.SetChannel(outChannel,nv);
    }:null,instant));
    if(!instant) Add(amount = new(amountCh));
    if(!string.IsNullOrWhiteSpace(outChannel))Add(ownval = new(outChannel));
  }
  public override void Update() {
    base.Update();
    if(!instant){
      var m = amount.value * (useDt?Engine.DeltaTime:1);
      if(ownval?.value!=to.value && m!=0){
        if(ownval.value<to.value) ChannelState.SetChannel(outChannel,Math.Min(to.value,ownval.value+m));
        else ChannelState.SetChannel(outChannel, Math.Max(to.value,ownval.value-m));
      }
    }
  }
}


using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateChannelmover")]
public class TemplateChannelmover:Template, IChannelUser{
  Vector2 movevec;
  float relspd;
  float asym;
  float dir;
  float prog;
  public string channel {get;set;}
  public override Vector2 virtLoc => Position+sprog*movevec;
  float sprog;
  bool toggle = false;
  enum Easing {
    Linear, Smoothstep, SineInOut, SineIn, QuadIn
  }
  Easing easing;
  public TemplateChannelmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateChannelmover(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    movevec = d.Nodes[0]-d.Position;
    channel = d.Attr("channel","");
    relspd = 1/d.Float("move_time",1);
    asym = d.Float("asymmetry",1f);
    easing = d.Enum<Easing>("easing",Easing.Linear);
    toggle = d.Bool("complete_and_switch",false);
  }
  float ndir;
  public void setChVal(int val){
    ndir = (val&1)==1?1:-1*asym;
  }
  public override void addTo(Scene scene){
    ChannelState.watch(this);
    dir = (ChannelState.readChannel(channel) &1)==1?1:-1*asym;
    prog = dir == 1?1:0;
    base.addTo(scene);
  }
  float lprog;
  public override void Update(){
    base.Update();
    if(!toggle || prog==0 || prog==1) dir=ndir;
    lprog = prog;
    prog = System.Math.Clamp(prog+dir*relspd*Engine.DeltaTime,0,1);
    if(lprog != prog){
      float x = toggle && dir<0?1-prog:prog;
      float deriv=1;
      float y = easing switch {
        Easing.Linear=>x,
        Easing.QuadIn=>Util.QuadIn(x,out deriv),
        Easing.SineIn=>Util.SineIn(x,out deriv),
        Easing.SineInOut=>Util.SineInOut(x,out deriv),
        Easing.Smoothstep=>Util.Smoothstep(x,out deriv),
        _=>x
      };
      sprog = toggle && dir<0?1-y:y;
      ownLiftspeed = dir*relspd*movevec*deriv;
      childRelposSafe();
    } else {
      ownLiftspeed = Vector2.Zero;
    }
  }
}


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
  bool altern = false;
  bool doshake = false;
  Util.Easings easing;
  public TemplateChannelmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateChannelmover(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    movevec = d.Nodes[0]-d.Position;
    channel = d.Attr("channel","");
    relspd = 1/d.Float("move_time",1);
    asym = d.Float("asymmetry",1f);
    easing = d.Enum<Util.Easings>("easing",Util.Easings.Linear);
    toggle = d.Bool("complete",false);
    altern = d.Bool("alternateEasing",true);
    if(d.Bool("complete_and_switch",false)){
      toggle = altern = true;
    }
    doshake = d.Bool("shake",false);
  }
  float ndir;
  public void setChVal(int val){
    ndir = (val&1)==1?1:-1*asym;
    if(altern && !toggle){
      if(ndir>0) prog = Util.getEasingPreimage(easing, sprog);
      else prog=1-Util.getEasingPreimage(easing,1-sprog);
    }
  }
  public override void addTo(Scene scene){
    ChannelState.watch(this);
    dir = (ChannelState.readChannel(channel) &1)==1?1:-1*asym;
    sprog = prog = dir == 1?1:0;
    //DebugConsole.Write($"{prog} {dir} {Position} {virtLoc}");
    base.addTo(scene);
  }
  float lprog;
  public override void Update(){
    base.Update();
    if(!toggle || prog==0 || prog==1) dir=ndir;
    lprog = prog;
    prog = System.Math.Clamp(prog+dir*relspd*Engine.DeltaTime,0,1);
    if(lprog != prog){
      float x = altern && dir<0?1-prog:prog;
      float y = Util.ApplyEasing(easing, x, out var deriv);
      sprog = altern && dir<0?1-y:y;
      ownLiftspeed = dir*relspd*movevec*deriv;
      childRelposSafe();
      if((prog==1||prog==0)&&doshake)shake(0.2f);
    } else {
      ownLiftspeed = Vector2.Zero;
    }
  }
}
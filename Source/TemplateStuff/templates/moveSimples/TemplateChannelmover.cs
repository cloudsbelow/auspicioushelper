

using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateChannelmover")]
public class TemplateChannelmover:Template{
  float relspd;
  float asym;
  public string channel {get;set;}
  SplineAccessor spos;
  protected override Vector2 virtLoc => Position+spos.pos;
  bool toggle = false;
  bool altern = false;
  bool doshake = false;
  Util.Easings easing;
  EntityData dat;
  public TemplateChannelmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateChannelmover(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    dat = d;
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
  int target;
  int low;
  float cur=>low+cfrac;
  float cfrac=0;
  float afrac=0;
  float dir=0;
  public void setChVal(double val){
    target = (int)Math.Floor(val);
    if(toggle || (cfrac!=0 && Math.Sign(dir)==Math.Sign(target-cur))) return;
    dir = target>cur?1:-1*asym;
    if(cfrac == 0){
      if(dir<0){
        low--;
        afrac = cfrac=1;
      }
    } else if(cfrac == 1){
      if(dir>0){
        low++;
        afrac = cfrac = 0;
      }
    } else if(altern){
      if(dir>0) cfrac = Util.getEasingPreimage(easing, afrac);
      else cfrac = 1-Util.getEasingPreimage(easing,1-afrac);
    }
  }
  public override void addTo(Scene scene){
    target = low = (int) Math.Floor(new ChannelTracker(channel, setChVal).AddTo(this).value);
    Spline se;
    spos = new(se=SplineEntity.GetSpline(dat, SplineEntity.Types.simpleLinear), Vector2.Zero, true);
    spos.set(target);
    for(float i=0.001f; i<0.002; i+=0.001f){
      spos.setSidedFromDir(i,1);
    }
    base.addTo(scene);
  }
  public override void Update(){
    base.Update();
    if(cfrac == 0 && low == target){
      ownLiftspeed = Vector2.Zero;
    } else if(Engine.DeltaTime!=0){
      cfrac = Math.Clamp(cfrac+Engine.DeltaTime*dir*relspd,0,1);
      float x = altern && dir<0?1-cfrac:cfrac;
      float y = Util.ApplyEasing(easing, x, out var deriv);
      afrac = altern && dir<0?1-y:y;
      spos.setSidedFromDir(low%spos.numsegs+afrac, Math.Sign(dir));
      ownLiftspeed = spos.tangent*relspd*dir*deriv;
      childRelposSafe();
      if(cfrac == 1){
        afrac = cfrac = 0;
        low++;
      }
      if(cfrac == 0){
        if(doshake){
          if(low == target) shake(0.2f);
          else shake(0.1f);
        }
        if(target<low){
          afrac = cfrac = 1;
          low--;
        }
      }
      DebugConsole.Write("",spos.pos,spos.t,cfrac,afrac);
    }
  }
}
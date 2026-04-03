

using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

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
  bool allowFraction = false;
  float startupTime = 0;
  string soundSuffix=null;
  bool muted=>soundSuffix==null;
  static readonly List<string> allowedSounds = new(){"stone","stonescrape"};
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
    allowFraction = d.Bool("allowFraction",false);
    soundSuffix = d.Attr("sound","none");
    if(!allowedSounds.Contains(soundSuffix)) soundSuffix=null;
    if(!muted) Add(sfx = new SoundSource());
    startupTime = d.Float("startupTime",0);
  }
  float target;
  int low;
  float cur=>low+cfrac;
  float cfrac=0;
  float afrac=0;
  float dir=0;
  float pauseTimer = 0;
  public void setChVal(double val){
    if(!allowFraction){
      target = (int)Math.Floor(val);
      if((toggle && dir!=0 && pauseTimer<=0) || (cfrac!=0 && Math.Sign(dir)==Math.Sign(target-cur))) return;
      if(target==cur){
        dir=0;
        return;
      }
      TryStart();
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
    } else {
      target = (float) val;
      dir = target>cur?1:-1*asym;
    }
  }
  public override void addTo(Scene scene){
    if(!allowFraction){
      target = low = (int) Math.Floor(new ChannelTracker(channel, setChVal).AddTo(this).value);
    } else {
      target = cfrac = afrac = (float) new ChannelTracker(channel, setChVal).AddTo(this).value;
    }
    Spline se;
    spos = new(se=SplineEntity.GetSpline(dat, SplineEntity.Types.simpleLinear), Vector2.Zero, true);
    spos.set(target);
    base.addTo(scene);
  }
  float speedparam{
    get {
      float len = ownLiftspeed.Length();
      return 1/(1+MathF.Exp(-len/150+1)); //me forgetting to divide:
    }
  }
  void Audioplay(string prefix, float? par=null){
    DebugConsole.Write(prefix+soundSuffix,par??speedparam);
    Audio.Play(prefix+soundSuffix,virtLoc,"speed",par??speedparam);
  }
  void Arrive(){
    if(!muted) Audioplay("event:/auspicioushelper/channelmover/impact/");
    dir = 0;
    if(doshake) shake(0.2f); 
    sfx?.Stop();
  }
  void TryStart(){
    if(pauseTimer<=0){
      pauseTimer = startupTime;
      if(pauseTimer<=0){
        if(!muted) Audioplay("event:/auspicioushelper/channelmover/start/",0.5f);
      } else shake(pauseTimer);
      if(!muted) sfx.Play("event:/auspicioushelper/channelmover/loop/"+soundSuffix,"speed",0.5f);
    }
  }
  SoundSource sfx;
  public override void Update(){
    base.Update();
    if(pauseTimer>0){
      ownLiftspeed=Vector2.Zero;
      pauseTimer-=Engine.DeltaTime;
      if(pauseTimer<=0) Audioplay("event:/auspicioushelper/channelmover/start/",0.5f);
      else return;
    }
    if(sfx!=null){
      sfx.Position=virtLoc-Position;
      if(dir!=0) sfx.Param("speed",speedparam);
    }
    if(allowFraction){
      if(afrac == target) ownLiftspeed = Vector2.Zero;
      else {
        bool flag1 = afrac==cfrac;
        afrac = Util.EaseOutApproach(easing, afrac, target, Math.Abs(dir*Engine.DeltaTime), out float deriv);
        bool flag2 = afrac==target;
        spos.setSidedFromDir(Util.SafeMod(afrac,spos.numsegs), Math.Sign(dir));
        ownLiftspeed = spos.tangent*(flag1&&flag2? (afrac-cfrac)/MathF.Max(Engine.DeltaTime,0.001f) : relspd*dir*deriv); 
        childRelposSafe();
        if(flag2){
          cfrac = afrac;
          Arrive();
        }
      }
    } else {
      if(cfrac == 0 && low == target) ownLiftspeed = Vector2.Zero;
      else if(Engine.DeltaTime!=0){
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
          if(low == target) Arrive();
          else {
            if(toggle && Math.Sign(dir)*Math.Sign(target-cur)<0){
              Arrive();
              TryStart();
              dir = target>cur?1:-1*asym;
            } else {
              if(doshake) shake(0.1f);
              if(!muted) Audioplay("event:/auspicioushelper/channelmover/waypoint/");
            }
          }
          if(target<low){
            afrac = cfrac = 1;
            low--;
          }
        }
      }
    }
  }
}
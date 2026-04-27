

using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateChannelmover")]
public class TemplateChannelmover:Template{
  ChannelState.FloatCh movetime;
  ChannelState.FloatCh asym;
  public string channel {get;set;}
  SplineAccessor spos;
  protected override Vector2 virtLoc => bonk? (Position+spos.pos).Round():(Position+spos.pos);
  bool toggle, altern, doshake, allowFraction, bonk, bonkedLastFrame=false;
  Util.Easings easing;
  EntityData dat;
  ChannelState.FloatCh startupTime;
  string soundSuffix=null;
  bool muted=>soundSuffix==null;
  static readonly List<string> allowedSounds = new(){"stone","stonescrape"};
  public TemplateChannelmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateChannelmover(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    dat = d;
    channel = d.Attr("channel","");
    movetime = d.ChannelFloat("move_time",1);
    asym = d.ChannelFloat("asymmetry",1f);
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
    startupTime = d.ChannelFloat("startupTime",0);
    bonk = d.Bool("bonkMode",false);
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
      } else if(doshake) shake(pauseTimer);
      if(!muted) sfx.Play("event:/auspicioushelper/channelmover/loop/"+soundSuffix,"speed",0.5f);
    }
  }
  SoundSource sfx;
  public bool tryMoveTo(float loc, float validLsMult){
    if(!bonk){
      spos.setSidedFromDir(Util.SafeMod(loc, spos.numsegs), Math.Sign(dir));
    } else {
      float delta = Util.SafeMod(dir>0? loc-spos.t:spos.t-loc, spos.numsegs);
      Vector2 likelyMove = (delta*spos.tangent).Abs()+Vector2.One*8;
      var q = TemplateMoveCollidable.getq(this, likelyMove, true, true, false);
      float last = spos.t;
      float orig = last;
      int ds = Math.Sign(dir);
      float limit = spos.t+delta*ds;
      Vector2 oloc = virtLoc;
      HashSet<Vector2> cleared = new();
      for(int i=0; i<10&&Util.SafeMod(last-limit,spos.numsegs)!=0; i++){
        Vector2 cloc = virtLoc;
        while(Util.SafeMod(last-limit,spos.numsegs)!=0){
          spos.moveDistLimit(ds,limit);
          cloc = virtLoc;
          if(!cleared.Contains(cloc)) break;
        }
        if(q.q.Collide(q.s,cloc-oloc)){
          spos.setSidedFromDir(last, ds);
          if(bonkedLastFrame) validLsMult = (last-orig)/MathF.Max(Engine.DeltaTime,0.001f);
          else {
            if(doshake) shake(0.1f);
            if(!muted) Audioplay("event:/auspicioushelper/channelmover/impact/",speedparam);
          }
          bonkedLastFrame = true;
          cleared.Add(cloc);
          goto end;
        }
        last = spos.t;
      }
      bonkedLastFrame = false;
    }
    end:
      ownLiftspeed = validLsMult*spos.tangent;
      childRelposSafe();
      return bonkedLastFrame;
  }
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
    /**
      It shold be noticed that afrac/cfrac have different relationships in these two cases.
      
      Under allowfraction, cfrac serves as a store of the last frame position if the target
      was reached (in order to calc liftspeed). Has no meaning if the target wasn't. 
      Also isn't frac.

      In the other case, cfrac is the uneased distance and afrac the eased one. A
      ctually frac here.
    */
    if(allowFraction){
      if(afrac == target) ownLiftspeed = Vector2.Zero;
      else {
        bool flag1 = afrac==cfrac;
        cfrac = afrac;
        afrac = Util.EaseOutApproach(easing, afrac, target, Math.Abs(dir*Engine.DeltaTime), out float deriv);
        bool flag2 = afrac==target;
        bool useFd = (flag1 && flag2) || movetime==0;
        if(tryMoveTo(afrac, useFd? (afrac-cfrac)/MathF.Max(Engine.DeltaTime,0.001f) : dir*deriv/movetime)){
          afrac = cfrac = spos.t;
        }
        if(flag2) Arrive();
      }
    } else {
      if(cfrac == 0 && low == target) ownLiftspeed = Vector2.Zero;
      else if(Engine.DeltaTime!=0){
        cfrac = movetime==0? target:Math.Clamp(cfrac+Engine.DeltaTime*dir/movetime,0,1);
        bool flip = altern && dir<0;
        float x = flip?1-cfrac:cfrac;
        float y = Util.ApplyEasing(easing, x, out var deriv);
        afrac = flip?1-y:y;
        if(tryMoveTo(low%spos.numsegs+afrac, movetime==0?0:dir*deriv/movetime)){
          afrac = spos.t - MathF.Floor(spos.t);
          cfrac = Util.getEasingPreimage(easing, flip?1-afrac:afrac);
          if(flip) cfrac = 1-cfrac;
        }
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
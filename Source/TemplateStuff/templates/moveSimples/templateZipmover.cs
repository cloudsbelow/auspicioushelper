


using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateZipmover")]
public class TemplateZipmover:Template, ITemplateTriggerable{
  private SoundSource sfx = new SoundSource();
  protected override Vector2 virtLoc=>Position+spos.pos;
  SplineAccessor spos;
  EntityData dat;
  public string channel {get;set;} = null;
  public enum ReturnType{
    loop,
    none,
    normal,
  }
  ReturnType rtype;
  public enum ActivationType{
    ride,
    dash,
    rideAutomatic,
    dashAutomatic,
    manual
  }
  ActivationType atype;
  Util.Easings outEasing = Util.Easings.SineIn;
  Util.Easings inEasing = Util.Easings.SineIn;
  float outSpeed = 2;
  float inSpeed = 0.5f;
  string outgoingSound;
  string returningSound;
  public TemplateZipmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateZipmover(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    Add(new Coroutine(FancySequence()));
    Add(upd = new UpdateHook());
    Add(sfx);
    dat=d;
    rtype = d.Attr("return_type","normal") switch {
      "loop"=>ReturnType.loop,
      "none"=>ReturnType.none,
      _=>ReturnType.normal
    };
    atype = d.Attr("activation_type","ride") switch {
      "ride"=>ActivationType.ride,
      "dash"=>ActivationType.dash,
      "rideAutomatic"=>ActivationType.rideAutomatic,
      "dashAutomatic"=>ActivationType.dashAutomatic,
      "manual"=>ActivationType.manual,
      _=>ActivationType.ride,
    };
    if(!d.Bool("propegateRiding"))prop &= ~Propagation.Riding;
    if(!string.IsNullOrWhiteSpace(d.Attr("channel","")))channel = d.Attr("channel");
    outSpeed = d.Float("speed",2);
    inSpeed = d.Float("returnSpeed",0.5f);
    outEasing = d.Enum<Util.Easings>("easing",Util.Easings.SineIn);
    inEasing = d.Enum<Util.Easings>("returnEasing",Util.Easings.SineIn);
    outgoingSound = d.Attr("outgoingSound","event:/auspicioushelper/zip/ah_zip_start");
    returningSound = d.Attr("returningSound","event:/auspicioushelper/zip/ah_zip_return");
  }
  UpdateHook upd;
  ChannelTracker ct;
  public override void Added(Scene scene) {
    base.Added(scene);
    if(channel!=null) Add(ct=new ChannelTracker(channel, setChVal));
    
  }
  public void setChVal(double val){
    if(triggered || triggerNextFrame) return;
    if(val!=0) OnTrigger(null);
  }
  public override void addTo(Scene scene){
    Spline spline = SplineEntity.GetSpline(dat, SplineEntity.Types.compoundLinear);
    spos = new SplineAccessor(spline, Vector2.Zero,true);
    if(atype == ActivationType.dash || atype==ActivationType.dashAutomatic){
      OnDashCollide = (Player p, Vector2 dir)=>{
        if(dashed == 0){
          dashed = 1;
          Add(new AudioMangler(Audio.Play("event:/new_content/game/10_farewell/fusebox_hit_1"),0.15f));
          return DashCollisionResults.Rebound;
        }
        return DashCollisionResults.NormalCollision;
      };
    }
    base.addTo(scene);
  }
  int dashed;
  bool triggerNextFrame;
  public override void Update(){
    if(triggerNextFrame && !triggered){
      triggered=true;
      triggerNextFrame=false;
    }
    sfx.Position = spos.pos;
    base.Update();
  }
  
  int currentSegment = 0;
  private IEnumerator FancySequence(){
    float at;
    waiting:
      dashed = 0;
      triggered = false;
      yield return null;
      if(triggered) goto going;
      if(ct!=null && ct.value!=0) OnTrigger(null);
      if(atype == ActivationType.ride || atype==ActivationType.rideAutomatic){
        if(hasRiders<Player>()) OnTrigger(null);
      } else if(atype == ActivationType.dash || atype==ActivationType.dashAutomatic){
        if(dashed!=0) OnTrigger(null);
      }
      goto waiting;
    going:
      triggered = true;
      if(outgoingSound.HasContent())sfx.Play(outgoingSound);
      yield return 0.1f;
      at=0;
      while(at<1){
        at+=Engine.DeltaTime*outSpeed;
        float pos = Util.ApplyEasingBounded(outEasing,at, out var deriv);
        spos.setSidedFromDir(pos+currentSegment, at==0?-1:1);
        ownLiftspeed = spos.tangent*deriv*outSpeed;
        childRelposSafe();
        yield return null;
      }
      currentSegment = (currentSegment+1)%spos.numsegs;
      spos.set(currentSegment);
      ownLiftspeed = Vector2.Zero;
      
      Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
      SceneAs<Level>().Shake();
      shake(0.1f);
      if(channel!=null)ChannelState.SetChannel(channel,0);
      sfx.instance?.setParameterValue("start_end",1);
      yield return 0.25f;

      if((atype == ActivationType.rideAutomatic || atype==ActivationType.dashAutomatic) && 
      (rtype == ReturnType.loop || spos.t<spos.numsegs-1)){
        goto going;
      } else{
        if(currentSegment==spos.numsegs-1 && rtype == ReturnType.normal)goto returning;
        if(currentSegment<spos.numsegs-1 || rtype == ReturnType.loop)goto waiting;
        else yield break;
      }

    returning:
      triggered=false;
      yield return 0.25f;
      sfx.Stop();
      if(returningSound.HasContent())sfx.Play(returningSound);
      at=0;
      while(at<1){
        yield return null;
        at+=Engine.DeltaTime*inSpeed;
        float pos = Util.ApplyEasingBounded(inEasing,at, out var deriv);
        spos.setSidedFromDir(currentSegment-pos,at==0?1:-1);
        ownLiftspeed = -spos.tangent*deriv*inSpeed;
        childRelposSafe();
      }
      currentSegment--;
      sfx.instance?.setParameterValue("return_end",1);
      shake(0.1f);
      yield return null;

      ownLiftspeed = Vector2.Zero;
      if(currentSegment>0) goto returning;
      if(channel!=null)ChannelState.SetChannel(channel,0);
      yield return 0.5f;
      sfx.Stop();
      goto waiting;
  }
  public override void Removed(Scene scene){
    base.Removed(scene);
    sfx?.Stop();
  }
  public bool triggered;
  public void OnTrigger(TriggerInfo info){
    if(!TriggerInfo.TestPass(info,this)) return;
    //DebugConsole.Write($"{Position} triggered {upd.updatedThisFrame}");
    if(upd.updatedThisFrame) triggered = true;
    else triggerNextFrame = true;
    if(channel != null) ChannelState.SetChannel(channel,1);
    if((prop&Propagation.Riding)!=0 && parent!=null)parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(info);
  }
}
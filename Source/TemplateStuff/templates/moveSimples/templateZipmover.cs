


using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateZipmover")]
public class TemplateZipmover:Template, ITemplateTriggerable, IChannelUser{
  public enum Themes{
    Normal,
    Moon
  }
  private SoundSource sfx = new SoundSource();
  Themes theme = Themes.Normal;
  public override Vector2 virtLoc=>Position+spos.pos;
  SplineAccessor spos;
  EntityData dat;
  Vector2 offset;
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
  }
  ActivationType atype;
  public TemplateZipmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateZipmover(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    Add(new Coroutine(FancySequence()));
    Add(upd = new UpdateHook());
    Add(sfx);
    dat=d;
    this.offset=offset;
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
      _=>ActivationType.ride,
    };
    if(!d.Bool("propegateRiding"))prop &= ~Propagation.Riding;
    if(!string.IsNullOrWhiteSpace(d.Attr("channel","")))channel = d.Attr("channel");
  }
  UpdateHook upd;
  public override void Added(Scene scene) {
    base.Added(scene);
    if(channel!=null) ChannelState.watch(this);
    
  }
  public void setChVal(int val){
    if(triggered || triggerNextFrame) return;
    if(val!=0) OnTrigger(null);
  }
  public override void addTo(Scene scene){
    Spline spline=null;
    if(!string.IsNullOrEmpty(dat.Attr("spline"))){
      Spline.splines.TryGetValue(dat.Attr("spline"), out spline);
      if(spline == null){
        spline = SplineEntity.constructImpl(dat.Position,dat.Nodes,offset,dat.Attr("spline"),dat.Bool("lastNodeIsKnot",true));
      }
    }
    if(spline == null){
      //DebugConsole.Write("using fallback spline");
      LinearSpline l =new LinearSpline();
      spline = l;
      l.fromNodes(SplineEntity.entityInfoToNodes(dat.Position,dat.Nodes,offset,dat.Bool("lastNodeIsKnot",true)));
    }
    spos = new SplineAccessor(spline, Vector2.Zero);
    if(atype == ActivationType.dash || atype==ActivationType.dashAutomatic){
      OnDashCollide = (Player p, Vector2 dir)=>{
        if(dashed == 0){
          dashed = 1;
          Add(new Coroutine(dashAudioSeq()));
          return DashCollisionResults.Rebound;
        }
        return DashCollisionResults.NormalCollision;
      };
    }
    base.addTo(scene);

  }
  IEnumerator dashAudioSeq(){
    var audio = Audio.Play("event:/new_content/game/10_farewell/fusebox_hit_1");
    yield return 0.15f;
    Audio.Stop(audio,true);
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
  
  
  private IEnumerator FancySequence(){
    bool done; float at;
    waiting:
      dashed = 0;
      triggered = false;
      yield return null;
      if(triggered) goto going;
      if(atype == ActivationType.ride || atype==ActivationType.rideAutomatic){
        if(hasRiders<Player>()) OnTrigger(null);
      } else if(atype == ActivationType.dash || atype==ActivationType.dashAutomatic){
        if(dashed!=0) OnTrigger(null);
      }
      goto waiting;
    going:
      triggered = true;
      sfx.Play((theme == Themes.Normal) ? "event:/game/01_forsaken_city/zip_mover" : "event:/new_content/game/10_farewell/zip_mover");
      yield return 0.1f;
      at=0;
      done = false;
      while(!done){
        yield return null;
        at+=Engine.DeltaTime*2;
        Vector2 old = virtLoc;
        done = spos.towardsNext((float)(2*(Math.PI/2)*Math.Sin(at*Math.PI/2)*Engine.DeltaTime));
        //DebugConsole.Write($"{at}, {spos.t}");
        if(!done) ownLiftspeed = Math.Sign(Engine.DeltaTime)*(virtLoc-old)/Engine.DeltaTime;
        childRelposSafe();
      }
      ownLiftspeed = Vector2.Zero;
      
      Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
      SceneAs<Level>().Shake();
      shake(0.1f);
      if(channel!=null)ChannelState.SetChannel(channel,0);
      yield return 0.25f;

      if((atype == ActivationType.rideAutomatic || atype==ActivationType.dashAutomatic) && 
      (rtype == ReturnType.loop || spos.t<spos.numsegs-1)){
        sfx.Stop();
        goto going;
      } else{
        if(spos.t==spos.numsegs-1 && rtype == ReturnType.normal)goto returning;
        sfx.Stop();
        if(spos.t<spos.numsegs-1 || rtype == ReturnType.loop)goto waiting;
        else yield break;
      }

    returning:
      triggered=false;
      yield return 0.25f;
      sfx.Stop();
      sfx.Play((theme == Themes.Normal) ? "event:/game/01_forsaken_city/zip_mover" : "event:/new_content/game/10_farewell/zip_mover");
      sfx.instance.setTimelinePosition(1000);
      done = false;
      at=0;
      while(!done){
        yield return null;
        at+=Engine.DeltaTime/2;
        Vector2 old = virtLoc;
        done = spos.towardsNext((float)(-(Math.PI/2)*Math.Sin(at*Math.PI/2)*Engine.DeltaTime/2));
        //virtLoc = Position+spos.pos;
        if(!done) ownLiftspeed = Math.Sign(Engine.DeltaTime)*(virtLoc-old)/Engine.DeltaTime;
        childRelposSafe();
      }
      ownLiftspeed = Vector2.Zero;
      shake(0.1f);
      if(spos.t>0) goto returning;
      yield return 0.5f;
      goto waiting;
  }
  public override void Removed(Scene scene){
    base.Removed(scene);
    sfx.Stop();
  }
  public bool triggered;
  public void OnTrigger(StaticMover sm){
    //DebugConsole.Write($"{Position} triggered {upd.updatedThisFrame}");
    if(upd.updatedThisFrame) triggered = true;
    else triggerNextFrame = true;
    if(channel != null) ChannelState.SetChannel(channel,1);
    if((prop&Propagation.Riding)!=0 && parent!=null)parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(sm);
  }
}
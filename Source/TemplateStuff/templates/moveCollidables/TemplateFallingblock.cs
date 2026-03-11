


using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateFallingblock")]
public class TemplateFallingblock:TemplateMoveCollidable{
  public TemplateFallingblock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}

  Vector2 falldir = Vector2.UnitY;
  float basemaxspeed;
  string tch;
  string rch;
  string ImpactSfx = "event:/game/general/fallblock_impact";
  string ShakeSfx = "event:/game/general/fallblock_shake";
  float maxspeed;
  float gravity;
  bool setTch=false;
  bool triggeredByRiding = true;
  UpdateHook upd;
  float waitTimer=0;
  float waitTime=0;
  float[] delays;
  bool triggerOnImpact;
  public TemplateFallingblock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    falldir = d.Attr("direction") switch{
      "down"=> Vector2.UnitY,
      "up"=>-Vector2.UnitY,
      "left"=>-Vector2.UnitX,
      "right"=>Vector2.UnitX,
      _=>Vector2.UnitX
    };
    rch = d.Attr("reverseChannel");
    tch = d.Attr("triggerChannel");
    ImpactSfx = d.Attr("impact_sfx","event:/game/general/fallblock_impact");
    ShakeSfx = d.Attr("shake_sfx","event:/game/general/fallblock_shake");
    basemaxspeed = maxspeed = d.Float("max_speed",130f);
    gravity = d.Float("gravity", 500);
    setTch = d.Bool("set_trigger_channel",false) && !string.IsNullOrWhiteSpace(tch);
    triggeredByRiding = d.Bool("triggeredByRiding",true);
    delays = Util.csparseflat(d.Attr("customFallTiming",""),0.25f,0.1f,-1);
    waitTimer = waitTime = d.Float("maxWaitTiming",0);
    triggerOnImpact = d.Bool("triggerOnImpact",false);

    Add(new Coroutine(Sequence()));
    if(setTch)Add(upd = new UpdateHook());
  }
  IEnumerator Sequence(){
    float speed=0;
    bool first = true;
    while((!triggeredByRiding || !hasPlayerRider()) && !triggered){
      yield return null;
    }
    OnTrigger(null);
    disconnect();
    //emancipate();
    parent?.GetFromTree<IRemovableContainer>()?.RemoveChild(this);
    shake(delays[0]+waitTime);
    Audio.Play(ShakeSfx,Position);
    yield return delays[0];
    while(waitTimer>0 && hasPlayerRider()){
      waitTimer-=Engine.DeltaTime;
      yield return null;
    }
    trying:
      Query qs = getq(falldir*Math.Sign(maxspeed));
      if(TestMove(qs, 1, falldir*Math.Sign(maxspeed))){
        speed = 0;
        if(!first){
          shake(delays[1]+waitTime);
          Audio.Play(ShakeSfx,Position);
          yield return delays[1];
          waitTimer=waitTime;
          while(waitTimer>0 && hasPlayerRider()){
            waitTimer-=Engine.DeltaTime;
            yield return null;
          }
          EndShake();
          goto falling;
        }
      } else {
        yield return null;
        goto trying;
      } 
    falling:
      EndShake();
      first = false;
      yield return null;
      speed = Calc.Approach(speed,maxspeed,gravity*Engine.DeltaTime);
      qs = getq(falldir*speed*Engine.DeltaTime);
      FloatRect lb = Util.levelBounds(Scene);
      if(!qs.s.bounds.CollideFr(lb) && lb.y+lb.h+200<Position.Y) goto removing;
      ownLiftspeed = speed*falldir;
      bool res = falldir.X==0?
        MoveVCollide(qs,speed*Engine.DeltaTime*falldir.Y,0):
        MoveHCollide(qs,speed*Engine.DeltaTime*falldir.X,0);
      if(res){
        ownLiftspeed = Vector2.Zero;
        Audio.Play(ImpactSfx,Position);
        if(triggerOnImpact)parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new TriggerInfo.EntInfo("fallingBlock",this,true));
        if(delays[2]>=0){
          shake(delays[2]);
          yield return delays[2];
        }
        goto trying;
      }
      else goto falling;
    removing:
      yield return null;
      Vector2 fds = falldir*Math.Sign(maxspeed);
      for(int i=0; i<40; i++){
        speed = Calc.Approach(speed,160,500*Engine.DeltaTime);
        Position+=fds*speed*Engine.DeltaTime;
        ownLiftspeed = fds*speed;
        childRelposSafe();
        yield return null;
      }
      destroy(false);
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(!string.IsNullOrWhiteSpace(tch)){
      if(ChannelState.readChannel(tch)!=0) triggered = true;
      else Add(new ChannelTracker(tch,(double val)=>{
        if(val!=0) OnTrigger(null);
      }));
    }
    if(!string.IsNullOrWhiteSpace(rch)){
      Add(new ChannelTracker(rch, (double val)=>{
        maxspeed = val==0?basemaxspeed:-basemaxspeed;
      },true));
    }
  }
  bool triggerNextFrame;
  public override void Update() {
    if(triggerNextFrame && !triggered){
      triggered=true;
      triggerNextFrame=false;
    }
    base.Update();
  }
  public override void OnTrigger(TriggerInfo sm) {
    if(!TriggerInfo.TestPass(sm,this)) return;
    if(!setTch || upd.updatedThisFrame) triggered = true;
    else triggerNextFrame = true;
    if(setTch) ChannelState.SetChannel(tch,1);
  }
}
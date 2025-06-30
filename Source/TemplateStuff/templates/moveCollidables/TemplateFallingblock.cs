


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
  Vector2 basefalldir = Vector2.UnitY;
  string tch;
  string rch;
  string ImpactSfx = "event:/game/general/fallblock_impact";
  string ShakeSfx = "event:/game/general/fallblock_shake";
  float maxspeed;
  float gravity;
  bool setTch=false;
  UpdateHook upd;
  public TemplateFallingblock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    basefalldir = d.Attr("direction") switch{
      "down"=> Vector2.UnitY,
      "up"=>-Vector2.UnitY,
      "left"=>-Vector2.UnitX,
      "right"=>Vector2.UnitX,
      _=>Vector2.UnitX
    };
    falldir = basefalldir;
    rch = d.Attr("reverseChannel");
    tch = d.Attr("triggerChannel");
    ImpactSfx = d.Attr("impact_sfx","event:/game/general/fallblock_impact");
    ShakeSfx = d.Attr("shake_sfx","event:/game/general/fallblock_shake");
    maxspeed = d.Float("max_speed",130f);
    gravity = d.Float("gravity", 500);
    setTch = d.Bool("set_trigger_channel",false) && !string.IsNullOrWhiteSpace(tch);

    Add(new Coroutine(Sequence()));
    if(setTch)Add(upd = new UpdateHook());
  }
  IEnumerator Sequence(){
    float speed;
    bool first = true;
    while(!hasRiders<Player>() && !triggered){
      yield return null;
    }
    OnTrigger(null);
    disconnect();
    shake(0.2f);
    Audio.Play(ShakeSfx,Position);
    yield return 0.25f;
    trying:
      yield return null;
      Query qs = getq(falldir);
      if(TestMove(qs, 1, falldir)){
        speed = 0;
        if(!first){
          shake(0.2f);
          Audio.Play(ShakeSfx,Position);
          yield return 0.1f;
          goto falling;
        }
      } else goto trying;
    falling:
      first = false;
      yield return null;
      speed = Calc.Approach(speed,maxspeed,gravity*Engine.DeltaTime);
      qs = getq(falldir*speed*Engine.DeltaTime);
      if(!qs.s.bounds.CollideFr(Util.levelBounds(Scene)) && !Util.levelBounds(Scene).CollidePoint(Position)) goto removing;
      ownLiftspeed = speed*falldir;
      bool res = falldir.X==0?
        MoveVCollide(qs,speed*Engine.DeltaTime*falldir.Y,0,speed*falldir):
        MoveHCollide(qs,speed*Engine.DeltaTime*falldir.X,0,speed*falldir);
      if(res){
        ownLiftspeed = Vector2.Zero;
        Audio.Play(ImpactSfx,Position);
        goto trying;
      }
      else goto falling;
    removing:
      yield return null;
      Vector2 fds = falldir;
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
      else Add(new ChannelTracker(tch,(int val)=>{
        if(val!=0) OnTrigger(null);
      }));
    }
    if(!string.IsNullOrWhiteSpace(rch)){
      if(ChannelState.readChannel(rch)!=0) falldir=-basefalldir;
      Add(new ChannelTracker(rch, (int val)=>{
        falldir = val==0?basefalldir:-basefalldir;
      }));
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
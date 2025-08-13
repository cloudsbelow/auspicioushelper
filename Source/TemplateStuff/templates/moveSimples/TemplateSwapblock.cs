


using System;
using System.Collections;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateSwapblock")]
public class TemplateSwapblock:Template{
  EventInstance movesfx;
  float progress {
    get=>spos.t;
    set=>spos.t=value;
  }
  float target=0;
  public override Vector2 virtLoc=>Position+spos.pos;
  SplineAccessor spos;
  EntityData dat;
  Vector2 offset;
  float speed=0;
  float maxspeed = 360;
  float maxreturnspeed=120;
  bool returnable = false;
  float returnTimer = 0.8f;
  public TemplateSwapblock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateSwapblock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    dat=d;
    this.offset=offset;
    maxspeed = d.Float("max_speed",360);
    maxreturnspeed = d.Float("max_return_speed",120);
    returnable = d.Bool("returning",false);
    //DebugConsole.Write($"{t.childEntities.Count}");
  }
  bool returning;
  public override void Update(){
    base.Update();
    if(returnTimer>0){
      returnTimer-=Engine.DeltaTime;
      if(returnable && returnTimer<=0 && progress!=0){
        returning = true;
        target = Math.Max(target-1,0);
        speed = 0f;
        if(movesfx!=null)Audio.Stop(movesfx);
        movesfx = Audio.Play("event:/game/05_mirror_temple/swapblock_return", virtLoc);
      }
    }
    if(progress!=target){
      if(!returning) speed = Calc.Approach(speed,maxspeed,maxspeed*Engine.DeltaTime*6);
      else speed = Calc.Approach(speed, -maxreturnspeed, maxreturnspeed*Engine.DeltaTime/1.5f);
      Vector2 old = virtLoc;
      bool done = spos.towardsNextDist(speed*Engine.DeltaTime);
      if(done && returning && target == 0){
        Audio.Stop(movesfx);
        movesfx = null;
      }
      if(done && returning) target = Math.Max(target-1,0);
      if(done && progress ==0){
        target = target%spos.numsegs;
      }
      ownLiftspeed = (virtLoc-old).SafeNormalize()*Math.Abs(speed);
      childRelposSafe();
    } else {
      speed=0;
      Audio.Stop(movesfx);
      movesfx = null;
      ownLiftspeed = Vector2.Zero;
    }
  }
  public void activate(){
    if(movesfx!=null)Audio.Stop(movesfx);
    movesfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", virtLoc);
    returnTimer = 0.8f;
    returning = false;
    if(!returnable)target+=1;
    else target = Math.Min(target+1,spos.numsegs-1);
    speed=Math.Max(Math.Abs(speed),maxspeed/3);
  }
  public override void addTo(Scene scene){
    Spline spline = SplineEntity.GetSpline(dat, SplineEntity.Types.simpleLinear);
    spos = new SplineAccessor(spline, Vector2.Zero);
    Add(new DashListener((Vector2 dir)=>activate()));
    base.addTo(scene);
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    if(movesfx!=null)Audio.Stop(movesfx);
  }
}




using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateRotator")]
public class CirculatingTemplate:TemplateInstanceable, IRemovableContainer{
  class Item{
    public Template te;
    public float offset;
    public ITemplateChild removedChild;
    public bool active;
  }
  List<Item> belt = new();
  EntityData dat;
  ChannelState.FloatCh radius;
  ChannelState.FloatCh[] moveTimes, moveLengths;
  RespawnCountdown[] pauseTimers;
  float[] shakeTimes;
  RespawnCountdown initialDelay;
  Util.Easings easing;
  Util.DerivativeThing radiusDt;
  public CirculatingTemplate(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public CirculatingTemplate(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    dat=d;
    moveTimes = d.ChannelFloatList("moveTimes");
    moveLengths = d.ChannelFloatList("moveDegrees");
    pauseTimers = d.ChannelFloatList("pauseAmounts").Map(x=>new RespawnCountdown(x));
    shakeTimes = Util.csparseflat(d.Attr("shakeTimes",""));
    radius = d.ChannelFloat("radius",0.3f);
    radiusDt = new(radius);
    initialDelay = new(d.ChannelFloat("initialDelay", 0));
    easing = d.Enum("easing",Util.Easings.Linear);
    count = Math.Max(1,dat.Int("count",1));
    Add(new Coroutine(routine()));
  }
  static Vector2 degpos(float p, out Vector2 tangent){
    float c = MathF.Cos(p*MathF.PI/180);
    float s = MathF.Sin(p*MathF.PI/180);
    tangent = new(-s,c);
    return new(c,s);
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    float[] offsets = Util.csparseflat(dat.Attr("offsets",""));
    float offset = 0;
    for(int i=0; i<count; i++){
      offset = i<offsets.Length? offsets[i] : offset+=360f/count;
      Template nte = addInstance(radius*degpos(offset, out var _));
      belt.Add(new(){te=nte, offset=offset});
    }    
  }
  float rotDegBase, rotDeg, rotDt;
  int idx, count;
  float index(float[] li, float def)=>li.Length==0? def:li[idx%li.Length];
  float index(ChannelState.FloatCh[] li, float def)=>li.Length==0? def:li[idx%li.Length];
  public IEnumerator routine(){
    RespawnCountdown delay=initialDelay;
    if(moveTimes.Length>0){
      while(true){
        delay.Begin();
        while(!delay.Prog(Engine.DeltaTime)) yield return null;
        float nextDuration = index(moveTimes, 1f);
        float nextAngle = index(moveLengths, 1);
        float at=0;
        while(at!=1){
          if(nextDuration==0) at=1;
          else at = Util.Approach(at, 1, Engine.DeltaTime/nextDuration);
          float prog = Util.ApplyEasing(easing, at, out float deriv);
          rotDeg = rotDegBase + prog*nextAngle;
          rotDt = nextAngle*deriv/MathF.Max(0.001f,nextDuration);
          yield return null;
        }
        rotDt = 0;
        rotDegBase = Util.SafeMod(rotDegBase+nextAngle,360);
        if(rotDegBase<1 || rotDegBase>359) rotDegBase = 0;
        shake(index(shakeTimes,0));
        if(pauseTimers.Length==0) delay = new(new("0"));
        else delay = pauseTimers[idx%pauseTimers.Length];
        idx++;
      }
    } else {
      delay.Begin();
      while(!delay.Prog(Engine.DeltaTime)) yield return null;
      while(true){
        rotDt = index(moveLengths,180);
        rotDeg = Util.SafeMod(rotDeg + rotDt*Engine.DeltaTime, 360);
        yield return null;
      }
    }
  }
  public override void Update() {
    base.Update();
    radiusDt.prog(Engine.DeltaTime);
    foreach(var desc in belt){
      var radial = degpos(desc.offset+rotDeg, out var tangent);
      desc.te.toffset = radial*radiusDt.Value;
      desc.te.ownLiftspeed = radial*radiusDt.Derivative + tangent*radiusDt.Value*rotDt*MathF.PI/180;
    }
    childRelposSafe();
  }
}
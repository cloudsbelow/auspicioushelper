

using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public interface IRelocateTemplates{
  public interface IDontRelocate{}
  Vector2 GetLiftspeed();
  static Template FindRelocatable(Level l, Vector2 loc){
    List<Template> candidates = new();
    foreach(var e in l.Entities) if(e is Template t and not IDontRelocate && t.relocator == null){
      if(t.parent==null || (t is TemplateMoveCollidable td && td.detatched)) candidates.Add(t);
    }
    return candidates.Minimize(x=>(x.Position-loc).LengthSquared());
  }
}

[CustomEntity("auspicioushelper/channelTemplateMover")]
public class TemplateMover:Entity{
  Util.Easings easing;
  Vector2 moveOffset;
  float shakeTime;
  float arrivalShake;
  float speed;
  bool relative;
  bool log;
  string channel;
  public TemplateMover(EntityData d, Vector2 o):base(d.Position+o){
    moveOffset = d.Nodes[0]-d.Position;
    shakeTime = d.Float("shakeTime",0);
    arrivalShake = d.Float("arrivalShake",0);
    speed = d.Float("moveTime",1)==0? -1: 1/d.Float("moveTime",1);
    easing = d.Enum("easing", Util.Easings.Linear);
    relative = d.Bool("relative",true);
    log = d.Bool("log",false);
    channel = d.Attr("activateChannel", "");
  }
  public class RelocateRoutine:Coroutine, IRelocateTemplates{
    float at = 0;
    TemplateMover m;
    Vector2 correction = Vector2.Zero;
    Vector2 IRelocateTemplates.GetLiftspeed() {
      Util.ApplyEasing(m.easing,at, out var derivative);
      return m.moveOffset*m.speed*derivative + correction;
    }
    void CorrectPosition(Template t, Vector2 to){
      Vector2 oldpos = t.Position;
      t.Position = to;
      correction = (t.Position-oldpos)/Math.Max(Engine.DeltaTime,0.001f);
    }
    IEnumerator moveRoutine(Template t){
      if(m.shakeTime>0){
        t.shake(m.shakeTime);
        if(m.relative) yield return m.shakeTime;
        else {
          float timer = 0;
          while(timer <m.shakeTime){
            timer+=Engine.DeltaTime;
            CorrectPosition(t,m.Position);
            t.childRelposSafe();
            yield return null;
          }
        }
      }
      Vector2 lastOffset = Vector2.Zero;
      while(at<1){
        at = Math.Min(1, at+m.speed*Engine.DeltaTime);
        if(!m.relative){
          CorrectPosition(t,m.Position+lastOffset);
        }
        var eased = Util.ApplyEasing(m.easing,at);
        t.Position += eased*m.moveOffset - lastOffset;
        lastOffset = eased*m.moveOffset;
        t.childRelposSafe();
        yield return null;
      }
      correction = Vector2.Zero;
      if(m.arrivalShake>0) t.shake(m.arrivalShake);
      t.relocator = null;
      yield return null;
      t.Remove(this);
    }
    public RelocateRoutine(Template t, TemplateMover m):base(true){
      this.m=m;
      Active = true;
      enumerators.Push(moveRoutine(t));
    }
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    Add(new ChannelTracker(channel, x=>{
      if(x==0 || Scene is not Level l) return;
      var t = IRelocateTemplates.FindRelocatable(l,Position);
      if(t==null){
        DebugConsole.Write("Could not find movable tempalte");
        return;
      }
      if(log) DebugConsole.Write($"moving template {t} at position {t.Position} with {t.children.Count} children");
      t.Add((RelocateRoutine)(t.relocator = new RelocateRoutine(t,this)));
    }));
  }
}
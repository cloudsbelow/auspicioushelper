



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.mod.auspicioushelper;

[CustomEntity("auspicioushelper/ConveyerTempalte")]
public class ConveyerTemplate:TemplateInstanceable{
  class BeltItem{
    public Template te;
    public SplineAccessor sp;
  }
  Queue<BeltItem> belt = new();  
  bool loop;
  int ntemplates;
  Spline spline;
  EntityData dat;
  float timePerSegment;
  float initialOffset;
  float timer;
  float maxtimer;
  public ConveyerTemplate(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public ConveyerTemplate(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    dat=d;
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();

  }
  public override void addTo(Scene scene) {
    spline = SplineEntity.GetSpline(dat,SplineEntity.Types.centripetalNormalized);
    base.addTo(scene);
  }
  public override void Update() {
    base.Update();
    if(!loop){
      while(belt.Peek().sp.t>=spline.segments-1){
        Template child = belt.Dequeue().te;
        children.Remove(child);
        child.destroy(false);
      }
    }
    foreach(var desc in belt){
      desc.sp.move(Engine.DeltaTime/timePerSegment);
      
    }
    if(timer<=0){
      SplineAccessor spos = new(spline, Vector2.Zero, false, false);
      spos.setPos(-timer/timePerSegment);
      timer+=maxtimer;
      Template nte = addInstance(spos.pos);
      belt.Enqueue(new(){te=nte,sp=spos});
    } else timer-=Engine.DeltaTime;
  }
}
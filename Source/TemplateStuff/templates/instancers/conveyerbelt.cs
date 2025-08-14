



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateBelt")]
public class ConveyerTemplate:TemplateInstanceable{
  class BeltItem{
    public Template te;
    public SplineAccessor sp;
    public float extent;
  }
  Queue<BeltItem> belt = new();  
  bool loop;
  Spline spline;
  EntityData dat;
  float speed;
  float initialOffset;
  float timer;
  float maxtimer;
  public ConveyerTemplate(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public ConveyerTemplate(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    dat=d;
    speed = d.Float("speed",0.3f);
    maxtimer = 1/d.Float("numPerSegment",3)/speed;
    initialOffset = d.Float("offset",0)%maxtimer;
    loop = d.Bool("loop",false);
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    List<BeltItem> l= new();
    for(float i=initialOffset; i<spline.segments-(loop?0:1); i+=speed*maxtimer){
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      spos.set(i);
      Template nte = addInstance(spos.pos);
      l.Add(new(){te=nte, extent = i, sp=spos});
    }
    timer = maxtimer-initialOffset;
    for(int i=l.Count-1; i>=0; i--) belt.Enqueue(l[i]);
  }
  public override void addTo(Scene scene) {
    spline = SplineEntity.GetSpline(dat,SplineEntity.Types.centripetalNormalized);
    if(spline.segments==1 && !dat.Bool("lastNodeIsKnot")) loop = true;
    base.addTo(scene);
  }
  public override void Update() {
    base.Update();
    if(!loop){
      while(belt.Count>0 && belt.Peek().extent>=spline.segments-1){
        Template child = belt.Dequeue().te;
        children.Remove(child);
        child.destroy(false);
      }
    }
    foreach(var desc in belt){
      desc.extent+=Engine.DeltaTime*speed;
      desc.sp.set(desc.extent);
      desc.te.ownLiftspeed = desc.sp.tangent*speed;
      desc.te.toffset = desc.sp.pos;
    }
    childRelposSafe();
    if(!loop && timer<=0){
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      float extent = -timer*speed;
      spos.setPos(extent);
      timer+=maxtimer;
      Template nte = addInstance(spos.pos);
      belt.Enqueue(new(){te=nte,sp=spos,extent = extent});
    } else timer-=Engine.DeltaTime;
  }
}
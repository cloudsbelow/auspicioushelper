



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateBelt")]
public class ConveyerTemplate:TemplateInstanceable, IRemovableContainer{
  class BeltItem{
    public Template te;
    public SplineAccessor sp;
    public ITemplateChild removedChild;
    public float extent;
    public bool active = true;
  }
  Queue<BeltItem> belt = new();  
  bool loop;
  Spline spline;
  EntityData dat;
  float speed;
  float initialOffset;
  float timer;
  float maxtimer;
  string channel;
  Util.Easings easing;
  public ConveyerTemplate(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public ConveyerTemplate(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    dat=d;
    speed = d.Float("speed",0.3f);
    maxtimer = 1/d.Float("numPerSegment",3)/speed;
    initialOffset = (maxtimer*d.Float("initialOffset",0))%maxtimer;
    loop = d.Bool("loop",false);
    channel = d.Attr("channel","");
    easing = d.Enum("easing",Util.Easings.Linear);
    if(!string.IsNullOrWhiteSpace(channel)) timer = 1000000000;
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    if(!string.IsNullOrWhiteSpace(channel)) return;
    List<BeltItem> l= new();
    for(float i=initialOffset*speed; i<spline.segments-(loop?0:1); i+=speed*maxtimer){
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      spos.set(Util.ApplyEasingFrac(easing,i,out var _));
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
    if(!string.IsNullOrWhiteSpace(channel))Add(new ChannelTracker(channel,(double val)=>{
      if(val == 0) return;
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      Template nte = addInstance(spos.pos);
      belt.Enqueue(new(){te=nte, extent = 0, sp=spos});
    }));
  }
  public override void Update() {
    base.Update();
    if(!loop){
      while(belt.Count>0 && belt.Peek().extent>=spline.segments-1){
        var child = belt.Dequeue();
        if(!child.active){
          List<Template> toEmancipate = new();
          foreach(var te in child.te.children) if(te is Template tem) toEmancipate.Add(tem);
          foreach(var te in toEmancipate){
            te.emancipate();
            children.Add(te);
            te.parent = this;
          }
          child.te.destroy(false);
        } else {
          child.te.destroy(false);
        }
      }
    }
    foreach(var desc in belt){
      desc.extent+=Engine.DeltaTime*speed;
      if(!desc.active) continue;
      desc.sp.set(Util.ApplyEasingFrac(easing,desc.extent,out float deriv));
      desc.te.ownLiftspeed = desc.sp.tangent*speed*deriv;
      desc.te.toffset = desc.sp.pos;
    }
    childRelposSafe();
    if(!loop && timer<=0){
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      float extent = -timer*speed;
      spos.setPos(Util.ApplyEasingFrac(easing,extent, out var _));
      timer+=maxtimer;
      Template nte = addInstance(spos.pos);
      belt.Enqueue(new(){te=nte,sp=spos,extent = extent});
    } else timer-=Engine.DeltaTime;
  }
  public void RemoveChild(ITemplateChild c){
    foreach(var desc in belt) if(desc.te == c.parent && desc.te.children.Count==1){
      desc.active=false;
      desc.removedChild = c;
      return;
    }
  }
  public TemplateDisappearer.vcaTracker stat=new();
  public override void parentChangeStat(int vis, int col, int act) {
    base.parentChangeStat(vis, col, act);
    stat.Align(vis,col,act);
  }
  public bool RestoreChild(ITemplateChild c){
    foreach(var desc in belt) if(desc.removedChild==c){
      if(c is Template te) te.emancipate();
      c.parent = desc.te;
      desc.te.children.Add(c);
      desc.active = true;
      desc.sp.setSidedFromDir(desc.extent,1);
      desc.te.ownLiftspeed = desc.sp.tangent*speed;
      desc.te.toffset = desc.sp.pos;
      if(stat.Collidable)c.parentChangeStat(0,-1,0);
      relposOne(desc.te);
      if(stat.Collidable)c.parentChangeStat(0,1,0);
      return true;
    }
    return false;
  }
}




using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
  Util.TwoQueue<BeltItem> belt = new();  
  bool loop;
  Spline spline;
  EntityData dat;
  ChannelState.FloatCh speed, numPerSegment;
  float initialOffset;
  float timer, fliptimer;
  string channel;
  Util.Easings easing;
  public ConveyerTemplate(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public ConveyerTemplate(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    dat=d;
    speed = d.ChannelFloat("speed",0.3f);
    numPerSegment = d.ChannelFloat("numPerSegment",3);
    initialOffset = Util.SafeMod(d.Float("initialOffset",0),1);
    loop = d.Bool("loop",false);
    channel = d.Attr("channel","");
    easing = d.Enum("easing",Util.Easings.Linear);
    if(!string.IsNullOrWhiteSpace(channel)) timer = 1000000000;
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    if(!string.IsNullOrWhiteSpace(channel)) return;
    List<BeltItem> l= new();
    if(numPerSegment==0) return;
    float max = spline.segments-(loop?0:1);
    float step = 1/numPerSegment;
    float ioff = initialOffset*step;
    float numinst = loop? Math.Max(1, MathF.Round((max-ioff)/step)): MathF.Ceiling((max-ioff)/step);
    
    for(int j=(int)numinst-1; j>=0; j--){
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      spos.set(Util.ApplyEasingFrac(easing,j*step+ioff,out var _));
      Template nte = addInstance(spos.pos);
      belt.PushBack(new(){te=nte, extent = j*step+ioff, sp=spos});
    }
    timer = 1-initialOffset;
    fliptimer = 1-(max-(numinst-1+initialOffset)*step)/step;
  }
  public override void addTo(Scene scene) {
    spline = SplineEntity.GetSpline(dat,SplineEntity.Types.centripetalNormalized);
    if(spline.segments==1 && !dat.Bool("lastNodeIsKnot")) loop = true;
    base.addTo(scene);
    if(!string.IsNullOrWhiteSpace(channel))Add(new ChannelTracker(channel,(double val)=>{
      if(val == 0) return;
      SplineAccessor spos = new(spline, Vector2.Zero, true, loop);
      Template nte = addInstance(spos.pos);
      belt.PushBack(new(){te=nte, extent = 0, sp=spos});
    }));
  }
  public override void Update() {
    base.Update();
    
    if(!loop){
      if(speed>0) while(belt.Count>0 && belt.Front().extent>=spline.segments-1){
        var child = belt.PopFront();
        fliptimer = numPerSegment==0? 0 : (child.extent-(spline.segments-1))*numPerSegment;
        if(!child.active) children.Add(child.te);
        else child.te.destroy(false);
      }
      if(speed<0) while(belt.Count>0 && belt.Back().extent<=0){
        var child = belt.PopBack();
        timer = numPerSegment==0? 0 : -child.extent*numPerSegment;
        if(!child.active) children.Add(child.te);
        else child.te.destroy(false);
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

    
    if(loop) return;
    timer-=Engine.DeltaTime*speed*numPerSegment;
    fliptimer+=Engine.DeltaTime*speed*numPerSegment;
    if(timer<=0){
      if(numPerSegment==0) timer=0;
      else {
        SplineAccessor spos = new(spline, Vector2.Zero, true);
        float extent = -timer/numPerSegment;
        spos.setPos(Util.ApplyEasingFrac(easing,extent, out var _));
        timer+=1;
        Template nte = addInstance(spos.pos);
        belt.PushBack(new(){te=nte, sp=spos, extent=extent});
      }
    } 
    if(fliptimer<=0){
      if(numPerSegment==0) fliptimer=0;
      else {
        SplineAccessor spos = new(spline, Vector2.Zero, true);
        float extent = spline.segments-1 + fliptimer/numPerSegment;
        spos.setPos(Util.ApplyEasingFrac(easing, extent, out var _));
        fliptimer+=1;
        Template nte = addInstance(spos.pos);
        belt.PushFront(new(){te=nte, sp=spos, extent=extent});
      }
    }
  }
  public void RemoveChild(ITemplateChild c){
    ITemplateChild f=c;
    while(f.parent!=this){
      if(f.parent==null || f.parent.children.Count>1) return;
      f=f.parent;
    }
    foreach(var desc in belt) if(desc.te == f){
      desc.active=false;
      desc.removedChild = c;
      desc.te.ownLiftspeed = Vector2.Zero;
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
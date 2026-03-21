

using System;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/TemplateGluable")]
[MapenterEv(nameof(Search))]
public class TemplateGluable:Template, IRelocateTemplates.IDontRelocate{
  string lookingFor;
  Entity gluedto;
  enum Constraint{
    None, OnlyX, OnlyY, spline
  }
  Constraint constraint;
  float maxspeed;
  public override Vector2 gatheredLiftspeed => constraint==Constraint.None?ownLiftspeed:base.gatheredLiftspeed;
  LiftspeedSm.LiftspeedHelper ls = new();
  Vector2 offset = Vector2.Zero;
  protected override Vector2 virtLoc=>Position+offset;
  float usedSpd=0;
  public override void relposTo(Vector2 loc, Vector2 parentLiftspeed) {
    if(constraint!=Constraint.None){
      DealWithMovement(false);
      base.relposTo(loc, parentLiftspeed);
    }
  }
  SplineAccessor spos;
  EntityData d;
  public TemplateGluable(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateGluable(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    lookingFor = d.Attr("glue_to_identifier");
    if(d.tryGetStr("constraint", out var s)){
      if(!Enum.TryParse(s,out constraint)){
        constraint = Constraint.spline;
      }
    } else constraint = Constraint.None;
    if(d.Bool("onlyX",false)) constraint = Constraint.OnlyX;
    if(d.Bool("onlyY",false)) constraint = Constraint.OnlyY;
    maxspeed = d.Float("maxSpeed",float.PositiveInfinity);
    this.d=d;
  }
  static Regex matchReg = new(@"^\s*\d+\s*(?:[\/,]\s*\d+\s*)*$",RegexOptions.Compiled);
  static void Search(EntityData d){
    string str = d.Attr("glue_to_identifier");
    if(matchReg.Match(str).Success) Finder.enqueueIdent(str);
  }
  bool added = false;
  public void make(Entity e){
    Depth = e.Depth-1;
    offset = constraint switch {
      Constraint.OnlyX=>new Vector2(e.X-Position.X,0),
      Constraint.OnlyY=>new Vector2(0,e.Y-Position.Y),
      Constraint.spline=>spos.setToClosest(e.Position-Position),
      _=>Vector2.Zero
    };
    if(constraint == Constraint.None) Position = e.Position;
    added=true;
    remake();
  }
  public override void addTo(Scene scene){
    setTemplate(scene:scene);
    scene.Add(this);
    if(constraint==Constraint.spline){
      spos = new(SplineEntity.GetSpline(d, d.Enum("constraint",SplineEntity.Types.simpleLinear)), Vector2.Zero, true);
    }
  }
  public override void Awake(Scene scene){
    base.Awake(scene);
    if(!added){
      gluedto = FoundEntity.find(lookingFor)?.Entity;
      DebugConsole.Write("found",gluedto);
      if(gluedto != null && gluedto.Scene!=null) make(gluedto);
    }
    
  }
  float clampBudget(float x, float dt)=>Math.Clamp(x+usedSpd, -dt*maxspeed, dt*maxspeed)-usedSpd;
  void DealWithMovement(bool doRelpos=true){
    float dt = Engine.DeltaTime;
    float safedt = dt==0?0.001f:dt;
    float delta;
    switch(constraint){
      case Constraint.spline:
        Vector2 targ = gluedto.Position-Position;
        float sign = Vector2.Dot(targ-spos.pos,spos.tangent);
        if(Math.Abs(sign)/spos.tangent.Length()<1) break;
        float allow = sign>0?maxspeed-usedSpd:maxspeed+usedSpd;
        float mov = spos.approach(targ, 0, spos.numsegs-1, allow*dt, out float sdist);
        usedSpd+=sdist;
        
        if(mov!=0){
          ls.AddSpeed(spos.tangent*mov/safedt);
          offset = spos.pos;
          //return;
          goto relpos;
        }
      break; case Constraint.OnlyX:
        delta = clampBudget(gluedto.Position.X-virtLoc.X,dt);
        if(delta!=0){
          ls.AddSpeed(Vector2.UnitX*delta/safedt);
          offset.X += delta;
          goto relpos;
        }
      break; case Constraint.OnlyY:
        delta = clampBudget(gluedto.Position.Y-virtLoc.Y,dt);
        if(delta!=0){
          ls.AddSpeed(Vector2.UnitY*delta/safedt);
          offset.Y += delta;
          goto relpos;
        }
      break; default:
        delta = clampBudget((gluedto.Position-Position).Length(),dt);
        if(delta!=0){
          Vector2 move = (gluedto.Position-Position).SafeNormalize(delta);
          ls.AddSpeed(move/safedt);
          Position+=move;
          goto relpos;
        }
      break;
    }
    return;
    relpos:
      ownLiftspeed = ls.getLiftspeedSmear(4);
      // DebugConsole.Write("Relposing",ownLiftspeed,spos.t);
      if(doRelpos) childRelposSafe();
  }
  public override void Update(){
    base.Update();
    if(!added){
      gluedto = FoundEntity.find(lookingFor)?.Entity;
      if(gluedto != null && gluedto.Scene!=null) make(gluedto);
      return;
    }
    if(gluedto.Scene == null){
      gluedto = null;
      destroyChildren(true);
      added=false;
      return;
    } 
    usedSpd = 0;
    ls.Update();
    DealWithMovement(true);
  }
}
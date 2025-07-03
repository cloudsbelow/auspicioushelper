


using System;
using Celeste.Mod.Entities;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateMoonblock")]
public class TemplateMoonblock:Template{
  float floatfreq;
  float floatamp;
  float sinkamount;
  float sinkSpeed;
  float dashmagn;
  Vector2 offset;
  public override Vector2 virtLoc => Position+offset;
  float sinkTimer = 0;
  float ylerp = 0;
  float sinephase =0;
  float dashease =0;
  Vector2 dashdir;
  public TemplateMoonblock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateMoonblock(EntityData d, Vector2 offset, int depthoffset):base(d,d.Position+offset,depthoffset){
    floatfreq = d.Float("drift_frequency",1f);
    floatamp = d.Float("drift_amplitude",4);
    sinkamount = d.Float("sink_amount",12);
    sinkSpeed = d.Float("sink_speed",1f);
    dashmagn = d.Float("dash_influence",8);
    sinephase = d.Bool("useCustomStartphase",false)?d.Float("startphase",0): Calc.Random.NextFloat(MathF.PI*2);
    OnDashCollide = OnDash;
    offset = Vector2.UnitY*floatamp*MathF.Sin(sinephase*floatfreq);
  }
  DashCollisionResults OnDash(Player p, Vector2 ddir){
    if(dashease<0.2f){
      dashease = 1;
      dashdir = ddir;
    }
    return DashCollisionResults.NormalOverride;
  } 
  public override void Update() {
    base.Update();
    if(hasRiders<Player>()){
      sinkTimer = 0.3f;
    } else {
      if(sinkTimer>0)sinkTimer -= Engine.DeltaTime;
    }
    ylerp = Util.Approach(ylerp,sinkTimer>0?1:0,Engine.DeltaTime*sinkSpeed, out var sign);
    sinephase += Engine.DeltaTime;
    dashease = Calc.Approach(dashease, 0f, Engine.DeltaTime * 1.5f);

    ownLiftspeed = Vector2.Zero;
    offset = Vector2.Zero;
    offset += Vector2.UnitY*floatamp*MathF.Sin(sinephase*floatfreq);
    ownLiftspeed += Vector2.UnitY*floatamp*floatfreq*MathF.Cos(sinephase*floatfreq);
    offset += Util.SineInOut(ylerp, out var dyl)*Vector2.UnitY*sinkamount;
    ownLiftspeed += Vector2.UnitY*sinkSpeed*sign*dyl*sinkamount;
    offset += dashdir*dashmagn*Util.Spike(Util.QuadIn(dashease,out var d2), out var d1);
    ownLiftspeed += dashdir*dashmagn*d1*d2;
    childRelposSafe();
  }
}
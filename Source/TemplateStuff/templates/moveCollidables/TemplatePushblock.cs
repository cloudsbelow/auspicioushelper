



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplatePushBlock")]
public class TemplatePushblock:TemplateMoveCollidable{
  Vector2 speed;
  float terminalVelocity = 130f;
  float gravity = 500;
  int leniency = 4;
  float reflectX = 0.5f;
  float drag = 300;
  float nophysicstime = 0;
  float giveNoPhysics = 0.3f;
  float dashSpeed = 100;
  float ownSpringRecoil = 60;
  string ImpactSfx = "event:/game/general/fallblock_impact";
  bool hitSprings = true;
  bool startDisconnected = true;
  public TemplatePushblock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplatePushblock(EntityData d, Vector2 pos, int depthoffset):base(d,pos+d.Position,depthoffset){
    OnDashCollide = (Player p, Vector2 dir)=>{
      if(!detatched) disconnect();
      if(dir.X != 0 && (speed.X*dir.X<0 || Math.Abs(dir.X)*dashSpeed>Math.Abs(speed.X))){
        speed.X=dir.X*dashSpeed; nophysicstime = giveNoPhysics;
      }
      if(dir.Y !=0) nophysicstime = giveNoPhysics;
      if(dir.Y == -1) speed.Y = -dashSpeed;
      if(dir.Y == 1){
        speed.Y = Math.Max(dashSpeed,speed.Y);
        return DashCollisionResults.NormalOverride;
      }
      return DashCollisionResults.Rebound;
    };
    giveNoPhysics = d.Float("NoPhysicsTime", 0.3f);
    dashSpeed = d.Float("speedFromDash",100);
    drag = d.Float("horizontalDrag",300);
    leniency = d.Int("movementLeniency",4);
    ImpactSfx = d.Attr("ImpactSfx","event:/game/general/fallblock_impact");
    startDisconnected = d.Bool("startDisconnected",true);
    hitSprings = d.Bool("hitSprings", true);
    terminalVelocity = d.Float("terminalVelocity", 130f);
    gravity = d.Float("gravity", 500f);
    reflectX = d.Float("BounceStrengthFromWall",0.4f);
    ownSpringRecoil = d.Float("ownSpringRecoil", 60);
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(startDisconnected)disconnect();
  }
  public override void Update() {
    if(detatched){
      var q = getq((speed*Engine.DeltaTime).Abs()+Vector2.UnitY);
      bool grounded = speed.Y>=0 && TestMoveLeniency(q.q,q.s, 1, Vector2.UnitY, leniency, Vector2.UnitX)==Vector2.Zero;
      if(!grounded || speed.Y>terminalVelocity){
        if(nophysicstime<=0)speed.Y = Calc.Approach(speed.Y,terminalVelocity,gravity*Engine.DeltaTime);
      }
      if(nophysicstime<=0) speed.X = Calc.Approach(speed.X,0,Engine.DeltaTime*drag*(grounded?2:1));
      else nophysicstime-=Engine.DeltaTime;
      if(speed!=Vector2.Zero){
        if(MoveHCollide(q, speed.X*Engine.DeltaTime, leniency, speed)){
          speed.X = -speed.X*reflectX;
        }
        if(MoveVCollide(q, speed.Y*Engine.DeltaTime, leniency, speed)){
          speed.Y=0;
          Audio.Play(ImpactSfx,Position);
        }
      }
      if(hitSprings){
        QueryIn qe = getQself();
        HashSet<Spring> exclude = new(GetChildren<Spring>());
        foreach(SpringTracker s in Scene.Tracker.GetComponents<SpringTracker>()){
          if(!exclude.Contains(s.Spring) && qe.Collide(new FloatRect(s.Entity))){
            s.Spring.BounceAnimate();
            if(s.Spring.Orientation == Spring.Orientations.Floor){
              speed.Y = -160f; nophysicstime = 0.25f;
            }
            if(s.Spring.Orientation == Spring.Orientations.WallLeft){
              speed.X = 160; speed.Y=-40; nophysicstime=0.25f;
            }
            if(s.Spring.Orientation == Spring.Orientations.WallRight){
              speed.X = -160; speed.Y=-40; nophysicstime=0.25f;
            }
          }
        }
      }
    }
  }
  public override void OnTrigger(StaticMover sm) {
    base.OnTrigger(sm);
    if(ownSpringRecoil == 0) return;
    Entity e = EntityParser.SmInfo.getInfo(sm).entity;
    if(e is Spring s){
      if(s.Orientation == Spring.Orientations.Floor) speed.Y=Math.Min(speed.Y+ownSpringRecoil,terminalVelocity);
      if(s.Orientation == Spring.Orientations.WallLeft) speed.X-=ownSpringRecoil;
      if(s.Orientation == Spring.Orientations.WallRight) speed.X+=ownSpringRecoil;
      nophysicstime = giveNoPhysics;
    }
  }
}




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
  ChannelState.FloatCh terminalVelocity, gravity, drag, floordragraw;
  float floordrag=>floordragraw==null? drag*2:floordragraw;
  int leniency = 4;
  float reflectX = 0.5f;
  float nophysicstime = 0;
  float giveNoPhysicsDash = 0.3f;
  float giveNoPhysicsStill = 0.2f;
  float giveNoPhysicsSpring = 0.25f;
  float giveNoPhysicsOwnSpring = 0.3f;
  ChannelState.FloatCh dashSpeed;
  float ownSpringRecoil = 60;
  string ImpactSfx = "event:/game/general/fallblock_impact";
  bool hitSprings = true;
  bool startDisconnected = true;
  bool alwaysDrag;
  public TemplatePushblock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplatePushblock(EntityData d, Vector2 pos, int depthoffset):base(d,pos+d.Position,depthoffset){
    OnDashCollide = (Player p, Vector2 dir)=>{
      if(!detatched) disconnect();
      if(dir.X != 0 && (speed.X*dir.X<0 || Math.Abs(dir.X)*dashSpeed>Math.Abs(speed.X))){
        speed.X=dir.X*dashSpeed; nophysicstime = giveNoPhysicsDash;
      }
      if(dir.Y !=0) nophysicstime = giveNoPhysicsDash;
      if(dir.Y == -1) speed.Y = -dashSpeed;
      if(dir.Y == 1){
        if(!grounded)speed.Y = Math.Max(dashSpeed,speed.Y);
        return DashCollisionResults.NormalOverride;
      }
      return DashCollisionResults.Rebound;
    };
    var npt = Util.csparseflat(d.Attr("NoPhysicsTime", "0.3"));
    if(npt.Length>0) giveNoPhysicsDash = npt[0];
    if(npt.Length>1) giveNoPhysicsStill = npt[1];
    if(npt.Length>2) giveNoPhysicsSpring = npt[2];
    if(npt.Length>3) giveNoPhysicsOwnSpring = npt[3];
    dashSpeed = d.ChannelFloat("speedFromDash",100);
    var drags = Util.listparseflat(d.Attr("horizontalDrag")).Map(x=>new ChannelState.FloatCh(x)).ToArray();
    drag = drags.Length==0? new("300"):drags[0];
    floordragraw = drags.Length>1? drags[1]:null;
    ImpactSfx = d.Attr("ImpactSfx","event:/game/general/fallblock_impact");
    startDisconnected = d.Bool("startDisconnected",true);
    hitSprings = d.Bool("hitSprings", true);
    terminalVelocity = d.ChannelFloat("terminalVelocity", 130f);
    gravity = d.ChannelFloat("gravity", 500f);
    reflectX = d.Float("BounceStrengthFromWall",0.4f);
    ownSpringRecoil = d.Float("ownSpringRecoil", 60);
    alwaysDrag = d.Bool("alwaysDrag",false);
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(startDisconnected)disconnect();
  }
  bool grounded;
  public override void Update() {
    base.Update();
    if(detatched){
      var q = getq((speed*Engine.DeltaTime).Abs()+Vector2.UnitY);
      grounded = speed.Y==0 && TestMoveLeniency(q.q,q.s, 1, Vector2.UnitY, 0, Vector2.UnitX)==Vector2.Zero;
      if(grounded && speed==Vector2.Zero) nophysicstime = giveNoPhysicsStill;
      if(!grounded || speed.Y>terminalVelocity){
        if(nophysicstime<=0)speed.Y = Calc.Approach(speed.Y,terminalVelocity,gravity*Engine.DeltaTime);
      }
      if(nophysicstime<=0 || alwaysDrag) speed.X = Calc.Approach(speed.X,0,Engine.DeltaTime*(grounded?floordrag:drag));
      if(nophysicstime>0) nophysicstime-=Engine.DeltaTime;
      ownLiftspeed = speed;
      if(speed!=Vector2.Zero){
        if(MoveHCollide(q, speed.X*Engine.DeltaTime, leniency)){
          speed.X = -speed.X*reflectX;
        }
        if(MoveVCollide(q, speed.Y*Engine.DeltaTime, leniency)){
          speed.Y=0;
          Audio.Play(ImpactSfx,Position);
        }
      }
      if(hitSprings){
        QueryIn qe = getQself();
        HashSet<Spring> exclude = new(GetChildren<Spring>());
        foreach(SpringTracker s in Scene.Tracker.GetComponents<SpringTracker>()){
          if(!exclude.Contains(s.Spring) && qe.Collide(s.Entity)){
            s.Spring.BounceAnimate();
            Vector2 m = SpringTracker.Multiplier(s.Spring);
            speed = SpringTracker.GetDir(s.Spring) switch {
              SpringTracker.Dir.Right=>new Vector2(160,-40)*m,
              SpringTracker.Dir.Left=>new Vector2(-160,-40)*m,
              SpringTracker.Dir.Down=>new(speed.X*0.6f,160*m.Y),
              SpringTracker.Dir.Up=>new(speed.X,-160*m.Y),
              _=>speed,
            };
            nophysicstime = giveNoPhysicsSpring;
          }
        }
      }
    }
  }
  public override void OnTrigger(TriggerInfo info) {
    if(!TriggerInfo.TestPass(info,this)) return;
    base.OnTrigger(info);
    if(!detatched) disconnect();
    if(ownSpringRecoil == 0) return;
    Entity e = info?.entity;
    if(e is Spring s){
      if(s.Orientation == Spring.Orientations.Floor) speed.Y=Math.Min(speed.Y+ownSpringRecoil,terminalVelocity);
      if(s.Orientation == Spring.Orientations.WallLeft) speed.X-=ownSpringRecoil;
      if(s.Orientation == Spring.Orientations.WallRight) speed.X+=ownSpringRecoil;
      nophysicstime = giveNoPhysicsOwnSpring;
    }
  }
}
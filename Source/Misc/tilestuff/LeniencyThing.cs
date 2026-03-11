



using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/leniencything")]
public class PixelLeniencyTrigger:Trigger{
  public struct Ruleset{
    public int staticSlip;
    public int fallingSlip;
    public int maxGroundedStep;
    public float maxStepSlope;
    public float fallDepth;
    public int snapDown;
    public bool forceSlip;
    public override string ToString() {
      return $"staticSlip={staticSlip}, fallingSlip={fallingSlip}, falldepth={fallDepth}, maxGroundedStep={maxGroundedStep}, maxStepSlope={maxStepSlope}";
    }
  }
  static Ruleset defaultRules=>new(){staticSlip=0, fallingSlip=0, maxGroundedStep=0, maxStepSlope=0, fallDepth=3, snapDown=0, forceSlip=false};
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnReset)]
  static void SetRules(){
    curRules = defaultRules;
    overRules.Clear();
    FixRules();
    inNormalupdate=false; //paranoia
  }
  [Import.SpeedrunToolIop.Static]
  static Ruleset curRules = defaultRules;
  [Import.SpeedrunToolIop.Static]
  static LinkedList<Ruleset> overRules = new();
  [Import.SpeedrunToolIop.Static]
  static Ruleset appliedRules = defaultRules;
  public Ruleset rules;
  bool temp;
  bool setOnAwake;
  public PixelLeniencyTrigger(EntityData d, Vector2 o):base(d,o){
    rules.staticSlip = Math.Clamp(d.Int("staticSlip",1),0,4);
    rules.fallingSlip = Math.Clamp(d.Int("fallingSlip",1),0,6);
    rules.fallDepth = Math.Clamp(d.Int("neededFallDist",2),1,8);
    rules.maxGroundedStep = Math.Clamp(d.Int("maxGroundedStep",2),0,8);
    rules.maxStepSlope = Math.Clamp(d.Float("maxStepSlope",1),0.25f,4);
    rules.snapDown = Math.Clamp(d.Int("snapDownAmount",1),0,3);
    rules.forceSlip = d.Bool("alwaysForceSlip",true);
    temp = d.Bool("onlyWhenInside");
    setOnAwake = d.Bool("setOnAwake");
    ResetEvents.LazyEnable(typeof(PixelLeniencyTrigger));
  }
  LinkedListNode<Ruleset> ownNode;
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(setOnAwake){
      curRules = rules;
      FixRules();
    }
  }
  public static void FixRules(){
    appliedRules = overRules.Count>0?overRules.First.Value:curRules;
  }
  public override void OnEnter(Player player) {
    base.OnEnter(player);
    if(temp) ownNode??=overRules.AddFirst(rules);
    else curRules = rules;
    FixRules();
  }
  public override void OnLeave(Player player) {
    base.OnLeave(player);
    if(temp) overRules.Remove(ownNode);
    ownNode = null;
    FixRules();
  }
  static bool inNormalupdate = false;
  [ResetEvents.OnHook(typeof(Player),nameof(Player.OnCollideH))]
  static void Hook(On.Celeste.Player.orig_OnCollideH orig, Player p, CollisionData c){
    int maxStep = appliedRules.maxGroundedStep;
    if(p.onGround && p.StateMachine.State==Player.StNormal && maxStep>0){
      for(int i=0; i<=maxStep; i++){
        Vector2 v = p.Position-i*Vector2.UnitY+Math.Sign(c.Direction.X)*Vector2.UnitX;
        Vector2 vc = p.Position-i*Vector2.UnitY+i*Math.Sign(c.Direction.X)*Vector2.UnitX/appliedRules.maxStepSlope;
        if(!p.CollideCheck<Solid>(v) && !p.CollideCheck<Solid>(vc)){
          p.MoveVExact(-i);
          p.MoveHExact(Math.Sign(c.Direction.X));
          return;
        }
      } 
    }
    orig(p,c);
  }
  static bool SolidAt(Player p,Vector2 at)=>p.CollideCheck<Solid>(at) || p.CollideCheckOutside<JumpThru>(at);
  [ResetEvents.OnHook(typeof(Player),nameof(Player.MoveHExact))]
  static bool Hook(On.Celeste.Actor.orig_MoveHExact orig, Actor a, int m, Collision c, Solid pusher){
    bool o = orig(a,m,c,pusher);
    if(!inNormalupdate || a is not Player p || !p.onGround || appliedRules.snapDown==0) return o;
    if(SolidAt(p,p.Position+Vector2.UnitY)) return o;
    for(int i=1; i<=appliedRules.snapDown; i++){
      if(SolidAt(p,p.Position+Vector2.UnitY*(i+1))){
        p.MoveVExact(i);
        break;
      }
    }
    return o;
  }
  static bool TrySlip(Player p, int threshold){
    bool positive = p.CollideCheck<Solid>(p.Position+Vector2.UnitX*threshold);
    bool negative = p.CollideCheck<Solid>(p.Position-Vector2.UnitX*threshold);
    for(int i=1; i<=threshold; i++){
      for(int j=1; j>=-1; j-=2){
        if(!(j>0?negative:positive)) continue;
        if(j*Math.Sign(p.moveX)<0 && !appliedRules.forceSlip) continue;
        Vector2 v = p.Position+Vector2.UnitY*appliedRules.fallDepth+Vector2.UnitX*i*j;
        Vector2 v2 = p.Position-Vector2.UnitX*(threshold-i+1)*j;
        if(!p.CollideCheck<Solid>(v) && p.CollideCheck<Solid>(v2)){
          p.MoveHExact(i*j);
          p.MoveVExact(1);
          return true;
        }
      }
    }
    return false;
  }
  const float slopeFallspeed=90;
  [ResetEvents.OnHook(typeof(Player),nameof(Player.OnCollideV))]
  static void Hook(On.Celeste.Player.orig_OnCollideV orig, Player p, CollisionData c){
    bool canSlip = p.Speed.Y>0 && (p.StateMachine.State!=Player.StClimb || Input.MoveY.Value==1);
    if(canSlip && SolidAt(p,p.Position+Vector2.UnitY) && TrySlip(p,appliedRules.fallingSlip)){
      if(p.StateMachine.State==Player.StNormal && p.Speed.Y>slopeFallspeed) p.Speed.Y=Math.Min(p.Speed.Y-20,slopeFallspeed);
      return;
    }
    orig(p,c);
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.orig_Update))]
  static void Hook(Action<Player> orig, Player p){
    if(p.StateMachine.State==Player.StNormal && p.Speed.Y>=0 && SolidAt(p,p.Position+Vector2.UnitY)){
      if(TrySlip(p, appliedRules.staticSlip) && p.Speed.Y>slopeFallspeed) p.Speed.Y=Math.Min(p.Speed.Y-20,slopeFallspeed);
    }
    using(Util.WithRestore(ref inNormalupdate, true)) orig(p);
  }
  // [ResetEvents.OnHook(typeof(Player),nameof(Actor.MoveVExact))]
  // static bool Hook(On.Celeste.Actor.orig_MoveVExact orig, Actor a, int m, Collision c, Solid pusher){
  //   bool v = orig(a,m,c,pusher);
  //   if(v && a is Player p && p.StateMachine.State == Player.StClimb && m>0 && TrySlip(p,appliedRules.staticSlip)) return false;
  //   return v;
  // }
}
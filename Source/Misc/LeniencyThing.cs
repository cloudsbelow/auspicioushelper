



using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/leniencything")]
public class PixelLeniencyTrigger:Trigger{
  public struct Ruleset{
    public int staticSlip;
    public int fallingSlip;
    public int maxGroundedStep;
    public float maxStepSlope;
    public float fallDepth;
    public override string ToString() {
      return $"staticSlip={staticSlip}, fallingSlip={fallingSlip}, falldepth={fallDepth}, maxGroundedStep={maxGroundedStep}, maxStepSlope={maxStepSlope}";
    }
  }
  static Ruleset defaultRules=>new(){staticSlip=0, fallingSlip=0, maxGroundedStep=0, maxStepSlope=0, fallDepth=3};
  //static Ruleset regularRules=>new(){staticSlip=0, fallingSlip=0, maxGroundedStep=0, maxStepSlope=0};
  static Ruleset curRules = defaultRules;
  static LinkedList<Ruleset> overRules = new();
  static Ruleset appliedRules = defaultRules;
  public Ruleset rules;
  bool temp;
  public PixelLeniencyTrigger(EntityData d, Vector2 o):base(d,o){
    rules.staticSlip = Math.Clamp(d.Int("staticSlip",1),0,4);
    rules.fallingSlip = Math.Clamp(d.Int("fallingSlip",1),0,6);
    rules.fallDepth = Math.Clamp(d.Int("neededFallDist",3),1,8);
    rules.maxGroundedStep = Math.Clamp(d.Int("maxGroundedStep",2),0,8);
    rules.maxStepSlope = Math.Clamp(d.Float("maxStepSlope",1),0.25f,4);
    temp = d.Bool("onlyWhenInside");
    if(d.Bool("setOnAwake")){
      curRules = rules;
    }
    hooks.enable();
  }
  LinkedListNode<Ruleset> ownNode;
  public static void FixRules(){
    appliedRules = overRules.Count>0?overRules.First.Value:curRules;
    DebugConsole.Write("Rules:",appliedRules);
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
  static void Hook(On.Celeste.Player.orig_OnCollideH orig, Player p, CollisionData c){
    orig(p,c);
    int maxStep = appliedRules.maxGroundedStep;
    if(p.onGround && p.StateMachine.State==Player.StNormal && maxStep>0){
      for(int i=0; i<=maxStep; i++){
        Vector2 v = p.Position-i*Vector2.UnitY+Math.Sign(c.Direction.X)*Vector2.UnitX;
        Vector2 vc = p.Position-i*Vector2.UnitY+i*Math.Sign(c.Direction.X)*Vector2.UnitX/appliedRules.maxStepSlope;
        if(!p.CollideCheck<Solid>(v) && !p.CollideCheck<Solid>(vc)){
          p.MoveVExact(-i);
          p.MoveHExact(Math.Sign(c.Direction.X));
        }
      }
    }
  }
  static void Hook(On.Celeste.Player.orig_OnCollideV orig, Player p, CollisionData c){
    orig(p,c);
    if(p.Speed.Y<=0) return;
    int threshold = appliedRules.fallingSlip;
    if(!p.CollideCheck<Solid>(p.Position+Vector2.UnitY)) return;
    bool positive = p.CollideCheck<Solid>(p.Position+Vector2.UnitX*threshold);
    bool negative = p.CollideCheck<Solid>(p.Position-Vector2.UnitX*threshold);
    for(int i=1; i<=threshold; i++){
      for(int j=1; j>=-1; j-=2){
        if(j*Math.Sign(p.moveX)<0) continue;
        if(!(j>0?negative:positive)) continue;
        Vector2 v = p.Position+Vector2.UnitY*appliedRules.fallDepth+Vector2.UnitX*i*j;
        Vector2 v2 = p.Position-Vector2.UnitX*(threshold-i+1)*j;
        if(!p.CollideCheck<Solid>(v) && p.CollideCheck<Solid>(v2)){
          p.MoveHExact(i*j);
          p.MoveVExact(1);
        }
      }
    }
  }
  static int Hook(On.Celeste.Player.orig_NormalUpdate orig, Player p){
    int val = orig(p);
    if(p.onGround){
      int threshold = appliedRules.staticSlip;
      bool positive = p.CollideCheck<Solid>(p.Position+Vector2.UnitX*threshold);
      bool negative = p.CollideCheck<Solid>(p.Position-Vector2.UnitX*threshold);
      for(int i=1; i<=threshold; i++){
        for(int j=1; j>=-1; j-=2){
          if(j*Math.Sign(p.moveX)<0) continue;
          if(!(j>0?negative:positive)) continue;
          Vector2 v = p.Position+Vector2.UnitY*appliedRules.fallDepth+Vector2.UnitX*i*j;
          Vector2 v2 = p.Position-Vector2.UnitX*(threshold-i+1)*j;
          if(!p.CollideCheck<Solid>(v) && p.CollideCheck<Solid>(v2)){
            p.MoveHExact(i*j);
            p.MoveVExact(1);
          }
        }
      }
    }
    return val;
  }

  public static HookManager hooks = new(()=>{
    On.Celeste.Player.OnCollideH+=Hook;
    On.Celeste.Player.OnCollideV+=Hook;
    On.Celeste.Player.NormalUpdate+=Hook;
  },()=>{
    curRules = defaultRules;
    overRules.Clear();
    FixRules();
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    On.Celeste.Player.NormalUpdate-=Hook;
  },auspicioushelperModule.OnEnterMap);
}
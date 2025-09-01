



using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Import;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public class PixelLeniencyTrigger:Trigger{
  public struct Ruleset{
    public int staticSlip;
    public int fallingSlip;
    public int maxGroundedStep;
    public float maxStepSlope;
    public override string ToString() {
      return $"staticSlip={staticSlip}, fallingSlip={fallingSlip}, maxGroundedStep={maxGroundedStep}, maxStepSlope={maxStepSlope}";
    }
  }
  static Ruleset partialTileRules=>new(){staticSlip=1, fallingSlip=2, maxGroundedStep=2, maxStepSlope=1};
  //static Ruleset regularRules=>new(){staticSlip=0, fallingSlip=0, maxGroundedStep=0, maxStepSlope=0};
  static Ruleset curRules = partialTileRules;
  static LinkedList<Ruleset> overRules = new();
  static Ruleset appliedRules = partialTileRules;
  public Ruleset rules;
  bool temp;
  public PixelLeniencyTrigger(EntityData d, Vector2 o):base(d,o){
    rules.staticSlip = d.Int("staticSlip",1);
    rules.fallingSlip = d.Int("fallingSlip",1);
    rules.maxGroundedStep = d.Int("maxGroundedStep",2);
    rules.maxStepSlope = d.Float("maxStepSlope",1);
    temp = d.Bool("onlyWhenInside");
    if(d.Bool("configureRoom")){
      curRules = rules;
    }
    hooks.enable();
  }
  LinkedListNode<Ruleset> ownNode;
  public static void FixRules(){
    appliedRules = overRules.Count>0?overRules.First.Value:curRules;
    DebugConsole.Write("Rules:",curRules, appliedRules);
  }
  public override void OnEnter(Player player) {
    base.OnEnter(player);
    if(temp) ownNode = overRules.AddFirst(rules);
    else curRules = rules;
    FixRules();
  }
  public override void OnLeave(Player player) {
    base.OnLeave(player);
    if(temp) overRules.Remove(ownNode);
    FixRules();
  }
  static void Hook(On.Celeste.Player.orig_OnCollideH orig, Player p, CollisionData c){
    orig(p,c);
    int maxStep = appliedRules.maxGroundedStep;
    if(p.onGround && p.StateMachine.State==Player.StNormal && maxStep>0){
      for(int i=0; i<=maxStep; i++){
        Vector2 v = p.Position-i*Vector2.UnitY+Math.Sign(c.Direction.X)*Vector2.UnitX;
        Vector2 vc = p.Position-i*Vector2.UnitY+Math.Sign(c.Direction.X)*Vector2.UnitX/appliedRules.maxStepSlope;
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
    for(int i=1; i<=threshold; i++){
      for(int j=1; j>=-1; j-=2){
        if(j*Math.Sign(p.moveX)<0) continue;
        Vector2 v = p.Position+Vector2.UnitY+Vector2.UnitX*i*j;
        if(!p.CollideCheck<Solid>(v)){
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
      for(int i=1; i<=threshold; i++){
        for(int j=1; j>=-1; j-=2){
          if(j*Math.Sign(p.moveX)<0) continue;
          Vector2 v = p.Position+Vector2.UnitY+Vector2.UnitX*i*j;
          if(!p.CollideCheck<Solid>(v)){
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
    curRules = partialTileRules;
    FixRules();
    overRules.Clear();
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    On.Celeste.Player.NormalUpdate-=Hook;
  },auspicioushelperModule.OnEnterMap);
}
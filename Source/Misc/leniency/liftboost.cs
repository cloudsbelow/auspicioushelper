


using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/LiftspeedTrigger")]
public class LiftspeedThing:Trigger{
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReset)]
  [Import.SpeedrunToolIop.Static]
  static LinkedList<Settings> overRules = new();
  [Import.SpeedrunToolIop.Static]
  static Settings appliedRules = new();
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnReset)]
  static void fix(){
    appliedRules = new();
    overRules.Clear();
  }
  static readonly Settings defaultRules = new();
  struct Settings{
    public bool disable = false;
    public bool disableMulti = false;
    public float gracetimeMultiplier = 1;
    public float speedMultiplier = 1;
    public Settings(){}
    public override string ToString() {
      return $"ls_rules:{{nols:{disable},nomulti:{disableMulti},grace:{gracetimeMultiplier},speed:{speedMultiplier}}}";
    }
  }

  LinkedListNode<Settings> ownNode;
  Settings rules;
  public LiftspeedThing(EntityData d, Vector2 o):base(d,o){
    hooks.enable();
    rules = new(){
      disable = d.Bool("dontGiveLiftspeed",false),
      gracetimeMultiplier = d.Float("gracetimeMultiplier",1),
      speedMultiplier = d.Float("speedMultiplier",1),
      disableMulti = d.Bool("disableMultiboost",false)
    };
    if(d.Bool("coverRoom"))Collider = new Hitbox(2000000000,2000000000,-1000000000,-1000000000);
  }
  public static void FixRules(){
    appliedRules = overRules.Count>0?overRules.First.Value:defaultRules;
    DebugConsole.Write("Rules:",appliedRules);
  }
  public override void OnEnter(Player player) {
    base.OnEnter(player);
    ownNode??=overRules.AddFirst(rules);
    FixRules();
  }
  public override void OnLeave(Player player) {
    base.OnLeave(player);
    overRules.Remove(ownNode);
    ownNode = null;
    FixRules();
  }
  static Hook lsh;
  static void SetHook(Action<Actor,Vector2> orig, Actor a, Vector2 ls){
    if(a is Player p){
      if(appliedRules.disable) {
        return;
      }
      ls*=appliedRules.speedMultiplier;
      orig(a,ls);
      a.liftSpeedTimer*=appliedRules.gracetimeMultiplier;
    } else orig(a,ls);
  }
  static void BoostOnce(Player p){
    if(appliedRules.disableMulti){
      DebugConsole.Write($"Disabling multiboost with speed {p.LiftSpeed} and grace {p.LiftSpeedGraceTime}");
      p.LiftSpeed = Vector2.Zero;
      p.currentLiftSpeed = Vector2.Zero;
      p.lastLiftSpeed = Vector2.Zero;
      p.liftSpeedTimer = 0;
    }
  }
  [OnLoad.ILHook(typeof(Player),nameof(Player.NormalUpdate))]
  [OnLoad.ILHook(typeof(Player),nameof(Player.ClimbUpdate))]
  [OnLoad.ILHook(typeof(Player),nameof(Player.Jump))]
  [OnLoad.ILHook(typeof(Player),nameof(Player.SuperJump))]
  [OnLoad.ILHook(typeof(Player),nameof(Player.SuperWallJump))]
  [OnLoad.ILHook(typeof(Player),"orig_WallJump")]
  static void UseHook(ILContext ctx){
    ILCursor c = new(ctx);
    while(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<Player>("get_LiftBoost"),
      itr=>itr.MatchCall<Vector2>("op_Addition"),
      itr=>itr.MatchStfld<Player>(nameof(Player.Speed))
    )){
      c.EmitLdarg0();
      c.EmitDelegate(BoostOnce);
    }
  }
  static HookManager hooks = new(()=>{
    lsh = new Hook(typeof(Actor).GetProperty(nameof(Actor.LiftSpeed)).SetMethod,SetHook);
  },()=>{
    lsh.Dispose();
    FixRules();
  }, auspicioushelperModule.OnEnterMap);
}


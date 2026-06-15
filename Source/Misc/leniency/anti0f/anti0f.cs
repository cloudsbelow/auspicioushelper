



using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/Anti0fZone")]
[Tracked]
public partial class Anti0fZone:Entity{
  public class SolidAnti0fComp():Component(true,false){
    HashSet<string> used = new();
    List<(string,Func<bool>)> reasons = new();
    public static void AddReason(Player p, string reason, Func<bool> fn){
      ResetEvents.Hooks<Anti0fZone>.enable();
      if(p.Get<SolidAnti0fComp>() is not {} c) p.Add(c=new());
      if(!c.used.Contains(reason)) c.reasons.Add((reason,fn));
    }
    public static bool Use(Player p){
      if(p.Get<SolidAnti0fComp>() is {} c) foreach(var (r,f) in c.reasons){
        if(f()) return true;
      }
      return false;
    }
  }
  bool ctriggers, cplayercolliders, cthrowables, csolids, alwayswjc, wholeroom;
  enum HitGroundMode{
    None, OnJump, Always 
  }
  HitGroundMode groundMode;
  public Anti0fZone(EntityData d, Vector2 offset):base(d.Position+offset){
    ResetEvents.Hooks<Anti0fZone>.enable();
    bool ci = d.Bool("completely_inside",false);
    cthrowables = d.Bool("holdables", false);
    cplayercolliders = d.Bool("player_colliders",true);
    ctriggers = d.Bool("triggers", false);
    csolids = d.Bool("solids", false);
    alwayswjc = d.Bool("always_walljumpcheck", false);
    csolids |= alwayswjc;
    wholeroom = d.Bool("cover_whole_room",false);
    Collider = ci?new Hitbox(d.Width-16,d.Height-12,8,6):new Hitbox(d.Width,d.Height);
    groundMode = d.Enum("ForceGroundCollide",HitGroundMode.None);
  }
  
  static HoldableRaster hrast = new();
  static ColliderRaster crast = new();
  static TriggerRaster trast = new();
  static SolidRaster srast = new();
  static ZoneRaster rast = new();
  static void ClearRasters(){
    hrast.Clear(); crast.Clear(); trast.Clear(); srast.Clear(); rast.Clear();
  }
  static bool PlayerUpdateDetour3(Player p){
    bool hasMovedAny = false;
    int _st = p.StateMachine.state;
    Vector2 _ispeed = p.Speed;
    int _idashes = p.Dashes;
    if(_st==9 || _st == 22 || skipNormal.OrShortcircuit(p)) return false;

    var dist = _ispeed*Engine.DeltaTime;
    if(dist.LInf()<=1) return false;
    bool exit()=>
      p.StateMachine.state!=_st || p.Dashes!=_idashes ||  (p.Speed!=_ispeed && hasMovedAny) || 
      p.Dead || Engine.FreezeTimer!=0 || exitNormal.OrShortcircuit(p);
    float frac;

    using TrackerOverride tover = new(p.Scene.Tracker);
    float totalfrac = 0;
    start:
      ClearRasters();
      float length = dist.LInf();
      Vector2 step = dist/length;
      if(!rast.Fill(p,step,length,totalfrac==0)) return false;
      float current = 0;

      while(current<length){
        float magn = rast.stepMagn(ref current, length);
        frac = current/length;
        bool rp = rast.prog(p,current);
        if(rp || exit()){
          //if(!hasMovedAny && !exit()) goto reconsile;
          DebugConsole.Write($"Explicit exit: {rp} {p.StateMachine.state} {_st}");
          goto exit;
        }

        var lpos = p.ExactPosition;
        bool flag = false;
        if(p.MoveH(step.X*magn,p.onCollideH)){
          _ispeed.X=p.Speed.X;
          flag = true;
        }
        if(p.MoveV(step.Y*magn,p.onCollideV)){
          _ispeed.Y=p.Speed.Y;
          flag = true;
        }
        hasMovedAny = true;
        if(flag) goto reconsile;
        if((lpos+step*magn - p.ExactPosition).LengthSquared()>0.25) goto reconsile;
      }

    exit:
      ClearRasters();
      return true;
    reconsile:
      totalfrac += frac*(1-totalfrac);
      //DebugConsole.Write($"Reconsiling: {p.Speed} {frac} {totalfrac} {_ispeed} {dist}");
      if(exit() || totalfrac>=1) goto exit;
      dist = _ispeed*Engine.DeltaTime*(1-totalfrac);
      if(dist == Vector2.Zero) goto exit;
      tover.restore();
      goto start;
  }
  [ResetEvents.OnHook(typeof(Actor),nameof(Actor.NaiveMove))]
  static void NaiveMoveHook(On.Celeste.Actor.orig_NaiveMove orig, Actor a, Vector2 dist){
    Player p = null;
    if(a is Player play && Math.Max(Math.Abs(dist.X),Math.Abs(dist.Y))>1 && runNaive.Or(play) && inPlayerUpdate){
      p=play;
    } else {
      orig(a,dist);
      return;
    }

    int _st = p.StateMachine.state;
    Vector2 _ispeed = p.Speed;
    int _idashes = p.Dashes;
    bool exit()=>
      p.StateMachine.state!=_st || p.Dashes!=_idashes || 
      p.Speed!=_ispeed || p.Dead || exitNaive.OrShortcircuit(p);
    
    float current = 0;
    float length = dist.LInf();
    Vector2 step = dist/length;
    using TrackerOverride tover = new(p.Scene.Tracker);

    ClearRasters();
    if(!rast.Fill(p,step,length,true)){
      orig(a,dist);
      return;
    }
    if(rast.hs){
      CullToPathEnt(p.Scene, typeof(DreamBlock), new FloatRect(p)._expand(8,8), dist);
      CullToPathComp(p.Scene, typeof(TemplateDreamblockModifier.DreamMarkerComponent), 
        new FloatRect(p)._expand(8,8), dist, c=>(c as TemplateDreamblockModifier.DreamMarkerComponent).Collider
      );
    }

    while(current<length){
      float magn = rast.stepMagn(ref current, length);
      bool rp = rast.prog(p,current);
      if(rp || exit() || (rast.hs && (_st==Player.StDreamDash || CommunalHelperIop.InTunnel(p)) && DreamExit(p))) goto exit;
      Vector2 lpos = p.ExactPosition;
      orig(p,step*magn);
      if((lpos+step*magn - p.ExactPosition).LengthSquared()>0.25) goto exit;
    }
    exit:
      ClearRasters();
  }
  static bool DreamExit(Player p){
    if(p.dreamDashCanEndTimer>0) return false;
    if(p.StateMachine.State == Player.StDreamDash){
      if(TemplateDreamblockModifier.tryBounce(p)) return true;
      return TemplateDreamblockModifier.DreamMarkerComponent.CheckContinue(p.CollideFirst<DreamBlock>(), p)==null;
    } else if(CommunalHelperIop.InTunnel(p)){
      return !p.CollideCheck<Solid,DreamBlock>();
    }
    return false;
  }

  public static Util.FunctionList<Player> skipNormal = new(Util.FunctionList<Player>.InvocationMode.OrShortcircuit);
  public static Util.FunctionList<Player> exitNormal = new(Util.FunctionList<Player>.InvocationMode.OrShortcircuit);
  public static Util.FunctionList<Player> runNaive = new(Util.FunctionList<Player>.InvocationMode.Or, naiveCheck);
  public static Util.FunctionList<Player> exitNaive = new(Util.FunctionList<Player>.InvocationMode.OrShortcircuit);

  static ILHook updateHook;
  [ResetEvents.ILHook(typeof(Player),nameof(Player.orig_Update))]
  static void ILUpdateHook(ILContext ctx){
    var c = new ILCursor(ctx);
    if(!c.TryGotoNextBestFit(MoveType.After, instr=>instr.MatchCallvirt<Player>("set_Ducking"),instr=>instr.MatchLdarg0())){
      goto bad;
    }
    ILCursor d = c.Clone();
    if(!d.TryGotoNextBestFit(MoveType.After, instr=>instr.MatchCall<Actor>("MoveV"),instr=>instr.MatchPop())){
      goto bad;
    }
    Instruction jumpTarget = d.Next;
    c.EmitDelegate(PlayerUpdateDetour3);
    c.Emit(OpCodes.Brtrue,jumpTarget);
    c.Emit(OpCodes.Ldarg_0);
    return;
    bad:
      DebugConsole.WriteFailure("Could not make anti0f hook",true);
  }
  static bool inPlayerUpdate = false;
  [ResetEvents.OnHook(typeof(Player),nameof(Player.Update))]
  static void Hook(On.Celeste.Player.orig_Update orig, Player p){
    inPlayerUpdate = true;
    orig(p);
    inPlayerUpdate = false;
  }
  static bool naiveCheck(Player p)=>p.StateMachine.State==Player.StDreamDash;
}
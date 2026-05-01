



using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
public class Anti0fZone:Entity{
  public FloatRect bounds;
  bool ctriggers, cplayercolliders, cthrowables, csolids, alwayswjc, wholeroom;
  enum HitGroundMode{
    None, OnJump, Always 
  }
  HitGroundMode groundMode;
  public Anti0fZone(EntityData d, Vector2 offset):base(d.Position+offset){
    bool ci = d.Bool("completely_inside",false);
    bounds = new FloatRect(Position.X,Position.Y,d.Width,d.Height);
    hooks.enable();
    cthrowables = d.Bool("holdables", false);
    cplayercolliders = d.Bool("player_colliders",true);
    ctriggers = d.Bool("triggers", false);
    csolids = d.Bool("solids", false);
    alwayswjc = d.Bool("always_walljumpcheck", false);
    csolids |= alwayswjc;
    wholeroom = d.Bool("cover_whole_room",false);
    Collider = ci?new Hitbox(d.Width-12,d.Height-12,6,6):new Hitbox(d.Width,d.Height);
    groundMode = d.Enum("ForceGroundCollide",HitGroundMode.None);
  }

  public static Anti0fZone getHit(Player p){
    FloatRect r = new FloatRect(p);
    foreach(Anti0fZone a in p.Scene.Tracker.GetEntities<Anti0fZone>()){
      if(a.wholeroom || a.bounds.CollideRectSweep(r,p.Speed*Engine.DeltaTime)){
        return a;
      }
    }
    return null;
  }

  class HoldableRaster:LinearRaster<Holdable>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(1,1);
      Fill(p.Scene.Tracker.GetComponents<Holdable>().Select(
        h=>{
          Holdable o = (Holdable) h;
          Collider origc = h.Entity.Collider;
          if(o.PickupCollider!=null){
            h.Entity.Collider = o.PickupCollider;
          }
          var res =  new ACol<Holdable>(f.ISweep(h.Entity.Collider,-step),(Holdable)h);
          h.Entity.Collider = origc;
          return res;
        }
      ),maxt);
    }
    public bool prog(Player p, float step){
      if(!Input.GrabCheck || p.IsTired || p.Holding!=null) return false;
      switch(p.StateMachine.state){
        case 0: case 7:
          if(p.Ducking) return false; break;
        case 2:
          if(p.DashDir == Vector2.Zero || !p.CanUnDuck) return false; break;
        default: return false;
      }
      prog(step);
      foreach(var h in active){
        if(h.o.Check(p) && p.Pickup(h.o)){
          DebugConsole.Write("Picked up");
          p.StateMachine.State = 8; 
          return true;
        }
      }
      return false;
    }
  }
  class ColliderRaster:LinearRaster<PlayerCollider>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(1,1);
      Fill(p.Scene.Tracker.GetComponents<PlayerCollider>().Where(h=>h.Entity.Collidable).Select(
        h=>new ACol<PlayerCollider>(f.ISweep(h.Entity.Collider,-step),(PlayerCollider)h)
      ),maxt);
    }
    public bool prog(Player p, float step){
      if(p.StateMachine.state == 21) return false;
      prog(step);
      Collider old = p.Collider;
      p.Collider = p.hurtbox;
      active.RemoveAll(cn=>{
        if(p.Dead || cn.o.Check(p)) return true;
        return false;
      });
      p.Collider = old;
      return p.Dead;
    }
  }
  class TriggerRaster:LinearRaster<Trigger>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(4,4);
      Fill(p.Scene.Tracker.GetEntities<Trigger>().Select(
        h=>new ACol<Trigger>(f.ISweep(h.Collider,-step),(Trigger)h)
      ),maxt);
    }
    public bool prog(Player p, float step){
      if (p.StateMachine.State == 18) return false;
      prog(step);
      active.RemoveAll(cn=>{
        var t = cn.o;
        if (p.CollideCheck(t)){
          if (!t.Triggered){
            t.Triggered = true;
            p.triggersInside.Add(t);
            t.OnEnter(p);
          }
          t.OnStay(p);
          return true;
        } 
        return false;
      });
      return false;
    }
  }
  class SolidRaster:LinearRaster<Solid>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(8,8);
      Fill(p.Scene.Tracker.GetEntities<Solid>().Select(
        h=>new ACol<Solid>(f.ISweep(h.Collider,-step),(Solid)h)
      ),maxt);
    }
    public bool prog(Player p, float step, bool doPlayerstuff=true, bool alwayswjc = false){
      
      if(prog(step)) p.Scene.Tracker.Entities[typeof(Solid)] = active.Map(x=>(Entity)x.o);
      //DebugConsole.Write($"solid reinterpolation {p.StateMachine.state}");
      bool wjcp,wjcn;
      bool grounded = false;
      if(!doPlayerstuff) return false;
      int st = p.StateMachine.State;
      if((st==Player.StNormal || st==Player.StDash || st==Player.StRedDash) && p.Speed.Y>=0){
        bool og = p.onGround;
        Vector2 gp = p.Position+Vector2.UnitY;
        Platform plat = (Platform)p.CollideFirst<Solid>(gp)?? p.CollideFirstOutside<JumpThru>(gp);
        if(plat!=null && !og){
          p.onGround = true;
          grounded = true;
          if(st==Player.StNormal){
            if(p.Holding == null){
              if((float)Input.MoveY == 1f) p.Ducking = true;
            } else {
              if(!p.Ducking && (float)Input.MoveY == 1f && !p.holdCannotDuck){
                p.Drop();
                p.Ducking = true;
              }
            }
          }
          p.StartJumpGraceTime();
          if(p.dashRefillCooldownTimer<=0){
            bool f1 = SaveData.Instance.Assists.DashMode == Assists.DashModes.Infinite && !p.level.InCutscene;
            if(f1 || (!p.Inventory.NoRefills && p.onGround && 
              (p.CollideCheck<Solid, NegaBlock>(gp) || p.CollideCheckOutside<JumpThru>(gp)) &&
              (!p.CollideCheck<Spikes>(p.Position) || SaveData.Instance.Assists.Invincible)
            )){
              p.RefillDash();
            }
          }
          bool noNj = !Input.Jump.Pressed || (TalkComponent.PlayerOver != null && Input.Talk.Pressed) || Input.Jump.bufferCounter==0;
          if(rast.active.Any(x=>x.o.groundMode==HitGroundMode.Always || (!noNj && x.o.groundMode==HitGroundMode.OnJump))){
            var oldspeed=p.Speed;
            p.OnCollideV(new(){Direction=Vector2.UnitY, Moved=Vector2.Zero, Hit=plat, Pusher=null, TargetPosition=gp});
            DebugConsole.Write("Forced ground collision", oldspeed, p.Speed);
          } 
        }
      }
      switch(p.StateMachine.state){
        case Player.StNormal:
          
          if (!Input.Jump.Pressed || (TalkComponent.PlayerOver != null && Input.Talk.Pressed) || Input.Jump.bufferCounter==0) return false;
          if((p.jumpGraceTimer>0f || p.onGround) && p.Speed.Y>=0){
            p.Jump(); //always happens before we reach here
            DebugConsole.Write("Normal jump in anti0f");
            return true;
          }
          if(p.CanUnDuck){
            wjcp = (alwayswjc||p.CollideCheck<Solid>(p.Position+Vector2.UnitX*5)) && p.WallJumpCheck(1);
            wjcn = (alwayswjc||p.CollideCheck<Solid>(p.Position-Vector2.UnitX*5)) && p.WallJumpCheck(-1);
            //DebugConsole.Write($"from anti0f: {alwayswjc} {wjcp} {wjcn}");
            if(wjcp){
              //DebugConsole.Write($"anti0fwj happened {p.onGround} {p.Speed} {Input.Jump.bufferCounter}");
              if (p.Facing==Facings.Right && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && p.Stamina>0f && 
                p.Holding==null && !ClimbBlocker.Check(p.Scene, p, p.Position + Vector2.UnitX * 3f)
              ) {
                var old = p.Speed;
                p.ClimbJump(); DebugConsole.Write($"0f'd cb {old} {p.Speed} {p.LiftSpeed} {p.liftSpeedTimer}");
              }
              else if (p.DashAttacking && p.SuperWallJumpAngleCheck) p.SuperWallJump(-1);
              else p.WallJump(-1);
              return true;
            } else if(wjcn){
              //DebugConsole.Write($"anti0fwj happened {p.onGround} {p.OnGround()}");
              if(p.Facing==Facings.Left && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && p.Stamina>0f && 
                p.Holding==null && !ClimbBlocker.Check(p.Scene, p, p.Position + Vector2.UnitX * -3f)
              ) {
                var old = p.Speed;
                p.ClimbJump(); DebugConsole.Write($"0f'd cb {old} {p.Speed} {p.LiftSpeed} {p.liftSpeedTimer}");
              }
              else if (p.DashAttacking && p.SuperWallJumpAngleCheck) p.SuperWallJump(1);
              else p.WallJump(1);
              return true;
            }
          }
          return false;
        case Player.StDash: case Player.StRedDash:
          //DebugConsole.Write($"dash reinterpolation {p.Speed}");
          if(!Input.Jump.Pressed || !p.CanUnDuck || Input.Jump.bufferCounter==0) return false;
          if(p.CanUnDuck && p.jumpGraceTimer>0f && p.Speed.Y>=0 && Math.Abs(p.DashDir.Y) < 0.1f){
            p.SuperJump(); 
            DebugConsole.Write("Super jump in anti0f");
            p.StateMachine.State  =0;
            return true;
          }
          wjcp = (alwayswjc||p.CollideCheck<Solid>(p.Position+Vector2.UnitX*5)) && p.WallJumpCheck(1);
          wjcn = (alwayswjc||p.CollideCheck<Solid>(p.Position-Vector2.UnitX*5)) && p.WallJumpCheck(-1);
          if(!(wjcp || wjcn)) return false;
          if(p.SuperWallJumpAngleCheck) p.SuperWallJump(wjcp?-1:1);
          else if(wjcp){
            if(p.Facing==Facings.Right && Input.GrabCheck && p.Stamina>0f && p.Holding==null && !ClimbBlocker.Check(p.Scene, p, p.Position+Vector2.UnitX*3f)){
              var old = p.Speed;
              p.ClimbJump(); DebugConsole.Write($"0f'd cb {old} {p.Speed} {p.LiftSpeed} {p.liftSpeedTimer}");
            } else if(p.SuperWallJumpAngleCheck)p.SuperWallJump(-1);
            else p.WallJump(-1);
          } else {
            if(p.Facing==Facings.Left && Input.GrabCheck && p.Stamina>0f && p.Holding==null && !ClimbBlocker.Check(p.Scene, p, p.Position-Vector2.UnitX*3f)){
              var old = p.Speed;
              p.ClimbJump(); DebugConsole.Write($"0f'd cb {old} {p.Speed} {p.LiftSpeed} {p.liftSpeedTimer}");
            } else if(p.SuperWallJumpAngleCheck)p.SuperWallJump(1);
            else p.WallJump(1);
          }
          p.StateMachine.State = 0;
          return true;
      }
      return grounded;
    }
  }
  class JtRaster:LinearRaster<JumpThru>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(1,4);
      Fill(p.Scene.Tracker.GetEntities<JumpThru>().Select(
        h=>new ACol<JumpThru>(f.ISweep(h.Collider,-step),(JumpThru)h)
      ),maxt, true);
    }
  }
  class ZoneRaster:LinearRaster<Anti0fZone>{
    int zpc; public bool doplayercolliders=>zpc>0; public bool hpc;
    int zt; public bool dotriggers=>zt>0; public bool ht;
    int zh; public bool doholdables=>zh>0; public bool hh;
    int zs; public bool dosolids=>zs>0; public bool hs;
    int zwj; public bool dowalljumps=>zwj>0;
    bool wholeroom;

    void inire(Anti0fZone z, int adj){
      zpc+=z.cplayercolliders?adj:0;
      zt+=z.ctriggers?adj:0;
      zh+=z.cthrowables?adj:0;
      zs+=z.csolids?adj:0;
      zwj+=z.alwayswjc?adj:0;
    }
    Anti0fZone iniw(Anti0fZone z){
      if(z.wholeroom){
        wholeroom = true;
        hpc|=z.cplayercolliders;
        ht|=z.ctriggers;
        hh|=z.cthrowables;
        hs|=z.csolids;
        inire(z,1);
      }
      return z;
    }
    public bool Fill(Player p, Vector2 step, float maxt, bool first){
      FloatRect f = new FloatRect(p)._expand(1,1);
      wholeroom = false; zpc=0; zt=0; zh=0; zs=0; zwj=0;
      hpc = false; ht = false; hh = false; hs = false;
      Fill(p.Scene.Tracker.GetEntities<Anti0fZone>().Select(
        h=>new ACol<Anti0fZone>(f.ISweep(h.Collider,-step),iniw((Anti0fZone)h))
      ),maxt);
      if(first && !useSteps) return false;
      foreach(var z_ in mayHit){
        var z = z_.o;
        hpc|=z.cplayercolliders;
        ht|=z.ctriggers;
        hh|=z.cthrowables;
        hs|=z.csolids;
      }
      if(hpc) crast.Fill(p,step,maxt);
      if(ht) trast.Fill(p,step,maxt);
      if(hh) hrast.Fill(p,step,maxt);
      if(hs){
        srast.Fill(p,step,maxt);
        jtrast.Fill(p,step,maxt);
        p.Scene.Tracker.Entities[typeof(Solid)] = srast.active.Select(s=>s.o).ToList<Entity>();
        p.Scene.Tracker.Entities[typeof(JumpThru)] = jtrast.active.Select(s=>s.o).ToList<Entity>();
      }
      return true;
    }
    public bool prog(Player p, float step){
      active.RemoveAll(x=>{
        if(x.f.exit<step){
          inire(x.o,-1);
          return false;
        }
        return false;
      });
      while(addIdx<mayHit.Count && mayHit[addIdx].f.enter<=step){
        if(mayHit[addIdx].f.exit>=step){
          inire(mayHit[addIdx].o,1);
          active.Add(mayHit[addIdx]);
        }
        addIdx++;
      }

      bool flag = false;
      if(hs)flag |= srast.prog(p,step,dosolids,dowalljumps);
      if(flag || exitNormal.OrShortcircuit(p)) return true;
      if(doholdables) flag |= hrast.prog(p,step);
      if(doplayercolliders) flag |= crast.prog(p,step);
      if(dotriggers) flag |= trast.prog(p,step);
      return flag;
    }
    //This function is what someone deeply scared of floating point misrepresentation writes.
    public float stepMagn(ref float current, float max){
      float dif = max-current;
      if(max-current<=1){
        current = max;
        return dif;
      }
      if(wholeroom || active.Count != 0){
        current=current+1;
        return 1;
      }
      if(addIdx>=mayHit.Count){
        current = max;
        return dif;
      }
      max = MathF.Max(1,MathF.Min(mayHit[addIdx].f.enter,max));
      dif = max-current;
      current = max;
      return dif;
    }
    public bool useSteps=>wholeroom||mayHit.Count>0;
  }
  
  static HoldableRaster hrast = new();
  static ColliderRaster crast = new();
  static TriggerRaster trast = new();
  static SolidRaster srast = new();
  static JtRaster jtrast = new();
  static ZoneRaster rast = new();
  static List<Entity> oldSolids;
  static List<Entity> oldJts;
  static void ClearRasters(){
    hrast.Clear(); crast.Clear(); trast.Clear(); srast.Clear(); jtrast.Clear(); rast.Clear();
  }
  static bool PlayerUpdateDetour3(Player p){
    bool first = true;
    int _st = p.StateMachine.state;
    Vector2 _ispeed = p.Speed;
    int _idashes = p.Dashes;
    if(_st==9 || _st == 22 || skipNormal.OrShortcircuit(p)) return false;

    var dist = _ispeed*Engine.DeltaTime;
    if(Math.Max(Math.Abs(dist.X),Math.Abs(dist.Y))<=1) return false;
    bool exit()=>
      p.StateMachine.state!=_st || p.Dashes!=_idashes || 
      p.Speed!=_ispeed || p.Dead || exitNormal.OrShortcircuit(p);
    float frac;

    oldSolids = p.Scene.Tracker.Entities[typeof(Solid)];
    oldJts = p.Scene.Tracker.Entities[typeof(JumpThru)];
    float totalfrac = 0;
    start:
      ClearRasters();
      float length = Math.Max(Math.Abs(dist.X), Math.Abs(dist.Y)); //L1 distance obviously
      Vector2 step = dist/length;
      if(!rast.Fill(p,step,length,first)) return false;
      //DebugConsole.Write($"Anti0f stuff start {_st}");
      first = false;
      float current = 0;
      //DebugConsole.Write($"currrent:{current} max:{length}");
      while(current<length){
        float magn = rast.stepMagn(ref current, length);
        //DebugConsole.Write($"magnitude:{magn} currrent:{current} max:{length}");
        frac = current/length;
        bool rp = rast.prog(p,current);
        if(rp||exit()){
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
        if(flag) goto reconsile;
        if((lpos+step*magn - p.ExactPosition).LengthSquared()>0.25){
          DebugConsole.Write("Position sharply changed while in anti0f! Attempting to reconsile");
          goto reconsile;
        }
      }

    exit:
      ClearRasters();
      p.Scene.Tracker.Entities[typeof(Solid)] = oldSolids;
      p.Scene.Tracker.Entities[typeof(JumpThru)] = oldJts;
      return true;
    reconsile:
      totalfrac += frac;
      //DebugConsole.Write($"Reconsiling: {p.Speed} {frac} {totalfrac} {_ispeed} {dist}");
      if(exit() || totalfrac>=1) goto exit;
      dist = _ispeed*Engine.DeltaTime*(1-totalfrac);
      if(dist == Vector2.Zero) goto exit;
      p.Scene.Tracker.Entities[typeof(Solid)] = oldSolids;
      p.Scene.Tracker.Entities[typeof(JumpThru)] = oldJts;
      goto start;
  }
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
    float length = Math.Max(Math.Abs(dist.X), Math.Abs(dist.Y)); //L1 distance obviously
    Vector2 step = dist/length;
    ClearRasters();
    oldSolids = p.Scene.Tracker.Entities[typeof(Solid)];
    oldJts = p.Scene.Tracker.Entities[typeof(JumpThru)];
    if(!rast.Fill(p,step,length,true)){
      orig(a,dist);
      return;
    }
    while(current<length){
      float magn = rast.stepMagn(ref current, length);
      bool rp = rast.prog(p,current);
      if(rp||exit()){
        DebugConsole.Write($"Explicit exit from naive move {p.Position}");
        goto exit;
      };
      Vector2 lpos = p.ExactPosition;
      orig(p,step*magn);
      if((lpos+step*magn - p.ExactPosition).LengthSquared()>0.25) goto exit;
    }
    exit:
      p.Scene.Tracker.Entities[typeof(Solid)] = oldSolids;
      p.Scene.Tracker.Entities[typeof(JumpThru)] = oldJts;
      ClearRasters();
  }

  public static Util.FunctionList<Player> skipNormal = new(Util.FunctionList<Player>.InvocationMode.OrShortcircuit);
  public static Util.FunctionList<Player> exitNormal = new(Util.FunctionList<Player>.InvocationMode.OrShortcircuit);
  public static Util.FunctionList<Player> runNaive = new(Util.FunctionList<Player>.InvocationMode.Or);
  public static Util.FunctionList<Player> exitNaive = new(Util.FunctionList<Player>.InvocationMode.OrShortcircuit);

  static ILHook updateHook;
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
    // for(int i=-10; i<30; i++){
    //   try{
    //     if(i==0) DebugConsole.Write("===========");
    //     DebugConsole.Write(c.Instrs[c.Index+i].ToString());
    //   }catch(Exception ex){
    //     DebugConsole.Write("cannot");
    //   }
    // }
    //DebugConsole.Write("Setup successfully");
    return;
    bad:
      DebugConsole.Write("Something went wrong while setting up player update hooks for anti0f");
  }
  static bool inPlayerUpdate = false;
  static void Hook(On.Celeste.Player.orig_Update orig, Player p){
    inPlayerUpdate = true;
    orig(p);
    inPlayerUpdate = false;
  }
  static bool naiveCheck(Player p)=>p.StateMachine.State==Player.StDreamDash;
  public static HookManager hooks = new HookManager(()=>{
    MethodInfo update = typeof(Player).GetMethod(
      "orig_Update", BindingFlags.Public |BindingFlags.Instance
    );
    updateHook = new ILHook(update, ILUpdateHook);
    On.Celeste.Actor.NaiveMove+=NaiveMoveHook;
    On.Celeste.Player.Update+=Hook;
    runNaive.Add(naiveCheck);
  },void ()=>{
    updateHook.Dispose();
    On.Celeste.Actor.NaiveMove-=NaiveMoveHook;
    On.Celeste.Player.Update-=Hook;
  },auspicioushelperModule.OnEnterMap);
}



using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public partial class Anti0fZone{
  class SolidRaster:LinearRaster<Solid>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(8,8);
      Fill(p.Scene.Tracker.GetEntities<Solid>().Map(
        h=>new ACol<Solid>(f.ISweep(h.Collider,-step),(Solid)h)
      ),maxt);
      TrackerOverride.SetEnt(typeof(Solid), active.Map(x=>(Entity)x.o));
    }
    public bool prog(Player p, float step, bool doPlayerstuff=true, bool alwayswjc = false){
      
      if(prog(step)) TrackerOverride.SetEnt(typeof(Solid), active.Map(x=>(Entity)x.o));
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
}



using System;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static class PlayerHelper{
  static public Entity getFollowEnt(){
    if(UpdateHook.cachedPlayer is not { } p) return null;
    switch(p.StateMachine.state){
      case Player.StBoost:
        if(p.CurrentBooster is IBooster.SentinalBooster sb){
          return null;
        } else return p.CurrentBooster;
      case Player.StDreamDash:
        if(p.dreamBlock is TemplateDreamblockModifier.SentinalDb sdb){
          return sdb.lastEntity;
        } else return null;
      default: return null; 
    }
  } 
  static public void MovePlayer(Template moved, Entity movewith, Vector2 oldpos){
    if(UpdateHook.cachedPlayer is not { } p) return;
    Vector2 del = movewith.Position-oldpos;
    if(p.TreatNaive){
      if(!MovementLock.movedX(p))p.NaiveMove(Vector2.UnitX*del.X);
      if(!MovementLock.movedY(p))p.NaiveMove(Vector2.UnitY*del.Y);
    } else {
      if(!MovementLock.movedX(p))p.MoveH(del.X);
      if(!MovementLock.movedY(p))p.MoveV(del.Y);
    }
    switch(p.StateMachine.state){
      case Player.StBoost:
        if(movewith is Booster b){
          if(b is not IBooster.SentinalBooster sb){
            p.boostTarget = b.Position;
          }
        }
        break;
    }
    Template parent = null;
    if(movewith is ITemplateChild itc)parent=itc.parent;
    if(movewith.Get<ChildMarker>() is ChildMarker cm)parent=cm.parent;
    if(parent!=null) p.LiftSpeed = parent.gatheredLiftspeed;
    else p.LiftSpeed = del/MathF.Max(0.001f,Engine.DeltaTime);  
  }

  [CustomEntity("auspicioushelper/triggerparenttrigger")]
  public class TriggerParentTrigger:Trigger,ISimpleEnt{
    public Template parent {get;set;}
    public Vector2 toffset {get;set;}
    bool onEnter;
    bool onExit;
    public TriggerParentTrigger(EntityData d, Vector2 o):base(d,o){
      onEnter = d.Bool("onEnter",true);
      onExit = d.Bool("onLeave",false);
    }
    public class PlayerInfo:TriggerInfo{
      bool use = true;
      string sts;
      public PlayerInfo(Player p, Template t, bool use=true){
        entity=p;
        sts=Util.TrimSt(p,p.StateMachine.State);
        parent = t;
        this.use=use;
      }
      public override bool shouldTrigger => use;
      public override string category=>"player/"+sts;
    }
    public override void OnEnter(Player player) {
      base.OnEnter(player);
      if(onEnter) parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new PlayerInfo(player, parent));
    }
    public override void OnLeave(Player player) {
      base.OnLeave(player);
      if(onExit) parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new PlayerInfo(player, parent));
    }
  }
}
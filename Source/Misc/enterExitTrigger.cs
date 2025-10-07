

using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public class EnterExitTriggerTrigger:Trigger{
  Vector2 n1;
  Vector2 n2;
  public EnterExitTriggerTrigger(EntityData d, Vector2 offset):base(d,offset){

  }
  public override void OnEnter(Player p){
    base.OnEnter(p);
    foreach(Trigger t in Scene.Tracker.GetEntities<Trigger>()){
      if(t.CollidePoint(n1))t.OnEnter(p);
    }
  }
  public override void OnLeave(Player p) {
    base.OnLeave(p);
    foreach(Trigger t in Scene.Tracker.GetEntities<Trigger>()){
      if(t.CollidePoint(n2))t.OnEnter(p);
    }
  }
  public override void OnStay(Player p) {
    base.OnStay(p);
  }
}
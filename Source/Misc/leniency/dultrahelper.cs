


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/dultraThing")]
public class DultraHelper:Trigger{
  public DultraHelper(EntityData d, Vector2 o):base(d,o){}
  public override void OnStay(Player p) {
    base.OnStay(p);
    if(p.DashDir.Y>0 && p.StateMachine.State == Player.StNormal){
      p.MoveV(1,p.OnCollideV);
      if(p.DashDir.Y==0) DebugConsole.Write("Dultra given");
    }
  }
}
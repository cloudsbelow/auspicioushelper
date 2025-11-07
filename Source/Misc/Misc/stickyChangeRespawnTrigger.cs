


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/PersistentChangeRespawn")]
public class PersistentChangeRespawn:ChangeRespawnTrigger{
  public PersistentChangeRespawn(EntityData d, Vector2 o):base(d,o){
  }
  public override void OnEnter(Player player) {
    base.OnEnter(player);
    Session s = (Scene as Level).Session;
    auspicioushelperModule.Session.respDat = new(){
      loc = s.RespawnPoint.Value, level = s.Level
    };
  }
}
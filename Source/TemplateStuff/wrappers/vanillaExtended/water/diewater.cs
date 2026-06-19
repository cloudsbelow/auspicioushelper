



using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/DieWater")]
public class DieWater:Water,ConnectedBlocks.ICustomCheckCollider{
  Collider oc;
  Collider ConnectedBlocks.ICustomCheckCollider.Get => oc??Collider;
  public DieWater(EntityData d,Vector2 offset):base(d,offset){}
  public override void Added(Scene scene){
    base.Added(scene);
    oc = Collider;
    Collider = new Hitbox(0,0,-100,-100);
    oc.Top+=3;
    Add(new PlayerCollider((Player p)=>{
      p.Die(Vector2.UnitY);
    },oc));
  }
}
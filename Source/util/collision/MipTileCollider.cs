




using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class MiptileCollider:ColliderList{
  MipGrid mipgrid;
  public override bool Collide(Circle circle) {
    return base.Collide(circle);
  }
  public override bool Collide(Grid grid) {
    return base.Collide(grid);
  }
  public override bool Collide(ColliderList list) {
    return base.Collide(list);
  }
  public override bool Collide(Hitbox hitbox) {
    return base.Collide(hitbox);
  }
  public override bool Collide(Rectangle rect) {
    return base.Collide(rect);
  }
  public override bool Collide(Vector2 from, Vector2 to) {
    return base.Collide(from, to);
  }
  public override bool Collide(Vector2 point) {
    return base.Collide(point);
  }
}
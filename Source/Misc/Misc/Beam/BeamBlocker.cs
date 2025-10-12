


using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/BeamBlocker")]
[Tracked]
public class BeamBlocker:Entity{
  Vector2 size;

  public BeamBlocker(EntityData data, Vector2 offset):base(data.Position+offset){
    size=new Vector2(data.Width,data.Height);
  }
  public float rayCollision(Vector2 origin, Vector2 dir){
    Vector2 tsp = (Position+size-origin)/dir;
    Vector2 tsn = (Position-origin)/dir;
    float texit = Math.Min(Math.Max(tsp.X,tsn.X),Math.Max(tsp.Y,tsn.Y));
    float tenter = Math.Max(Math.Min(tsp.X,tsn.X),Math.Min(tsp.Y,tsn.Y));
    if(texit<tenter || texit<0){
      return float.PositiveInfinity;
    }
    return tenter;
  }
  public override void Render()
  {
      base.Render();
      Draw.Rect(Position, size.X, size.Y, new Color(0,0.5f,0.5f,0.3f));
  }
}
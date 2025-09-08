



using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

[TrackedAs(typeof(Water))]
public class WaterW:Water,ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset { get;set; }

  public WaterW(EntityData d, Vector2 offset):base(d,offset){
  }
  void ITemplateChild.relposTo(Vector2 nloc, Vector2 liftspeed){
    Vector2 nl = (nloc+toffset).Round();
    if(nl==Position) return;
    Vector2 d = nl-Position;
    foreach(Surface s in Surfaces){
      
    }
    Position = nl;
  }
  public override void Render() {
    base.Render();
    //Draw.Rect(base.X + (float)fill.X, base.Y + (float)fill.Y, fill.Width, fill.Height, FillColor);
  }
}
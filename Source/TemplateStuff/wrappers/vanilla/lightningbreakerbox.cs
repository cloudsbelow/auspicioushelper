



using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class LightningBreakerW:LightningBreakerBox,ISimpleEnt{
  public Template parent{get;set;}
  public Vector2 toffset{get;set;}
  public LightningBreakerW(EntityData e, Vector2 o):base(e,o){}
  public override void Update() {
    start = parent.roundLoc+toffset;
    base.Update();
    if (!Collidable){
      sink = Calc.Approach(sink, 0, 2f * Engine.DeltaTime);
      sine.Rate = MathHelper.Lerp(1f, 0.5f, sink);
      Vector2 vector = start;
      vector.Y += sink * 6f + sine.Value * MathHelper.Lerp(4f, 2f, sink);
      vector += bounce.Value * bounceDir * 12f;
      MoveToX(vector.X);
      MoveToY(vector.Y);
      if (smashParticles){
        smashParticles = false;
        SmashParticles(bounceDir.Perpendicular());
        SmashParticles(-bounceDir.Perpendicular());
      }
    }
  }
}




using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateIceblock")]
public class TemplateIceblock:Template{
  Vector2 offset = Vector2.Zero;
  public override Vector2 virtLoc => Position+offset;
  float sinkTime;
  float sinkDist;
  float respawnTimer=0;
  float respawnTime = 2;
  public TemplateIceblock(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public TemplateIceblock(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    sinkTime = d.Float("sinkTime",1);
    sinkDist = d.Float("sinkDist",12);
  }
  IEnumerator iceRoutine(){
    float time = 0;
    shake(0.1f);
    ownLiftspeed = Vector2.UnitY*sinkDist/sinkTime;
    while(time<sinkTime-0.2){
      time = time+Engine.DeltaTime;
      offset = Vector2.UnitY*sinkDist*time/sinkTime;
      childRelposSafe();
      yield return null;
    }
    shake(0.2f);
    while(time<sinkTime){
      time = time+Engine.DeltaTime;
      offset = Vector2.UnitY*sinkDist*time/sinkTime;
      childRelposSafe();
      yield return null;
    }
    destroyChildren();
    respawnTimer = 1;
  }
  public override void Update() {
    base.Update();
  }
}
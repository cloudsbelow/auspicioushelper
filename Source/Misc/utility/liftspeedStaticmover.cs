


using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[TrackedAs(typeof(StaticMover))]
public class LiftspeedSm:StaticMover{
  const int trackLen = 5;
  Util.RingBuffer<Vector2> ls = new(trackLen);
  Template parent;
  public Vector2 getLiftspeed(){
    if(parent!=null) return parent.gatheredLiftspeed;
    for(int i=0; i<trackLen; i++){
      if(ls[i]!=Vector2.Zero) return ls[i];
    }
    return Vector2.Zero;
  }
  public Action<Vector2> OnMoveOther;
  public LiftspeedSm():base(){
    Active = true;
    OnMove = (Vector2 amt)=>{
      ls[0] = Platform.LiftSpeed;
      OnMoveOther(amt);
    };
  }
  public override void Update() {
    base.Update();
    if(Engine.DeltaTime!=0){
      ls.Backward();
      ls[0]=Vector2.Zero;
    }
  }
}
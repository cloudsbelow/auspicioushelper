


using System;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[TrackedAs(typeof(StaticMover))]
public class LiftspeedSm:StaticMover{
  const int trackLen = 5;
  Util.RingBuffer<(Vector2, float)> ls = new(trackLen);
  Template parent;
  public Vector2 getLiftspeed(){
    if(parent!=null) return parent.gatheredLiftspeed;
    float timesum = -Engine.DeltaTime;
    for(int i=0; i<trackLen; i++){
      if(ls[i].Item1!=Vector2.Zero){
        if(ls[i].Item1.L1()*timesum>1) return Vector2.Zero;
        return ls[i].Item1;
      }
      timesum+=ls[i].Item2;
    }
    return Vector2.Zero;
  }
  public Action<Vector2> OnMoveOther;
  new Action<Vector2> OnMove{get=>OnMoveOther; set=>OnMoveOther=value;}
  public LiftspeedSm():base(){
    Active = true;
    base.OnMove = (Vector2 amt)=>{
      ls[0].Item1 = Platform.LiftSpeed;
      OnMoveOther(amt);
    };
    OnAttach = p=>{
      if(p.Get<ChildMarker>() is {} cm) parent = cm.parent;
    };
  }
  public override void Update() {
    base.Update();
    if(Engine.DeltaTime!=0){
      ls.Backward();
      ls[0]=new(Vector2.Zero,Engine.DeltaTime);
    }
  }
}
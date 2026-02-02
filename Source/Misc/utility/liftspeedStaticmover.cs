


using System;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[TrackedAs(typeof(StaticMover))]
public class LiftspeedSm:StaticMover{
  const int trackLen = 8;
  Util.RingBuffer<(Vector2, float)> ls = new(trackLen);
  public Template parent;
  int frameCount;
  public Vector2 getLiftspeed(){
    if(parent!=null) return parent.gatheredLiftspeed;
    float timesum = frameCount == UpdateHook.framenum? -Engine.DeltaTime:0;
    for(int i=0; i<trackLen; i++){
      if(ls[i].Item1!=Vector2.Zero){
        DebugConsole.Write("ls",ls[i].Item1.LInf()*timesum, timesum, ls[i].Item1.LInf());
        if(ls[i].Item1.LInf()*timesum>1) return Vector2.Zero;
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
  }
  public override void Update() {
    base.Update();
    frameCount = UpdateHook.framenum;
    if(Engine.DeltaTime!=0){
      ls.Backward();
      ls[0]=new(Vector2.Zero,Engine.DeltaTime);
    }
  }
}
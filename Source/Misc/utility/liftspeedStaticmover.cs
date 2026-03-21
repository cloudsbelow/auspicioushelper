


using System;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[TrackedAs(typeof(StaticMover))]
public class LiftspeedSm:StaticMover{
  const int trackLen = 8;
  public struct LiftspeedHelper{
    Util.RingBuffer<(Vector2, float)> ls = new(trackLen);
    int frameCount;
    public Vector2 getLiftspeed(Template parent=null){
      if(parent!=null) return parent.gatheredLiftspeed;
      float timesum = frameCount == UpdateHook.framenum? -Engine.DeltaTime:0;
      for(int i=0; i<trackLen; i++){
        if(ls[i].Item1!=Vector2.Zero){
          if(ls[i].Item1.LInf()*timesum>1) return Vector2.Zero;
          return ls[i].Item1;
        }
        timesum+=ls[i].Item2;
      }
      return Vector2.Zero;
    }
    public Vector2 getLiftspeedSmear(int frames){
      Vector2 acc = Vector2.Zero;
      if(frameCount == UpdateHook.framenum) frames++;
      float timesum = frameCount == UpdateHook.framenum? -Engine.DeltaTime:0;
      for(int i=0; i<frames; i++){
        if(i!=0 && ls[i].Item1.LInf()*timesum>1) return acc/i; 
        acc+=ls[i].Item1;
        timesum+=ls[i].Item2;
      }
      return acc/frames;
    }
    public void Update(){
      frameCount = UpdateHook.framenum;
      if(Engine.DeltaTime!=0){
        ls.Backward();
        ls[0]=new(Vector2.Zero,Engine.DeltaTime);
      }
    }
    public void SetSpeed(Vector2 s){
      ls[0].Item1 = s;
    }
    public void AddSpeed(Vector2 s){
      ls[0].Item1 += s;
    }
    public LiftspeedHelper(){}
  }
  LiftspeedHelper ls = new();
  public Vector2 getLiftspeed()=>ls.getLiftspeed(parent);
  public Template parent;
  public Action<Vector2> OnMoveOther;
  new Action<Vector2> OnMove{get=>OnMoveOther; set=>OnMoveOther=value;}
  public LiftspeedSm():base(){
    Active = true;
    base.OnMove = (Vector2 amt)=>{
      ls.SetSpeed(Platform.LiftSpeed);
      OnMoveOther(amt);
    };
  }
  public override void Update() {
    base.Update();
    ls.Update();
  }
}
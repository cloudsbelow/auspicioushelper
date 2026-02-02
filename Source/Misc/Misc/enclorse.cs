


using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/enclorse")]
public class Enclorse:Entity{
  string channel;
  Sprite sprite;
  public Enclorse(EntityData d, Vector2 o):base(d.Position+o){
    channel = d.Attr("channel","enclosed_horse");
    Add(new NeedsAcceleratorComp(OnTree));
    Add(sprite = GFX.SpriteBank.Create("auspicioushelper_horse"));
    //sprite.Play("no");
    DebugConsole.Write("Here");
    Collider = new Hitbox(16,16,-8,-8);
  }
  enum Dir{
    Right,Up,Left,Down
  }
  static Dir next(Dir x)=>x switch{
    Dir.Right=>Dir.Up, Dir.Up=>Dir.Left, Dir.Left=> Dir.Down, Dir.Down=>Dir.Right, _=>throw new Exception("lala")
  };
  static Int2 dirToVec(Dir x)=>x switch{
    Dir.Right=>new(1,0), Dir.Up=>new(0,-1), Dir.Left=>new(-1,0), Dir.Down=>new(0,1), _=>throw new Exception("lala")
  };
  static Dir prev(Dir x)=>next(next(next(x)));
  public void OnTree(){
    HashSet<Int2> visited = new();
    int iturns = 0;
    Int2 at = Int2.Round(Position);
    FloatRect bounds = (IntRect)(Engine.Scene as Level).Bounds;

    if(SolidMiptree.TestPixel(at,CollisionDirection.yes)!=null) goto completed;
    while(bounds.CollidePoint(at) && SolidMiptree.TestPixel(at,CollisionDirection.yes)==null)at+=new Int2(0,1);
    if(SolidMiptree.TestPixel(at,CollisionDirection.yes)==null)return;
    Dir cdir = Dir.Right;
    for(int i=0; i<10000; i++){
      if(!bounds.CollidePoint(at)) return;
      if(visited.Contains(at)) goto completed;
      visited.Add(at);
      if(SolidMiptree.TestPixel(at+dirToVec(next(cdir)),CollisionDirection.yes)!=null){
        at=at+dirToVec(cdir = next(cdir));;
        iturns++;
      } else if(SolidMiptree.TestPixel(at+dirToVec(cdir), CollisionDirection.yes)!=null){
        at=at+dirToVec(cdir);
      } else if(SolidMiptree.TestPixel(at+dirToVec(next(cdir))+dirToVec(cdir),CollisionDirection.yes)!=null){
        at=at+dirToVec(next(cdir))+dirToVec(cdir);
        cdir=next(cdir);
        iturns++;
      }else if(SolidMiptree.TestPixel(at+dirToVec(prev(cdir)),CollisionDirection.yes)!=null){
        at=at+dirToVec(cdir = prev(cdir));
        iturns--;
      } else {
        //special case for 1-wide solid things
        while(visited.Contains(at))at-=dirToVec(cdir);
        cdir = prev(cdir);
        iturns--;
      }
    }
    DebugConsole.Write("Maximum iters passed");
    return;
    completed:
      if(iturns>=4){
        ChannelState.SetChannel(channel,1);
        sprite.Play("yes");
        Audio.Play("event:/game/07_summit/checkpoint_confetti");
        Celeste.Freeze(0.05f);
        Add(new Coroutine(RotateRoutine()));
        (Engine.Instance.scene as Level).Flash(Color.Green*0.4f);
        Remove(Get<NeedsAcceleratorComp>());
      }
  }
  IEnumerator RotateRoutine(){
    float amount = 0;
    while((amount=amount+Engine.DeltaTime)<1){
      sprite.Rotation = Util.SineInOut(amount)*MathF.PI*2;
      yield return null;
    }
    sprite.Rotation = 0;
  }
}
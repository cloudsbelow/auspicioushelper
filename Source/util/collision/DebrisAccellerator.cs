


using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked(true)]
public class FastDebris:Actor{
  Int2 intpos=>Int2.Round(Position);
  
  public class DebrisOptions{
    public int depth = 2000;
    public Action update;
    public Collision collideH;
    public Collision collideV;
    public Collision squish;
  }
  public FastDebris(Vector2 pos, DebrisOptions o):base(Int2.Round(pos)){
    Tag = Tags.Persistent;
    Depth = 2000;
    SquishCallback = o.squish??defaultSquish;
    Active = false;
    hooks.enable();
  }
  void defaultSquish(CollisionData d){
    RemoveSelf();
  }
  void updateCollision(bool useStructure){

  }

  public static void UpdateDebris(Level lv){
    var l = lv.Tracker.GetEntities<FastDebris>();
    if(l.Count<24)foreach(FastDebris e in l)e.updateCollision(false);
    else {
      SolidMiptree.Construct(lv,((IntRect)lv.Bounds).expandAll_(32));
    }
  } 

  static bool Hook(On.Celeste.Actor.orig_MoveHExact orig, Actor a, int move, Collision oncollide, Solid pusher){
    if(a is FastDebris d && pusher != null){
      d.NaiveMove(Vector2.UnitX*move);
      return false;
    }
    return orig(a,move,oncollide,pusher);
  }
  static bool Hook(On.Celeste.Actor.orig_MoveVExact orig, Actor a, int move, Collision oncollide, Solid pusher){
    if(a is FastDebris d && pusher != null){
      d.NaiveMove(Vector2.UnitY*move);
      return false;
    }
    return orig(a,move,oncollide,pusher);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Actor.MoveHExact+=Hook;
    On.Celeste.Actor.MoveVExact+=Hook;
  },()=>{
    On.Celeste.Actor.MoveHExact-=Hook;
    On.Celeste.Actor.MoveVExact-=Hook;
  });
}
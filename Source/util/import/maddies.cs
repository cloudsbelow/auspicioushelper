

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

//also gravityhelper 
public static class MaddiesIop{
  public static Type jt;
  public static Type dt;
  public static Type at;
  public static Type samah;
  public static Type fakesound;
  public static Util.FieldHelper<bool> side;
  public static Util.FieldHelper<Solid> playerInteractingSolid;
  public static Entity CollideFirstSidewaysJt(this Entity e, Vector2 at, bool leftToRight){
    if(jt==null || !e.Scene.Tracker.Entities.TryGetValue(jt, out var li) || li is null) return null;
    foreach(var j in li){
      if(!Collide.Check(e, j) && Collide.Check(e, j, at) && (side.get(j)!=leftToRight)) return j;
    }
    return null;
  }
  public static IEnumerable<Entity> CollideAllSidewaysJt(this Entity e, Vector2 at, bool leftToRight){
    if(jt==null || !e.Scene.Tracker.Entities.TryGetValue(jt, out var li) || li is null) yield break;
    foreach(var j in li){
      if(!Collide.Check(e, j) && Collide.Check(e, j, at) && (side.get(j)!=leftToRight)) yield return j;
    }
  }
  
  [OnLoad]
  [ResetEvents.RunOn(ResetEvents.Times.LvlCleanup)]
  static void Get(){
    jt = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.SidewaysJumpThru");
    dt = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru");
    at = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.AttachedSidewaysJumpThru");
    fakesound = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.SidewaysJumpThru+FakeCollidingSolid");
    samah = Util.getModdedType("GravityHelper","Celeste.Mod.GravityHelper.Entities.UpsideDownJumpThru");
    if(jt == null) return;
    side = new Util.FieldHelper<bool>(jt, "AllowLeftToRight", true);
    playerInteractingSolid = new(at,"playerInteractingSolid");
  }
}
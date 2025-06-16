


using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public interface IBooster{
  class SentinalBooster:Booster{
    public SentinalBooster():base(new EntityData(),Vector2.Zero){
    }
  }
  static SentinalBooster inst = (SentinalBooster)RuntimeHelpers.GetUninitializedObject(typeof(SentinalBooster));
  void PlayerBoosted(Player player, Vector2 direction);
  void PlayerReleased();
  void PlayerDied();
  void PlayerBoostEnded(Player player){}
  public static void startBoostPlayer(Player p, Entity s){
    hooks.enable();
    inst.Center = s.Center;
    lastUsed = (IBooster) s;
    p.Boost(inst);
  }
  static IBooster lastUsed;
  static void boostHandler(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player p, Vector2 dir){
    if(self is SentinalBooster){
      lastUsed.PlayerBoosted(p,dir);
    } else orig(self,p,dir);
  }
  static void releasedHandler(On.Celeste.Booster.orig_PlayerReleased orig, Booster self){
    if(self is SentinalBooster){
      lastUsed.PlayerReleased();
    } else orig(self);
  }
  static void dieHandler(On.Celeste.Booster.orig_PlayerDied orig, Booster self){
    if(self is SentinalBooster){
      try{
        lastUsed.PlayerDied();
      } catch(Exception){}
    } else orig(self);
  }
  static void boostEndHandler(On.Celeste.Player.orig_BoostEnd orig, Player p){
    if(p.CurrentBooster is SentinalBooster){
      lastUsed.PlayerBoostEnded(p);
    }
    orig(p);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Booster.PlayerBoosted += boostHandler;
    On.Celeste.Booster.PlayerDied += dieHandler;
    On.Celeste.Booster.PlayerReleased += releasedHandler;
    On.Celeste.Player.BoostEnd+=boostEndHandler;
  }, void ()=>{
    On.Celeste.Booster.PlayerBoosted -= boostHandler;
    On.Celeste.Booster.PlayerDied -= dieHandler;
    On.Celeste.Booster.PlayerReleased -= releasedHandler;
    On.Celeste.Player.BoostEnd-=boostEndHandler;
  },auspicioushelperModule.OnEnterMap);
}
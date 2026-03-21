


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
  static SentinalBooster inst = Util.GetUninitializedEntWithComp<SentinalBooster>();
  void PlayerBoosted(Player player, Vector2 direction);
  void PlayerReleased();
  void PlayerDied();
  void PlayerReplaces(){}
  void PlayerBoostEnded(Player player){}
  public static IBooster CurrentBooster(Player p){
    if(p.CurrentBooster is SentinalBooster sb) return lastUsed;
    else return null;
  }
  public static IBooster LastBooster(Player p){
    if(p.LastBooster is SentinalBooster sb) return lastUsed;
    else return null;
  }
  public static void startBoostPlayer(Player p, Entity s){
    ResetEvents.LazyEnable(typeof(IBooster));
    tryReleaseCurrent(p);
    locked = true;
    inst.Center = s.Center;
    lastUsed = (IBooster) s;
    p.Boost(inst);
  }
  public static void startRedBoostPlayer(Player p, Entity s){
    ResetEvents.LazyEnable(typeof(IBooster));
    tryReleaseCurrent(p);
    locked = true;
    inst.Center = s.Center;
    lastUsed = (IBooster) s;
    p.RedBoost(inst);
  }
  static void tryReleaseCurrent(Player p){
    CurrentBooster(p)?.PlayerReplaces();
    lastUsed = null;
  }
  bool autoMove=>true;
  [Import.SpeedrunToolIop.Static]
  private static IBooster lastUsed;
  private static bool locked=false;
  [ResetEvents.OnHook(typeof(Booster),nameof(Booster.PlayerBoosted))]
  static void boostHandler(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player p, Vector2 dir){
    if(self is SentinalBooster){
      lastUsed.PlayerBoosted(p,dir);
    } else orig(self,p,dir);
  }
  [ResetEvents.OnHook(typeof(Booster),nameof(Booster.PlayerReleased))]
  static void releasedHandler(On.Celeste.Booster.orig_PlayerReleased orig, Booster self){
    if(self is SentinalBooster){
      lastUsed.PlayerReleased();
    } else orig(self);
  }
  [ResetEvents.OnHook(typeof(Booster),nameof(Booster.PlayerDied))]
  static void dieHandler(On.Celeste.Booster.orig_PlayerDied orig, Booster self){
    if(self is SentinalBooster){
      try{
        lastUsed.PlayerDied();
      } catch(Exception){}
    } else orig(self);
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.BoostEnd))]
  static void boostEndHandler(On.Celeste.Player.orig_BoostEnd orig, Player p){
    if(p.CurrentBooster is SentinalBooster){
      lastUsed.PlayerBoostEnded(p);
    }
    orig(p);
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.Boost))]
  [ResetEvents.OnHook(typeof(Player),nameof(Player.RedBoost))]
  static void startBoostHook(Action<Player,Booster> orig, Player p, Booster b){
    if(!locked) tryReleaseCurrent(p);
    else locked=false;
    orig(p,b);
  }
}
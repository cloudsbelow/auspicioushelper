
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


[Tracked(true)]
public class UpdateHook:Component{
  public Action beforeAction=null;
  public Action afterAction=null;
  public bool updatedThisFrame = false;
  public static float TimeSinceTransMs=0;
  static List<Action> afterUpd = new();
  static bool updateBefore;
  static bool updateAfter;
  static bool updateAny;
  static public void AddAfterUpdate(Action action,bool causesEntUpd=true,bool needsEntUpd=false){
    afterUpd.Add(action);
    updateBefore|=needsEntUpd;
    updateAfter|=causesEntUpd;
  }
  static public void EnsureUpdateAny()=>updateAny = true;
  public UpdateHook(Action before=null, Action after=null):base(true,false){
    beforeAction=before;afterAction =after;
    hooks.enable();
  }
  public override void Update() {
    base.Update();
    updatedThisFrame = true;
  }
  internal static int framenum;
  public static Player cachedPlayer;
  static public Entity getFollowEnt(){
    if(cachedPlayer==null) return null;
    Player p=cachedPlayer;
    switch(cachedPlayer.StateMachine.state){
      case Player.StBoost:
        if(p.CurrentBooster is IBooster.SentinalBooster sb){
          return null;
        } else return p.CurrentBooster;
      case Player.StDreamDash:
        if(p.dreamBlock is TemplateDreamblockModifier.SentinalDb sdb){
          return sdb.lastEntity;
        } else return null;
      default: return null; 
    }
  }
  internal static void updateHook(On.Celeste.Level.orig_Update update, Level self){
    if(afterUpd.Count>0){
      foreach(var a in afterUpd) a();
      afterUpd.Clear();
      self.Entities.UpdateLists();
    }
    if(self.Paused){
      update(self);
      return;
    }
    cachedPlayer = self.Tracker.GetEntity<Player>();
    foreach(UpdateHook u in self.Tracker.GetComponents<UpdateHook>()){
      u.updatedThisFrame = false;
      if(u.beforeAction!=null)u.beforeAction();
    }
    framenum+=1; //doesn't matter if this overflows or anything <3
    update(self);
  
    FastDebris.UpdateDebris(self);
    if(TimeSinceTransMs<1000000)TimeSinceTransMs+=Engine.DeltaTime*1000;
    foreach(UpdateHook u in self.Tracker.GetComponents<UpdateHook>()){
      if(u.afterAction!=null)u.afterAction();
    }
    if(afterUpd.Count>0 || updateBefore || updateAfter || updateAny){
      if(updateBefore)self.Entities.UpdateLists();
      foreach(var a in afterUpd) a();
      afterUpd.Clear();
      if(updateAfter || (updateAny && !updateBefore))self.Entities.UpdateLists();
      updateBefore = (updateAny = (updateAfter = false));
    }
  }
  static PersistantAction clear;
  internal static HookManager hooks = new HookManager(()=>{
    On.Celeste.Level.Update+=updateHook;
    auspicioushelperModule.OnExitMap.enroll(clear=new PersistantAction(afterUpd.Clear));
  }, ()=>{
    On.Celeste.Level.Update-=updateHook;
    clear.remove();
  });
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


[Tracked(true)]
public class UpdateHook:Component{
  public class AfterUpdateLock:IDisposable{
    static int lockctr = 0;
    public AfterUpdateLock(){lockctr++;}
    void IDisposable.Dispose() {
      lockctr--;
    }
    public static bool locked=>lockctr>0;
    public static List<Action> afterUpd = new();
    static bool updateBefore = false;
    static bool updateAfter = false;
    static bool updateAny = false;
    static List<Tuple<Action, bool, bool>> enqd = new();
    static public void AddAfterUpdate(Action action,bool causesEntUpd=true,bool needsEntUpd=false){
      if(locked){
        enqd.Add(new(action, causesEntUpd, needsEntUpd));
        return;
      }
      afterUpd.Add(action);
      updateBefore|=needsEntUpd;
      updateAfter|=causesEntUpd;
    }
    static public void EnsureUpdateAny()=>updateAny = true;
    public static void ExtraUpdate(Level self){
      if(afterUpd.Count>0 || updateBefore || updateAfter || updateAny){
        //DebugConsole.Write($"Doign extra update {updateBefore} {updateAfter} ({updateAny})");
        while(afterUpd.Count>0){
          if(updateBefore){
            updateAny = (updateBefore = false);
            self.Entities.UpdateLists();
          }
          using(new AfterUpdateLock())foreach(var a in afterUpd) a();
          afterUpd.Clear();
          foreach(var t in enqd) AddAfterUpdate(t.Item1,t.Item2,t.Item3);
          enqd.Clear();
        }
        if(updateAfter||updateAny){
          updateAny = (updateAfter = false);
          self.Entities.UpdateLists();
        }
      }
    }
  }
  static public void AddAfterUpdate(Action action,bool causesEntUpd=true,bool needsEntUpd=false)=>AfterUpdateLock.AddAfterUpdate(action,causesEntUpd,needsEntUpd);
  public static void EnsureUpdateAny()=>AfterUpdateLock.EnsureUpdateAny();
  public Action beforeAction=null;
  public Action afterAction=null;
  public bool updatedThisFrame = false;
  public static float TimeSinceTransMs=0;

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
    cachedPlayer = self.Tracker.GetEntity<Player>();
    AfterUpdateLock.ExtraUpdate(self);
    if(self.Paused){
      update(self);
      return;
    }
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
    AfterUpdateLock.ExtraUpdate(self);
  }

  static PersistantAction clear;
  internal static HookManager hooks = new HookManager(()=>{
    On.Celeste.Level.Update+=updateHook;
    auspicioushelperModule.OnExitMap.enroll(clear=new PersistantAction(AfterUpdateLock.afterUpd.Clear));
  }, void ()=>{
    On.Celeste.Level.Update-=updateHook;
    clear.remove();
  });
}
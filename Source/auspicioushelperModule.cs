using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.auspicioushelper.iop;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper;

public class auspicioushelperModule : EverestModule {
  public static auspicioushelperModule Instance { get; private set; }

  public override Type SessionType => typeof(auspicioushelperModuleSession);
  public static auspicioushelperModuleSession Session => (auspicioushelperModuleSession) Instance._Session;
  public override Type SettingsType => typeof(auspicioushelperModuleSettings);
  public static auspicioushelperModuleSettings Settings => (auspicioushelperModuleSettings) Instance._Settings;
  public override Type SaveDataType => typeof(auspicioushelperModuleSaveData);
  public static auspicioushelperModuleSaveData SaveData=> (auspicioushelperModuleSaveData) Instance._SaveData;

  public auspicioushelperModule() {
    Instance = this;
    Logger.SetLogLevel(nameof(auspicioushelperModule), LogLevel.Info);
  }
  public static ActionList OnEnterMap = new ActionList();
  public static ActionList OnExitMap = new ActionList();
  public static ActionList OnReloadMap = new ActionList();
  public static ActionList OnNewScreen = new ActionList();
  public static ActionList OnReset = new ActionList();

  void OnTransition(Level level, LevelData next, Vector2 direction){
    Session.save();
    ChannelState.unwatchTemporary();
    MarkedRoomParser.clearDynamicRooms();

    OnReset.run();
    OnNewScreen.run();
  } 
  static void ChangerespawnHandler(On.Celeste.ChangeRespawnTrigger.orig_OnEnter orig, ChangeRespawnTrigger self, Player player){
    Session s = (self.Scene as Level).Session;
    Vector2? origPoint = s.RespawnPoint;
    orig(self, player);
    if(Session.respDat!=null) origPoint=null;
    Session.respDat=null;
    if(origPoint!=s.RespawnPoint)Session.save();
  }
  static void OnDie(Player player)=>MaterialPipe.onDie();
  static void LoadLevlHook(On.Celeste.Level.orig_LoadLevel orig, Level l, Player.IntroTypes playerIntro, bool isFromLoader = false){
    DebugConsole.Write($"{playerIntro}");
    if(playerIntro == Player.IntroTypes.Respawn){
      Session.load();
      ChannelState.unwatchAll();

      OnReset.run();
    }
    orig(l,playerIntro,isFromLoader);
  }
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnExit)]
  public static bool InFolderMod;
  static void OnEnter(Session session){
    OnExitMap.run();
    OnReset.run();
    OnNewScreen.run();
    OnReloadMap.run();
    OnEnterMap.run();
    try{
    InFolderMod = session?.MapData?.Filename!=null && 
      Path.Combine("Maps",session.MapData.Filename) is {} fpath && 
      Everest.Content.Get(fpath)?.Source?.Mod is {} Mod && 
      !string.IsNullOrWhiteSpace(Mod.PathDirectory);
    }catch(Exception){
      DebugConsole.Write($"Entering nonexistent area");
    }

    DebugConsole.Write($"\n\nEntering Map! Folder mod: {InFolderMod}");
    try{
      Session?.load();
      if(session?.MapData!=null){
        MapenterEv.Run(session.MapData);
        MarkedRoomParser.parseMapdata(session.MapData);
        DebugConsole.Write("Entered Level");
      } else {
        DebugConsole.Write("Session or mapdata null");
      }
    }catch(Exception ex){
      DebugConsole.Write("\n\n\nERROR IN MAPENTER:\n",ex.ToString());
      if(ex is DebugConsole.PassingException p) throw p;
    }
  }
  static void OnExit(Level l, LevelExit e, LevelExit.Mode m, Session s, HiresSnow h)=>OnExitMap.run();
  public static int CACHENUM;
  static void OnReload(bool silent){
    CACHENUM++;
    MapHider.handleReload(); 
    try {
      ChannelState.unwatchAll();
      if(Engine.Instance.scene is LevelLoader l){
        DebugConsole.Write("\n\nReloading Map!");
        OnReloadMap.run();
        MapenterEv.Run(l.Level.Session.MapData);
        MarkedRoomParser.parseMapdata(l.Level.Session.MapData);
      }
    } catch (Exception ex){
      if(ex is DebugConsole.PassingException p) throw p;
      else DebugConsole.Write($"reloading error: {ex}");
    }
    Session?.load();
  }

  public override void LoadContent(bool firstLoad) {
    base.LoadContent(firstLoad);
    SpeedrunToolIop.hooks.enable();
    CommunalHelperIop.load();
    ExtendedCameraIop.load();
    ImGui.load();
    DebugConsole.Write("Loading content");
  }
  public static void GiveUp(On.Celeste.Level.orig_GiveUp orig, Level l,int returnIndex, bool restartArea, bool minimal, bool showHint){
    ChannelState.clearChannels();
    orig(l,returnIndex,restartArea,minimal,showHint);
  }
  
  static void OnEnterEvent(Session session, bool fromsave)=>OnEnter(session);
  [OnLoad.ILHook(typeof(Commands),"LoadIdLevel")]
  static void OtherOnenter(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchNewobj<LevelLoader>(),
      itr=>itr.MatchCall<Engine>("set_Scene")
    )){
      c.EmitLdloc1();
      c.EmitDelegate(OnEnter);
    } else DebugConsole.WriteFailure("Could not add command onenter hook",false);
  }
  public override void Load() {
    Everest.Events.Level.OnTransitionTo += OnTransition;
    Everest.Events.Player.OnDie += OnDie;
    Everest.Events.Level.OnEnter += OnEnterEvent;
    Everest.Events.Level.OnExit += OnExit;
    Everest.Events.AssetReload.OnAfterReload += OnReload;
    On.Celeste.Level.GiveUp += GiveUp;
    On.Celeste.Level.LoadLevel += LoadLevlHook;
    On.Celeste.ChangeRespawnTrigger.OnEnter += ChangerespawnHandler;
    
    DebugConsole.Write("Loading");
    OnLoad.Run();
    //if(Settings.HideHelperMaps)MapHider.setHide();
    ResetEvents.Load();

    
    typeof(Anti0fIopExp).ModInterop();
    typeof(TemplateIopExp).ModInterop();
    typeof(ChannelIopExp).ModInterop();
    typeof(ChannelIop2).ModInterop();
    typeof(TemplateIop).ModInterop(); //for examples;
    
    TemplateIop.customClarify("auspicioushelper/ChannelMover",(l,d,o,e)=>{
      return new ChannelMover(e,o).makeComp();
    }); 
  }
  public override void Unload() {
    Everest.Events.Level.OnTransitionTo -= OnTransition;
    Everest.Events.Player.OnDie -= OnDie;
    Everest.Events.Level.OnEnter -= OnEnterEvent;
    Everest.Events.Level.OnExit -= OnExit;
    Everest.Events.AssetReload.OnAfterReload -= OnReload;
    On.Celeste.Level.GiveUp -= GiveUp;
    On.Celeste.Level.LoadLevel -= LoadLevlHook;
    On.Celeste.ChangeRespawnTrigger.OnEnter -= ChangerespawnHandler;

    HookManager.disableAll();
    DebugConsole.Close();
    MapHider.setUnhide();
  }
}
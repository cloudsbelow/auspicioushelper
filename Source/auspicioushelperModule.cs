using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.auspicioushelper.iop;
using Microsoft.Xna.Framework;
using Monocle;
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
  static void OnDie(Player player){
    ConditionalStrawb.handleDie(player);
    MaterialPipe.onDie();
  }
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
  static void OnEnter(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? position){
    OnExitMap.run();
    OnReset.run();
    OnNewScreen.run();
    OnReloadMap.run();
    OnEnterMap.run();
    InFolderMod = Path.Combine("Maps",session.MapData.Filename) is {} fpath && 
      Everest.Content.Get(fpath)?.Source?.Mod is {} Mod && 
      !string.IsNullOrWhiteSpace(Mod.PathDirectory);

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
      if(ex is DebugConsole.PassingException p) throw p;
      else DebugConsole.Write(ex.ToString());
    }
    orig(self,session,position);
  }
  static void OnExit(Level l, LevelExit e, LevelExit.Mode m, Session s, HiresSnow h)=>OnExitMap.run();
  static void OnReload(bool silent){
    MapHider.handleReload();
    try {
      ChannelState.unwatchAll();
      if(Engine.Instance.scene is LevelLoader l){
        OnReloadMap.run();
        DebugConsole.Write("\n\nReloading Map!");
        MapenterEv.Run(l.Level.Session.MapData);
        MarkedRoomParser.parseMapdata(l.Level.Session.MapData);
      }
      DebugConsole.Write(Engine.Scene?.ToString()??"null scene");
    } catch (Exception ex){
      if(ex is DebugConsole.PassingException p) throw p;
      else DebugConsole.Write($"reloading error: {ex}");
      Logger.Warn("auspicioushelper","Invalid state. Ausp prevented some potential errors but other mods may not.\n");
    }
    Session?.load();
  }

  public override void LoadContent(bool firstLoad) {
    base.LoadContent(firstLoad);
    SpeedrunToolIop.hooks.enable();
    CommunalHelperIop.load();
    ExtendedCameraIop.load();
    DebugConsole.Write("Loading content");
  }
  public static void GiveUp(On.Celeste.Level.orig_GiveUp orig, Level l,int returnIndex, bool restartArea, bool minimal, bool showHint){
    ChannelState.clearChannels();
    orig(l,returnIndex,restartArea,minimal,showHint);
  }
  
  public override void Load() {
    Everest.Events.Level.OnTransitionTo += OnTransition;
    Everest.Events.Player.OnDie += OnDie;
    On.Celeste.LevelLoader.ctor += OnEnter;
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
    typeof(TemplateIop).ModInterop(); //for examples;
    
    TemplateIop.customClarify("auspicioushelper/ChannelMover",(l,d,o,e)=>{
      return new ChannelMover(e,o).makeComp();
    }); 
  }
  public override void Unload() {
    Everest.Events.Level.OnTransitionTo -= OnTransition;
    Everest.Events.Player.OnDie -= OnDie;
    On.Celeste.LevelLoader.ctor -= OnEnter;
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
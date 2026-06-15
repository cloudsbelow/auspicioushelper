using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Celeste.Editor;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.auspicioushelper.iop;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;

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


  [OnLoad.EverestEvent(typeof(Everest.Events.Level), nameof(Everest.Events.Level.OnTransitionTo))]
  void OnTransition(Level level, LevelData next, Vector2 direction){
    Session.save();
    ChannelState.unwatchTemporary(true);
    MarkedRoomParser.clearDynamicRooms();
    if(Session is {} s)s.transitions++;

    ResetEvents.OnLvlReset.run();
    ResetEvents.OnNewScreen.run();
  }

  [OnLoad.OnHook(typeof(ChangeRespawnTrigger),nameof(ChangeRespawnTrigger.OnEnter))]
  static void ChangerespawnHandler(On.Celeste.ChangeRespawnTrigger.orig_OnEnter orig, ChangeRespawnTrigger self, Player player){
    Session s = (self.Scene as Level).Session;
    Vector2? origPoint = s.RespawnPoint;
    orig(self, player);
    if(Session.respDat!=null) origPoint=null;
    Session.respDat=null;
    if(origPoint!=s.RespawnPoint)Session.save();
  }

  static string LastLvl;
  [OnLoad.OnHook(typeof(Level),nameof(Level.LoadLevel))]
  static void LoadLevlHook(On.Celeste.Level.orig_LoadLevel orig, Level l, Player.IntroTypes playerIntro, bool isFromLoader = false){
    DebugConsole.Write($"{playerIntro}");
    TemplateBehaviorChain.mainRoom.Clear();
    if(playerIntro == Player.IntroTypes.Respawn){
      Session.load(null);
      ChannelState.unwatchTemporary(false);
      UpdateHook.TimeSinceTransMs = 1000000;

      ResetEvents.OnLvlReset.run();
    }
    LastLvl = l.Session.LevelData.Name;
    orig(l,playerIntro,isFromLoader);
  }

  [ResetEvents.NullOn(ResetEvents.Times.LvlCleanup)]
  public static bool InFolderMod;
  static void SetFoldermod(Session s){
    try{
      InFolderMod = s?.MapData?.Filename!=null && 
        Path.Combine("Maps",s.MapData.Filename) is {} fpath && 
        Everest.Content.Get(fpath)?.Source?.Mod is {} Mod && 
        !string.IsNullOrWhiteSpace(Mod.PathDirectory);
    }catch(Exception){
      DebugConsole.Write($"Entering nonexistent area");
    }
  }
  static void OnEnter(Session session){
    ResetEvents.OnLvlCleanup.run();
    ResetEvents.OnNewAssets.run();
    ResetEvents.OnNewScreen.run();
    ResetEvents.OnLvlReset.run();

    SetFoldermod(session);
    DebugConsole.Write($"\n\nEntering Map! Folder mod: {InFolderMod}");
    try{
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
  [ResetEvents.NullOn(ResetEvents.Times.NewAssets)]
  static bool resetInDebug;
  static void OnDebugEnter(Session session){
    //We narrowly avoid disaster since SRT clears state on reload. If it did not,
    //the srt thjrough a debug map-occupying reload would cause the world to end.
    if(resetInDebug) OnEnter(session); //thjis case blows up if SRT keeps state
    else{
      ResetEvents.OnNewScreen.run();   //this case blows up if assets have changed
      ResetEvents.OnLvlReset.run();   
    }
  }

  [OnLoad.EverestEvent(typeof(Everest.Events.Level), nameof(Everest.Events.Level.OnExit))]
  static void OnExit(Level l, LevelExit e, LevelExit.Mode m, Session s, HiresSnow h){
    DebugConsole.Write("Level exit", m);
    ResetEvents.OnLvlReset.run();
    ResetEvents.OnNewScreen.run();
    if(m!=LevelExit.Mode.Restart && m!=LevelExit.Mode.GoldenBerryRestart){
      ResetEvents.OnNewAssets.run();
      ResetEvents.OnLvlCleanup.run();
    } else {
      ChannelState.clearChannels();
    }
  }

  public static int CACHENUM;
  [OnLoad.EverestEvent(typeof(Everest.Events.AssetReload), nameof(Everest.Events.AssetReload.OnAfterReload))]
  static void OnReload(bool silent){
    CACHENUM++;
    MapHider.handleReload(); 
    try {
      ChannelState.ClearAll();
      if(Engine.Instance.scene is LevelLoader l){
        SetFoldermod(l.Level.Session);
        DebugConsole.Write("\n\nReloading Map! In foldermod: ",InFolderMod);
        if(InFolderMod && Settings.ExtraMappingUtils.KillSessiondonotload){
          l.Level.Session.DoNotLoad?.Clear();
          Session.brokenTempaltes?.Clear();
        }
        ResetEvents.OnNewAssets.run();
        MapenterEv.Run(l.Level.Session.MapData);
        MarkedRoomParser.parseMapdata(l.Level.Session.MapData);
      } else if(Engine.Instance.scene is MapEditor editor){
        ResetEvents.OnNewAssets.run();
        resetInDebug = true;
      }
    } catch (Exception ex){
      if(ex is DebugConsole.PassingException p) throw p;
      else DebugConsole.Write($"reloading error: {ex}");
    }
    Session?.load(null);
  }
  
  [OnLoad.EverestEvent(typeof(Everest.Events.Level),nameof(Everest.Events.Level.OnEnter))]
  static void OnEnterEvent(Session session, bool fromsave){
    OnEnter(session);
    if(fromsave) Session?.load(session);
  }

  [OnLoad.ILHook(typeof(Commands),"LoadIdLevel")]
  static void OnEnterCommand(ILContext ctx)=>OnEnterManip(ctx, OnEnter);

  [OnLoad.ILHook(typeof(MapEditor),nameof(MapEditor.LoadLevel))]
  [OnLoad.ILHook(typeof(MapEditor),"orig_LoadLevel")]
  [OnLoad.ILHook(typeof(MapEditor),"MakeMapEditorBetter")]
  static void OnEnterDebug(ILContext ctx)=>OnEnterManip(ctx, OnDebugEnter);
  static void OnEnterManip(ILContext ctx, Action<Session> choice){
    ILCursor c = new(ctx);
    VariableDefinition v = new(ctx.Import(typeof(Vector2?)));
    c.Body.Variables.Add(v);
    bool flag = true;
    while(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchNewobj<LevelLoader>(),
      itr=>itr.MatchCall<Engine>("set_Scene")
    )){
      c.EmitStloc(v);
      c.EmitDup();
      c.EmitDelegate(choice);
      c.EmitLdloc(v);
      c.Index+=2;
      flag=false;
    } 
    if(flag) DebugConsole.WriteFailure("Could not add command onenter hook",false);
  }
  
  public override void Load() {
    DebugConsole.Write("Loading");
    OnLoad.Run();
    ResetEvents.Load();
    
    typeof(Anti0fIopExp).ModInterop();
    typeof(TemplateIopExp).ModInterop();
    typeof(ChannelIopExp).ModInterop();
    typeof(ChannelIopExp2).ModInterop();
    typeof(TemplateIop).ModInterop(); //for examples;
    
    TemplateIop.customClarify("auspicioushelper/ChannelMover",(l,d,o,e)=>{
      return new ChannelMover(e,o).makeComp();
    }); 
  }
  public override void LoadContent(bool firstLoad) {
    base.LoadContent(firstLoad);
    SpeedrunToolIop.hooks.enable();
    CommunalHelperIop.load();
    ExtendedCameraIop.load();
    ImGui.load();
    typeof(SpringTracker.FrosthelperSpring).ModInterop();
    DebugConsole.Write("Loading content");
  }
  public override void Unload() {
    HookManager.disableAll();
    DebugConsole.Close();
    MapHider.setUnhide();
  }
  [Command("auspdebug_enableAllHooks", "enables all lazily loaded hooks in auspicioushelper")]
  public static void EnableAllHooks(){
    ResetEvents.Hooks<auspicioushelperModule>.enableAll();   
  }
}
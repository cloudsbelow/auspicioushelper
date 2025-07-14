using System;
using System.Collections.Generic;
using System.Diagnostics;
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
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(auspicioushelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(auspicioushelperModule), LogLevel.Info);
#endif
    }
    public static ActionList OnEnterMap = new ActionList();
    public static ActionList OnNewScreen = new ActionList();
    public static ActionList OnReset = new ActionList();

    public static void tinyCleanup(){
        PortalGateH.intersections.Clear();
    }
    void OnTransition(Level level, LevelData next, Vector2 direction){
        Session.save();
        ChannelState.unwatchTemporary();
        tinyCleanup();
        UpdateHook.TimeSinceTransMs=0;
        MarkedRoomParser.clearDynamicRooms();

        OnReset.run();
        OnNewScreen.run();
    } 
    static void ChangerespawnHandler(On.Celeste.ChangeRespawnTrigger.orig_OnEnter orig, ChangeRespawnTrigger self, Player player){
        orig(self, player);
        //if ((!session.RespawnPoint.HasValue || session.RespawnPoint.Value != Target))
        Session session = (self.Scene as Level).Session;
        if(!session.RespawnPoint.HasValue || session.RespawnPoint.Value != self.Target){
            Session.save();
        }
    }
    static void OnDie(Player player){
        ConditionalStrawb.handleDie(player);
        MaterialPipe.onDie();
    }
    static void LoadLevlHook(On.Celeste.Level.orig_LoadLevel orig, Level l, Player.IntroTypes playerIntro, bool isFromLoader = false){
        DebugConsole.Write($"{playerIntro}");
        if(playerIntro == Player.IntroTypes.Respawn){
            Session.load(false);
            ChannelState.unwatchAll();
            tinyCleanup();

            OnReset.run();
        }
        orig(l,playerIntro,isFromLoader);
    }
    static void OnEnter(Session session, bool fromSave){
        UpdateHook.TimeSinceTransMs=0;
        try{
            ChannelState.unwatchAll();

            OnReset.run();
            OnNewScreen.run();
            OnEnterMap.run();
            
            Session?.load(!fromSave);
            //ChannelState.writeAll();

            if(session?.MapData!=null){
                //DebugConsole.Write($"Mapdata: {(session.MapData==null?"null":session.MapData.ToString())}");
                MarkedRoomParser.parseMapdata(session.MapData);
                DebugConsole.Write("Entered Level");
            } else {
                DebugConsole.Write("Session or mapdata null");
            }
        }catch(Exception ex){
            DebugConsole.Write(ex.ToString());
        }
    }
    static void OnReload(bool silent){
        //DebugConsole.Write($"reloaded {Everest.Content.Map.Count} {Settings.HideHelperMaps}");
        //foreach (ModAsset item in Everest.Content.Map.Values.Where((ModAsset asset) => asset.Type == typeof(AssetTypeMap)))
        //if(Settings.HideHelperMaps && !MapHider.isHiding)MapHider.hideListed();
        //DebugConsole.Write(Engine.Instance.scene.ToString());
        try {
            ChannelState.unwatchAll();
            if(Engine.Instance.scene is LevelLoader l){
                MarkedRoomParser.parseMapdata(l.Level.Session.MapData);
            }
            DebugConsole.Write(Engine.Scene?.ToString()??"null scene");
        } catch (Exception ex){
            Logger.Warn("auspicioushelper","I don't know what happened here maybe this log can illuminate\n"+ex.ToString());
        }
        if(Session != null){
            try{
                Session.load(false);
            } catch(Exception ex){
                DebugConsole.Write($"reloading error: {ex}");
            }
        }
    }

    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
        SpeedrunToolIop.hooks.enable();
        CommunalHelperIop.load();
        ExtendedCameraIop.load();
        MaterialPipe.setup();
        DebugConsole.Write("Loading content");
    }
    public static void GiveUp(On.Celeste.Level.orig_GiveUp orig, Level l,int returnIndex, bool restartArea, bool minimal, bool showHint){
        ChannelState.clearChannels();
        orig(l,returnIndex,restartArea,minimal,showHint);
    }
    
    public override void Load() {
        Everest.Events.Level.OnTransitionTo += OnTransition;
        Everest.Events.Player.OnDie += OnDie;
        Everest.Events.Level.OnEnter += OnEnter;
        Everest.Events.AssetReload.OnAfterReload += OnReload;
        On.Celeste.Level.GiveUp += GiveUp;
        On.Celeste.Level.LoadLevel += LoadLevlHook;

        On.Celeste.ChangeRespawnTrigger.OnEnter += ChangerespawnHandler;
        DebugConsole.Write("Loading");
        ConditionalStrawb.hooks.enable();
        SpringTracker.hooks.enable();
        ChannelState.hooks.enable();
        TemplateBehaviorChain.setup();
        MapHider.uncache();
        
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
        Everest.Events.Level.OnEnter -= OnEnter;
        Everest.Events.AssetReload.OnAfterReload -= OnReload;
        On.Celeste.Level.GiveUp -= GiveUp;
        On.Celeste.Level.LoadLevel -= LoadLevlHook;

        HookManager.disableAll();
        DebugConsole.Close();
        On.Celeste.ChangeRespawnTrigger.OnEnter -= ChangerespawnHandler;
    }
}
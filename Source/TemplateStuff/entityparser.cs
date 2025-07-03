


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;
public static class EntityParser{
  public enum Types{
    unable,
    platformbasic,
    platformdisobedient,
    unwrapped,
    template,
    basic,
    removeSMbasic,
    iopClarified,
    initiallyerrors,
  }
  public static LevelData DefaultLD;
  static Dictionary<string, Types> parseMap = new Dictionary<string, Types>();
  static Dictionary<string, Level.EntityLoader> loaders = new Dictionary<string, Level.EntityLoader>();
  internal static void clarify(string name, Types t, Level.EntityLoader loader){
    if(name.StartsWith("auspicioushelper")) return; //no trolling
    parseMap[name] = t;
    loaders[name] = loader;
  }
  internal static void clarify(List<string> names, Types t, Level.EntityLoader loader){
    foreach(var name in names) clarify(name, t, loader);
  }
  static Dictionary<string,Func<Level,LevelData,Vector2,EntityData,Component>> cloaders = new();
  static HashSet<string> permittedExamples = ["auspicioushelper/ChannelMover"];
  public static void clarifyComp(string name, Func<Level,LevelData,Vector2,EntityData,Component> comploader){
    if(name.StartsWith("auspicioushelper") && !permittedExamples.Contains(name)) return;
    parseMap[name] = Types.iopClarified;
    cloaders[name] = comploader;
    loaders[name] = createComp;
  }
  public static Entity createComp(Level l, LevelData d, Vector2 o, EntityData e){
    DebugConsole.Write("Creating component!");
    Component c = cloaders[e.Name](l,d,o,e);
    if(c!=null){
      currentParent.addEnt(new IopControlled(c));
    }
    return null;
  }
  public static Types generateLoader_(EntityData d, LevelData ld, Level l){
    if(!parseMap.TryGetValue(d.Name, out var etype) || (l!=null && etype == Types.initiallyerrors)){
      Level.EntityLoader loader = Level.EntityLoaders.GetValueOrDefault(d.Name)??skitzoGuess(d.Name);
      if(loader == null){
        parseMap[d.Name] = Types.unable;
        DebugConsole.Write($"No loader found for ${d.Name}");
        return Types.unable;
      }
      try{
        Entity t = loader(l,ld,Vector2.Zero,d);
        if(t is Template){
          etype = parseMap[d.Name] = Types.template;
        }else if(t is Platform){
          etype = parseMap[d.Name] = Types.platformbasic;
        }else if(t is Actor || t is ITemplateChild){
          etype = parseMap[d.Name] = Types.unwrapped;
        }else{
          etype = parseMap[d.Name] = Types.removeSMbasic;
        }
        loaders[d.Name] = loader;
        DebugConsole.Write($"{d.Name} auto-classified as {etype}");
      } catch(Exception ex){
        DebugConsole.Write($"Entityloader generation for {d.Name} failed: \n{ex}");
        etype = parseMap[d.Name] = l!=null?Types.unable:Types.initiallyerrors;
      }
    }
    return etype;
  }
  public static bool generateLoader(EntityData d, LevelData ld, Level l=null)=>generateLoader_(d,ld,l)!=Types.unable;
  public static bool generateLoader(EntityData d, LevelData ld, Level l, out Types typ){
    typ=generateLoader_(d,ld,l);
    return typ!=Types.unable;
  }
  public static Template currentParent {get; private set;} = null;
  public static Entity create(EntityData d, Level l, LevelData ld, Vector2 simoffset, Template t, string path){
    ld=ld??DefaultLD;
    if(!parseMap.TryGetValue(d.Name,out var etype)){
      DebugConsole.Write($"Encountered unknown {d.Name}");
      parseMap[d.Name] = etype = Types.initiallyerrors;
    }
    if(etype == Types.unable) return null;
    if(etype == Types.initiallyerrors){
      if(!generateLoader(d,ld,l)) return null;
      etype = parseMap[d.Name];
      if(etype == Types.unable || etype==Types.initiallyerrors) return null;
    }
    var loader = getLoader(d.Name);
    if(loader == null) return null;
    if(etype == Types.template){
      if(string.IsNullOrWhiteSpace(d.Attr("template")) && t.t.chain==null){
        DebugConsole.WriteFailure($"Empty template did not get culled from template room");
        goto done;
      }
    }
    
    currentParent = t;
    Entity e = loader(l,ld,simoffset,d);
    if(e==null) goto done;
    if(path!=null && EntityMarkingFlag.flagged.TryGetValue(path+$"/{d.ID}",out var ident)){
      new FoundEntity(d,ident).finalize(e);
    }
    switch(etype){
      case Types.platformbasic: case Types.platformdisobedient:
        if(e is Platform p){
          if(etype == Types.platformdisobedient)
            t.addEnt(new Wrappers.BasicPlatformDisobedient(p,t,simoffset+d.Position-t.virtLoc));
          t.addEnt(new Wrappers.BasicPlatform(p,t,simoffset+d.Position-t.virtLoc));
          goto done;
        }else{
          DebugConsole.Write("Wrongly classified!!! "+d.Name);
          goto done;
        }
      case Types.unwrapped: case Types.template:
        currentParent = null;
        return e;
      case Types.basic:
        if(e!=null){
          t.AddBasicEnt(e,simoffset+d.Position-t.virtLoc);
        }
        goto done;
      case Types.removeSMbasic:
        List<StaticMover> SMRemove = new List<StaticMover>();
        foreach(Component c in e.Components) if(c is StaticMover sm){
          new DynamicData(sm).Set("__auspiciousSM", new TriggerInfo.SmInfo(t,e));
          SMRemove.Add(sm);
          smhooks.enable();
        }
        foreach(StaticMover sm in SMRemove) e.Remove(sm);
        t.AddBasicEnt(e,simoffset+d.Position-t.virtLoc);
        goto done;
      default:
        goto done;
    }
    done:
      currentParent = null;
      return null;
  }
  static void triggerPlatformsHook(On.Celeste.StaticMover.orig_TriggerPlatform orig, StaticMover sm){
    var smd = new DynamicData(sm);
    if(smd.TryGet<TriggerInfo.SmInfo>("__auspiciousSM", out var info)){
      info.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(info);
    }
    else orig(sm);
  }
  static HookManager smhooks = new HookManager(()=>{
    On.Celeste.StaticMover.TriggerPlatform+=triggerPlatformsHook;
  },()=>{
    On.Celeste.StaticMover.TriggerPlatform-=triggerPlatformsHook;
  },auspicioushelperModule.OnEnterMap);
  static EntityParser(){
    DefaultLD = (LevelData)RuntimeHelpers.GetUninitializedObject(typeof(LevelData));
    DefaultLD.Name = "ohmygoodnessiwanttodie";

    clarify("dreamBlock",     Types.platformbasic, static (l,ld,offset,e) => new DreamBlock(e, offset));
    clarify("jumpThru",       Types.platformbasic, static (l,ld,offset,e) => new JumpThruW(e, offset));
    clarify("glider",         Types.unwrapped,     static (l,ld,offset,e) => new Glider(e, offset));
    clarify("seekerBarrier",  Types.platformbasic, static (l,ld,offset,e) => new SeekerBarrier(e, offset));

    clarify("spikesUp",            Types.removeSMbasic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Up));
    clarify("spikesDown",          Types.removeSMbasic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Down));
    clarify("spikesLeft",          Types.removeSMbasic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Left));
    clarify("spikesRight",         Types.removeSMbasic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Right));

    clarify("triggerSpikesUp",     Types.removeSMbasic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Up));
    clarify("triggerSpikesDown",   Types.removeSMbasic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Down));
    clarify("triggerSpikesLeft",   Types.removeSMbasic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Left));
    clarify("triggerSpikesRight",  Types.removeSMbasic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Right));
    
    clarify("spring", Types.removeSMbasic, static (l,ld,offset,e)=>new Spring(e,offset,Spring.Orientations.Floor));
    clarify("wallSpringLeft", Types.removeSMbasic, static (l,ld,offset,e)=>new Spring(e,offset,Spring.Orientations.WallLeft));
    clarify("wallSpringRight", Types.removeSMbasic, static (l,ld,offset,e)=>new Spring(e,offset,Spring.Orientations.WallRight));
    clarify("spinner", Types.unwrapped, static (l,ld,offset,e)=>new Wrappers.Spinner(e,offset));
    
    clarify("lamp",Types.basic,static (l,ld,offset,e)=>new Lamp(offset + e.Position, e.Bool("broken")));
    clarify("hangingLamp",Types.basic,static (l,ld,offset,e)=>new HangingLamp(e,offset+e.Position));
    clarify("seeker",Types.basic,static (l,d,o,e)=>new Seeker(e, o));
    clarify("dashSwitchH",Types.unwrapped,static (l,d,o,e)=>new Wrappers.DashSwitchW(e,o,new EntityID(d.Name,e.ID)));
    clarify("dashSwitchV",Types.unwrapped,static (l,d,o,e)=>new Wrappers.DashSwitchW(e,o,new EntityID(d.Name,e.ID)));
    clarify("lightning", Types.basic, static (l,d,o,e)=>{
      if(!e.Bool("perLevel") && l.Session.GetFlag("disable_lightning")) return null;
      LightningRenderer lr = l.Tracker.GetEntity<LightningRenderer>();
      if(lr!=null) lr.StartAmbience();
      return new Lightning(e,o);
    });
    clarify("bigSpinner", Types.unwrapped, static (l,ld,o,e)=>new Wrappers.Bumperw(e,o));

    clarify("refill",Types.unwrapped,static (l,ld,offset,e)=>(Entity) new Wrappers.RefillW(e,offset));
    clarify("strawberry",Types.unwrapped,static (l,ld,offset,e)=>{
      EntityID id = new EntityID(ld.Name,e.ID);
      DebugConsole.Write("Trying to template berry: "+ id.ToString());
      if(l.Session.DoNotLoad.Contains(id)) return null;
      return (Entity) new StrawbW(e,offset,new EntityID(ld.Name,e.ID));
    });
    clarify("cameraTargetTrigger", Types.basic, static (l,d,o,e)=>{
      string text2 = e.Attr("deleteFlag");
      if(string.IsNullOrEmpty(text2) || !l.Session.GetFlag(text2)){
        return new CameraTargetTrigger(e,o);
      } else return null;
    });

    clarify("movingPlatform",Types.platformbasic,static (l,d,o,e)=>{
      MovingPlatform movingPlatform = new MovingPlatform(e, o);
      if (e.Has("texture")) movingPlatform.OverrideTexture = e.Attr("texture");
      return movingPlatform;
    });
    clarify("blackGem",Types.basic,HookVanilla.HeartGem);
    clarify("wire",Types.unwrapped,static (l,d,o,e)=>new CWire(e,o));
    clarify("lightningBlock",Types.unwrapped,static (l,d,o,e)=>new LightningBreakerW(e,o));
    defaultModdedSetup();
  }
  public static Level.EntityLoader getLoader(string name){
    if(!loaders.TryGetValue(name, out var loader)){
      if(!Level.EntityLoaders.TryGetValue(name,out loader)){
        loader = skitzoGuess(name);
      }
    }
    return loader;
  }

  static Level.EntityLoader skitzoGuess(string name){
    switch(name){
      case "jumpThru": return static (l,d,o,e)=>new JumpthruPlatform(e, o);
      case "refill": return static (l,d,o,e)=>new Refill(e, o);
      case "infiniteStar": return static (l,d,o,e)=>new FlyFeather(e, o);
      case "strawberry": return static (l,d,o,e)=>new Strawberry(e, o, new EntityID(d.Name,e.ID));
      case "summitgem": return static (l,d,o,e)=>new SummitGem(e, o, new EntityID(d.Name,e.ID));
      case "fallingBlock": return static (l,d,o,e)=>new FallingBlock(e, o);
      case "zipMover": return static (l,d,o,e)=>new ZipMover(e, o);
      case "crumbleBlock": return static (l,d,o,e)=>new CrumblePlatform(e, o);
      case "dreamBlock": return static (l,d,o,e)=>new DreamBlock(e, o);
      case "touchSwitch": return static (l,d,o,e)=>new TouchSwitch(e, o);
      case "switchGate": return static (l,d,o,e)=>new SwitchGate(e, o);
      case "negaBlock": return static (l,d,o,e)=>new NegaBlock(e, o);
      case "key": return static (l,d,o,e)=>new Key(e, o, new EntityID(d.Name,e.ID));
      case "lockBlock": return static (l,d,o,e)=>new LockBlock(e, o, new EntityID(d.Name,e.ID));
      case "movingPlatform": return static (l,d,o,e)=>new MovingPlatform(e, o);
      case "blockField": return static (l,d,o,e)=>new BlockField(e, o);
      case "cloud": return static (l,d,o,e)=>new Cloud(e, o);
      case "booster": return static (l,d,o,e)=>new Booster(e, o);
      case "moveBlock": return static (l,d,o,e)=>new MoveBlock(e, o);
      case "light": return static (l,d,o,e)=>new PropLight(e, o);
      case "swapBlock": return static (l,d,o,e)=>new SwapBlock(e, o);
      case "torch": return static (l,d,o,e)=>new Torch(e, o, new EntityID(d.Name,e.ID));
      case "seekerBarrier": return static (l,d,o,e)=>new SeekerBarrier(e, o);
      case "theoCrystal": return static (l,d,o,e)=>new TheoCrystal(e, o);
      case "glider": return static (l,d,o,e)=>new Glider(e, o);
      case "theoCrystalPedestal": return static (l,d,o,e)=>new TheoCrystalPedestal(e, o);
      case "badelineBoost": return static (l,d,o,e)=>new BadelineBoostW(e, o);
      case "wallBooster": return static (l,d,o,e)=>new WallBooster(e, o);
      case "bounceBlock": return static (l,d,o,e)=>new BounceBlock(e, o);
      case "coreModeToggle": return static (l,d,o,e)=>new CoreModeToggle(e, o);
      case "iceBlock": return static (l,d,o,e)=>new IceBlock(e, o);
      case "fireBarrier": return static (l,d,o,e)=>new FireBarrier(e, o);
      case "eyebomb": return static (l,d,o,e)=>new Puffer(e, o);
      case "flingBird": return static (l,d,o,e)=>new FlingBird(e, o);
      case "flingBirdIntro": return static (l,d,o,e)=>new FlingBirdIntro(e, o);
      case "lightningBlock": return static (l,d,o,e)=>new LightningBreakerBox(e, o);
      case "sinkingPlatform": return static (l,d,o,e)=>new SinkingPlatform(e, o);
      case "friendlyGhost": return static (l,d,o,e)=>new AngryOshiro(e, o);
      case "seeker": return static (l,d,o,e)=>new Seeker(e, o);
      case "seekerStatue": return static (l,d,o,e)=>new SeekerStatue(e, o);
      case "slider": return static (l,d,o,e)=>new Slider(e, o);
      case "templeBigEyeball": return static (l,d,o,e)=>new TempleBigEyeball(e, o);
      case "crushBlock": return static (l,d,o,e)=>new CrushBlock(e, o);
      case "bigSpinner": return static (l,d,o,e)=>new Bumper(e, o);
      case "starJumpBlock": return static (l,d,o,e)=>new StarJumpBlock(e, o);
      case "floatySpaceBlock": return static (l,d,o,e)=>new FloatySpaceBlock(e, o);
      case "glassBlock": return static (l,d,o,e)=>new GlassBlock(e, o);
      case "goldenBlock": return static (l,d,o,e)=>new GoldenBlock(e, o);
      case "fireBall": return static (l,d,o,e)=>new FireBall(e, o);
      case "risingLava": return static (l,d,o,e)=>new RisingLava(e, o);
      case "sandwichLava": return static (l,d,o,e)=>new SandwichLava(e, o);
      case "killbox": return static (l,d,o,e)=>new Killbox(e, o);
      case "fakeHeart": return static (l,d,o,e)=>new FakeHeart(e, o);
      case "finalBoss": return static (l,d,o,e)=>new FinalBoss(e, o);
      case "finalBossMovingBlock": return static (l,d,o,e)=>new FinalBossMovingBlock(e, o);
      case "dashBlock": return static (l,d,o,e)=>new DashBlock(e, o, new EntityID(d.Name,e.ID));
      case "invisibleBarrier": return static (l,d,o,e)=>new InvisibleBarrier(e, o);
      case "exitBlock": return static (l,d,o,e)=>new ExitBlock(e, o);
      case "coverupWall": return static (l,d,o,e)=>new CoverupWall(e, o);
      case "crumbleWallOnRumble": return static (l,d,o,e)=>new CrumbleWallOnRumble(e, o, new EntityID(d.Name,e.ID));
      case "tentacles": return static (l,d,o,e)=>new ReflectionTentacles(e, o);
      case "playerSeeker": return static (l,d,o,e)=>new PlayerSeeker(e, o);
      case "chaserBarrier": return static (l,d,o,e)=>new ChaserBarrier(e, o);
      case "introCrusher": return static (l,d,o,e)=>new IntroCrusher(e, o);
      case "bridge": return static (l,d,o,e)=>new Bridge(e, o);
      case "bridgeFixed": return static (l,d,o,e)=>new BridgeFixed(e, o);
      case "bird": return static (l,d,o,e)=>new BirdNPC(e, o);
      case "introCar": return static (l,d,o,e)=>new IntroCar(e, o);
      case "memorial": return static (l,d,o,e)=>new Memorial(e, o);
      case "wire": return static (l,d,o,e)=>new Wire(e, o);
      case "cobweb": return static (l,d,o,e)=>new Cobweb(e, o);
      case "hahaha": return static (l,d,o,e)=>new Hahaha(e, o);
      case "bonfire": return static (l,d,o,e)=>new Bonfire(e, o);
      case "colorSwitch": return static (l,d,o,e)=>new ClutterSwitch(e, o);
      case "resortmirror": return static (l,d,o,e)=>new ResortMirror(e, o);
      case "towerviewer": return static (l,d,o,e)=>new Lookout(e, o);
      case "picoconsole": return static (l,d,o,e)=>new PicoConsole(e, o);
      case "wavedashmachine": return static (l,d,o,e)=>new WaveDashTutorialMachine(e, o);
      case "oshirodoor": return static (l,d,o,e)=>new MrOshiroDoor(e, o);
      case "templeMirrorPortal": return static (l,d,o,e)=>new TempleMirrorPortal(e, o);
      case "reflectionHeartStatue": return static (l,d,o,e)=>new ReflectionHeartStatue(e, o);
      case "resortRoofEnding": return static (l,d,o,e)=>new ResortRoofEnding(e, o);
      case "gondola": return static (l,d,o,e)=>new Gondola(e, o);
      case "birdForsakenCityGem": return static (l,d,o,e)=>new ForsakenCitySatellite(e, o);
      case "whiteblock": return static (l,d,o,e)=>new WhiteBlock(e, o);
      case "plateau": return static (l,d,o,e)=>new Plateau(e, o);
      case "soundSource": return static (l,d,o,e)=>new SoundSourceEntity(e, o);
      case "templeMirror": return static (l,d,o,e)=>new TempleMirror(e, o);
      case "templeEye": return static (l,d,o,e)=>new TempleEye(e, o);
      case "clutterCabinet": return static (l,d,o,e)=>new ClutterCabinet(e, o);
      case "floatingDebris": return static (l,d,o,e)=>new FloatingDebris(e, o);
      case "foregroundDebris": return static (l,d,o,e)=>new ForegroundDebris(e, o);
      case "moonCreature": return static (l,d,o,e)=>new MoonCreature(e, o);
      case "lightbeam": return static (l,d,o,e)=>new LightBeam(e, o);
      case "door": return static (l,d,o,e)=>new Door(e, o);
      case "trapdoor": return static (l,d,o,e)=>new Trapdoor(e, o);
      case "resortLantern": return static (l,d,o,e)=>new ResortLantern(e, o);
      case "water": return static (l,d,o,e)=>new Water(e, o);
      case "waterfall": return static (l,d,o,e)=>new WaterFall(e, o);
      case "bigWaterfall": return static (l,d,o,e)=>new BigWaterfall(e, o);
      case "clothesline": return static (l,d,o,e)=>new Clothesline(e, o);
      case "cliffflag": return static (l,d,o,e)=>new CliffFlags(e, o);
      case "cliffside_flag": return static (l,d,o,e)=>new CliffsideWindFlag(e, o);
      case "flutterbird": return static (l,d,o,e)=>new FlutterBird(e, o);
      case "SoundTest3d": return static (l,d,o,e)=>new _3dSoundTest(e, o);
      case "SummitBackgroundManager": return static (l,d,o,e)=>new AscendManager(e, o);
      case "summitGemManager": return static (l,d,o,e)=>new SummitGemManager(e, o);
      case "heartGemDoor": return static (l,d,o,e)=>new HeartGemDoor(e, o);
      case "summitcheckpoint": return static (l,d,o,e)=>new SummitCheckpoint(e, o);
      case "summitcloud": return static (l,d,o,e)=>new SummitCloud(e, o);
      case "coreMessage": return static (l,d,o,e)=>new CoreMessage(e, o);
      case "playbackTutorial": return static (l,d,o,e)=>new PlayerPlayback(e, o);
      case "playbackBillboard": return static (l,d,o,e)=>new PlaybackBillboard(e, o);
      case "cutsceneNode": return static (l,d,o,e)=>new CutsceneNode(e, o);
      case "kevins_pc": return static (l,d,o,e)=>new KevinsPC(e, o);
      case "templeGate": return static (l,d,o,e)=>new TempleGate(e,o, d.Name);

      case "eventTrigger": return static (l,d,o,e)=>new EventTrigger(e, o);
      case "musicFadeTrigger": return static (l,d,o,e)=>new MusicFadeTrigger(e, o);
      case "musicTrigger": return static (l,d,o,e)=>new MusicTrigger(e, o);
      case "altMusicTrigger": return static (l,d,o,e)=>new AltMusicTrigger(e, o);
      case "cameraOffsetTrigger": return static (l,d,o,e)=>new CameraOffsetTrigger(e, o);
      case "lightFadeTrigger": return static (l,d,o,e)=>new LightFadeTrigger(e, o);
      case "bloomFadeTrigger": return static (l,d,o,e)=>new BloomFadeTrigger(e, o);
      case "cameraAdvanceTargetTrigger": return static (l,d,o,e)=>new CameraAdvanceTargetTrigger(e, o);
      case "respawnTargetTrigger": return static (l,d,o,e)=>new RespawnTargetTrigger(e, o);
      case "changeRespawnTrigger": return static (l,d,o,e)=>new ChangeRespawnTrigger(e, o);
      case "windTrigger": return static (l,d,o,e)=>new WindTrigger(e, o);
      case "windAttackTrigger": return static (l,d,o,e)=>new WindAttackTrigger(e, o);
      case "oshiroTrigger": return static (l,d,o,e)=>new OshiroTrigger(e, o);
      case "interactTrigger": return static (l,d,o,e)=>new InteractTrigger(e, o);
      case "checkpointBlockerTrigger": return static (l,d,o,e)=>new CheckpointBlockerTrigger(e, o);
      case "lookoutBlocker": return static (l,d,o,e)=>new LookoutBlocker(e, o);
      case "stopBoostTrigger": return static (l,d,o,e)=>new StopBoostTrigger(e, o);
      case "noRefillTrigger": return static (l,d,o,e)=>new NoRefillTrigger(e, o);
      case "ambienceParamTrigger": return static (l,d,o,e)=>new AmbienceParamTrigger(e, o);
      case "creditsTrigger": return static (l,d,o,e)=>new CreditsTrigger(e, o);
      case "goldenBerryCollectTrigger": return static (l,d,o,e)=>new GoldBerryCollectTrigger(e, o);
      case "moonGlitchBackgroundTrigger": return static (l,d,o,e)=>new MoonGlitchBackgroundTrigger(e, o);
      case "blackholeStrength": return static (l,d,o,e)=>new BlackholeStrengthTrigger(e, o);
      case "birdPathTrigger": return static (l,d,o,e)=>new BirdPathTrigger(e, o);
      case "spawnFacingTrigger": return static (l,d,o,e)=>new SpawnFacingTrigger(e, o);
      case "detachFollowersTrigger": return static (l,d,o,e)=>new DetachStrawberryTrigger(e, o);
      case "rumbleTrigger": return static (l,d,o,e)=>new RumbleTrigger(e,o,new EntityID(d.Name,e.ID+10000000));
      case "minitextboxTrigger": return static (l,d,o,e)=>new MiniTextboxTrigger(e,o,new EntityID(d.Name,e.ID+10000000));

    }
    return null;
  }
  public static void defaultModdedSetup(){
    FrostHelperStuff.setup();
  }
}
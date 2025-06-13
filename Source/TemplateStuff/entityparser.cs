


using System;
using System.Collections.Generic;
using System.Dynamic;
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
    basic,
    removeSMbasic,
    initiallyerrors,
  }
  static Dictionary<string, Types> parseMap = new Dictionary<string, Types>();
  static Dictionary<string, Level.EntityLoader> loaders = new Dictionary<string, Level.EntityLoader>();
  internal static void clarify(string name, Types t, Level.EntityLoader loader){
    if(name.StartsWith("auspicioushelper")) return; //no trolling
    parseMap[name] = t;
    loaders[name] = loader;
  }
  public static bool generateLoader(EntityData d, LevelData ld = null, Level l = null){
    if(!parseMap.TryGetValue(d.Name, out var etype) || (l!=null && etype == Types.initiallyerrors)){
      Level.EntityLoader loader = Level.EntityLoaders.GetValueOrDefault(d.Name)??skitzoGuess(d.Name);
      if(loader == null){
        parseMap[d.Name] = Types.unable;
        DebugConsole.Write($"No loader found for ${d.Name}");
        return false;
      }
      try{
        Entity t = loader(l,ld,Vector2.Zero,d);
        if(t is Platform){
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
    return etype!=Types.unable;
  }
  public static Template currentParent {get; private set;} = null;
  public static Entity create(EntityData d, Level l, LevelData ld, Vector2 simoffset, Template t, string path){
    if(!parseMap.TryGetValue(d.Name,out var etype) || etype == Types.unable){
      return null;
    }
    if(etype == Types.initiallyerrors){
      if(!generateLoader(d,ld,l)) return null;
      etype = parseMap[d.Name];
      if(etype == Types.unable || etype==Types.initiallyerrors) return null;
    }
    
    var loader = getLoader(d.Name);
    if(loader == null) return null;
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
      case Types.unwrapped:
        return e;
      case Types.basic:
        if(e!=null){
          t.AddBasicEnt(e,simoffset+d.Position-t.virtLoc);
        }
        goto done;
      case Types.removeSMbasic:
        List<StaticMover> SMRemove = new List<StaticMover>();
        foreach(Component c in e.Components) if(c is StaticMover sm){
          new DynamicData(sm).Set("__auspiciousTParent", t);
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
    if(smd.TryGet<Template>("__auspiciousTParent", out var parent)){
      parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(sm);
    }
    else orig(sm);
  }
  static HookManager smhooks = new HookManager(()=>{
    On.Celeste.StaticMover.TriggerPlatform+=triggerPlatformsHook;
  },()=>{
    On.Celeste.StaticMover.TriggerPlatform-=triggerPlatformsHook;
  },auspicioushelperModule.OnEnterMap);
  static EntityParser(){
    parseMap["dreamBlock"] = Types.platformbasic;
    loaders["dreamBlock"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new DreamBlock(e,offset);
    parseMap["jumpThru"] = Types.platformbasic;
    loaders["jumpThru"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new JumpThruW(e,offset);
    parseMap["glider"] = Types.unwrapped;
    loaders["glider"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new Glider(e,offset);
    parseMap["seekerBarrier"] = Types.platformbasic;
    loaders["seekerBarrier"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new SeekerBarrier(e,offset);
    
    parseMap["spikesUp"] = Types.removeSMbasic;
    loaders["spikesUp"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new Spikes(e,offset,Spikes.Directions.Up);
    parseMap["spikesDown"] = Types.removeSMbasic;
    loaders["spikesDown"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new Spikes(e,offset,Spikes.Directions.Down);
    parseMap["spikesLeft"] = Types.removeSMbasic;
    loaders["spikesLeft"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new Spikes(e,offset,Spikes.Directions.Left);
    parseMap["spikesRight"] = Types.removeSMbasic;
    loaders["spikesRight"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new Spikes(e,offset,Spikes.Directions.Right);
    parseMap["triggerSpikesUp"] = Types.removeSMbasic;
    loaders["triggerSpikesUp"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new TriggerSpikes(e,offset,TriggerSpikes.Directions.Up);
    parseMap["triggerSpikesDown"] = Types.removeSMbasic;
    loaders["triggerSpikesDown"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new TriggerSpikes(e,offset,TriggerSpikes.Directions.Down);
    parseMap["triggerSpikesLeft"] = Types.removeSMbasic;
    loaders["triggerSpikesLeft"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new TriggerSpikes(e,offset,TriggerSpikes.Directions.Left);
    parseMap["triggerSpikesRight"] = Types.removeSMbasic;
    loaders["triggerSpikesRight"] = static (Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new TriggerSpikes(e,offset,TriggerSpikes.Directions.Right);
    clarify("spring", Types.removeSMbasic, (Level l, LevelData ld, Vector2 offset, EntityData e)=>new Spring(e,offset,Spring.Orientations.Floor));
    clarify("wallSpringLeft", Types.removeSMbasic, (Level l, LevelData ld, Vector2 offset, EntityData e)=>new Spring(e,offset,Spring.Orientations.WallLeft));
    clarify("wallSpringRight", Types.removeSMbasic, (Level l, LevelData ld, Vector2 offset, EntityData e)=>new Spring(e,offset,Spring.Orientations.WallRight));
    clarify("spinner", Types.unwrapped, static (Level l, LevelData ld, Vector2 offset, EntityData e)=>new Wrappers.Spinner(e,offset));
    
    clarify("lamp",Types.basic,(Level l, LevelData ld, Vector2 offset, EntityData e)=>new Lamp(offset + e.Position, e.Bool("broken")));
    clarify("hangingLamp",Types.basic,(Level l, LevelData ld, Vector2 offset, EntityData e)=>new HangingLamp(e,offset+e.Position));
    clarify("seeker",Types.basic,static (Level l, LevelData d, Vector2 o, EntityData e)=>new Seeker(e, o));
    clarify("dashSwitchH",Types.unwrapped,static (Level l, LevelData d, Vector2 o, EntityData e)=>new Wrappers.DashSwitchW(e,o,new EntityID(d.Name,e.ID)));
    clarify("dashSwitchV",Types.unwrapped,static (Level l, LevelData d, Vector2 o, EntityData e)=>new Wrappers.DashSwitchW(e,o,new EntityID(d.Name,e.ID)));
    clarify("lightning", Types.basic, static (Level l, LevelData d, Vector2 o, EntityData e)=>{
      if(!e.Bool("perLevel") && l.Session.GetFlag("disable_lightning")) return null;
      LightningRenderer lr = l.Tracker.GetEntity<LightningRenderer>();
      if(lr!=null) lr.StartAmbience();
      return new Lightning(e,o);
    });
    clarify("bigSpinner", Types.unwrapped, static (Level l, LevelData ld, Vector2 o, EntityData e)=>new Wrappers.Bumperw(e,o));

    clarify("refill",Types.unwrapped,(Level l, LevelData ld, Vector2 offset, EntityData e)=>(Entity) new Wrappers.RefillW(e,offset));
    clarify("strawberry",Types.unwrapped,(Level l, LevelData ld, Vector2 offset, EntityData e)=>{
      EntityID id = new EntityID(ld.Name,e.ID);
      DebugConsole.Write("Trying to template berry: "+ id.ToString());
      if(l.Session.DoNotLoad.Contains(id)) return null;
      return (Entity) new StrawbW(e,offset,new EntityID(ld.Name,e.ID));
    });

    clarify("movingPlatform",Types.platformbasic,static(Level l, LevelData d, Vector2 o, EntityData e)=>{
      MovingPlatform movingPlatform = new MovingPlatform(e, o);
      if (e.Has("texture")) movingPlatform.OverrideTexture = e.Attr("texture");
      return movingPlatform;
    });
    clarify("blackGem",Types.basic,HookVanilla.HeartGem);
    clarify("wire",Types.unwrapped,static(Level l, LevelData d, Vector2 o, EntityData e)=>new CWire(e,o));
    clarify("lightningBlock",Types.unwrapped,(Level l, LevelData d, Vector2 o, EntityData e)=>new LightningBreakerW(e,o));
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
      case "jumpThru": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new JumpthruPlatform(e, o);
      case "refill": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Refill(e, o);
      case "infiniteStar": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FlyFeather(e, o);
      case "strawberry": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Strawberry(e, o, new EntityID(d.Name,e.ID));
      case "summitgem": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SummitGem(e, o, new EntityID(d.Name,e.ID));
      case "fallingBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FallingBlock(e, o);
      case "zipMover": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ZipMover(e, o);
      case "crumbleBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CrumblePlatform(e, o);
      case "dreamBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new DreamBlock(e, o);
      case "touchSwitch": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TouchSwitch(e, o);
      case "switchGate": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SwitchGate(e, o);
      case "negaBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new NegaBlock(e, o);
      case "key": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Key(e, o, new EntityID(d.Name,e.ID));
      case "lockBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new LockBlock(e, o, new EntityID(d.Name,e.ID));
      case "movingPlatform": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new MovingPlatform(e, o);
      case "blockField": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new BlockField(e, o);
      case "cloud": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Cloud(e, o);
      case "booster": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Booster(e, o);
      case "moveBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new MoveBlock(e, o);
      case "light": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new PropLight(e, o);
      case "swapBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SwapBlock(e, o);
      case "torch": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Torch(e, o, new EntityID(d.Name,e.ID));
      case "seekerBarrier": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SeekerBarrier(e, o);
      case "theoCrystal": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TheoCrystal(e, o);
      case "glider": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Glider(e, o);
      case "theoCrystalPedestal": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TheoCrystalPedestal(e, o);
      case "badelineBoost": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new BadelineBoostW(e, o);
      case "wallBooster": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new WallBooster(e, o);
      case "bounceBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new BounceBlock(e, o);
      case "coreModeToggle": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CoreModeToggle(e, o);
      case "iceBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new IceBlock(e, o);
      case "fireBarrier": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FireBarrier(e, o);
      case "eyebomb": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Puffer(e, o);
      case "flingBird": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FlingBird(e, o);
      case "flingBirdIntro": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FlingBirdIntro(e, o);
      case "lightningBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new LightningBreakerBox(e, o);
      case "sinkingPlatform": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SinkingPlatform(e, o);
      case "friendlyGhost": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new AngryOshiro(e, o);
      case "seeker": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Seeker(e, o);
      case "seekerStatue": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SeekerStatue(e, o);
      case "slider": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Slider(e, o);
      case "templeBigEyeball": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TempleBigEyeball(e, o);
      case "crushBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CrushBlock(e, o);
      case "bigSpinner": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Bumper(e, o);
      case "starJumpBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new StarJumpBlock(e, o);
      case "floatySpaceBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FloatySpaceBlock(e, o);
      case "glassBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new GlassBlock(e, o);
      case "goldenBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new GoldenBlock(e, o);
      case "fireBall": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FireBall(e, o);
      case "risingLava": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new RisingLava(e, o);
      case "sandwichLava": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SandwichLava(e, o);
      case "killbox": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Killbox(e, o);
      case "fakeHeart": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FakeHeart(e, o);
      case "finalBoss": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FinalBoss(e, o);
      case "finalBossMovingBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FinalBossMovingBlock(e, o);
      case "dashBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new DashBlock(e, o, new EntityID(d.Name,e.ID));
      case "invisibleBarrier": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new InvisibleBarrier(e, o);
      case "exitBlock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ExitBlock(e, o);
      case "coverupWall": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CoverupWall(e, o);
      case "crumbleWallOnRumble": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CrumbleWallOnRumble(e, o, new EntityID(d.Name,e.ID));
      case "tentacles": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ReflectionTentacles(e, o);
      case "playerSeeker": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new PlayerSeeker(e, o);
      case "chaserBarrier": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ChaserBarrier(e, o);
      case "introCrusher": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new IntroCrusher(e, o);
      case "bridge": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Bridge(e, o);
      case "bridgeFixed": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new BridgeFixed(e, o);
      case "bird": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new BirdNPC(e, o);
      case "introCar": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new IntroCar(e, o);
      case "memorial": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Memorial(e, o);
      case "wire": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Wire(e, o);
      case "cobweb": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Cobweb(e, o);
      case "hahaha": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Hahaha(e, o);
      case "bonfire": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Bonfire(e, o);
      case "colorSwitch": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ClutterSwitch(e, o);
      case "resortmirror": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ResortMirror(e, o);
      case "towerviewer": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Lookout(e, o);
      case "picoconsole": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new PicoConsole(e, o);
      case "wavedashmachine": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new WaveDashTutorialMachine(e, o);
      case "oshirodoor": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new MrOshiroDoor(e, o);
      case "templeMirrorPortal": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TempleMirrorPortal(e, o);
      case "reflectionHeartStatue": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ReflectionHeartStatue(e, o);
      case "resortRoofEnding": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ResortRoofEnding(e, o);
      case "gondola": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Gondola(e, o);
      case "birdForsakenCityGem": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ForsakenCitySatellite(e, o);
      case "whiteblock": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new WhiteBlock(e, o);
      case "plateau": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Plateau(e, o);
      case "soundSource": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SoundSourceEntity(e, o);
      case "templeMirror": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TempleMirror(e, o);
      case "templeEye": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TempleEye(e, o);
      case "clutterCabinet": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ClutterCabinet(e, o);
      case "floatingDebris": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FloatingDebris(e, o);
      case "foregroundDebris": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ForegroundDebris(e, o);
      case "moonCreature": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new MoonCreature(e, o);
      case "lightbeam": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new LightBeam(e, o);
      case "door": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Door(e, o);
      case "trapdoor": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Trapdoor(e, o);
      case "resortLantern": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new ResortLantern(e, o);
      case "water": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Water(e, o);
      case "waterfall": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new WaterFall(e, o);
      case "bigWaterfall": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new BigWaterfall(e, o);
      case "clothesline": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new Clothesline(e, o);
      case "cliffflag": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CliffFlags(e, o);
      case "cliffside_flag": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CliffsideWindFlag(e, o);
      case "flutterbird": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new FlutterBird(e, o);
      case "SoundTest3d": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new _3dSoundTest(e, o);
      case "SummitBackgroundManager": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new AscendManager(e, o);
      case "summitGemManager": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SummitGemManager(e, o);
      case "heartGemDoor": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new HeartGemDoor(e, o);
      case "summitcheckpoint": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SummitCheckpoint(e, o);
      case "summitcloud": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new SummitCloud(e, o);
      case "coreMessage": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CoreMessage(e, o);
      case "playbackTutorial": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new PlayerPlayback(e, o);
      case "playbackBillboard": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new PlaybackBillboard(e, o);
      case "cutsceneNode": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new CutsceneNode(e, o);
      case "kevins_pc": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new KevinsPC(e, o);
      case "templeGate": return static (Level l, LevelData d, Vector2 o, EntityData e)=>new TempleGate(e,o, d.Name);
    }
    return null;
  }
  public static void defaultModdedSetup(){
    Wrappers.FrostHelperStuff.Staticbumperwrapper.clarify.enable();
  }
}
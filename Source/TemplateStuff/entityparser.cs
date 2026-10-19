


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;
public static class EntityParser{
  public struct FakeLock:IDisposable{
    static int ctr = 0;
    public static bool fake=>ctr>0;
    public FakeLock(){
      ctr++;
    }
    void IDisposable.Dispose()=>ctr--;
  }
  public enum Types{
    unable, platformbasic, unwrapped, template,
    basic,
    iopClarified, initerr
  }
  public static LevelData DefaultLD;
  static Dictionary<string, (Types, Level.EntityLoader)> parsed = new();

  internal static void clarify(string name, Types t, Level.EntityLoader loader, bool trusted=false){
    if(name.StartsWith("auspicioushelper") && !trusted) return; //no trolling
    parsed[name] = new(t,loader);
  }
  internal static void clarify(List<string> names, Types t, Level.EntityLoader loader, bool trusted = false){
    foreach(var name in names) clarify(name, t, loader, trusted);
  }
  static Dictionary<string,Func<Level,LevelData,Vector2,EntityData,Component>> cloaders = new();
  static HashSet<string> permittedExamples = ["auspicioushelper/ChannelMover"];
  public static void clarifyComp(string name, Func<Level,LevelData,Vector2,EntityData,Component> comploader){
    if(name.StartsWith("auspicioushelper") && !permittedExamples.Contains(name)) return;
    parsed[name] = new(Types.iopClarified, createComp);
    cloaders[name] = comploader;
  }
  public static Entity createComp(Level l, LevelData ld, Vector2 o, EntityData e){
    Component c = cloaders[e.Name](l,ld,o,e);
    if(c!=null){
      currentParent.addEnt(new IopControlled(c));
    }
    return null;
  }



  static Level dummyLevel = (Level)RuntimeHelpers.GetUninitializedObject(typeof(Level));
  static LevelData dummyLd = (LevelData)RuntimeHelpers.GetUninitializedObject(typeof(LevelData));
  struct LoadInfo{
    public Level level; public EntityData data; public Vector2 offset; 
  }
  delegate ITemplateChild Wrapper(Entity e, LoadInfo d);
  static (Types, Level.EntityLoader) directWrapper(Level.EntityLoader loader, Wrapper wrapper){
    return new(Types.unwrapped, (l,ld,o,e)=>{
      var ent = loader(l,ld,o,e);
      var w = wrapper(ent,new(){level=l, data=e, offset=o});
      currentParent.addEnt(w);
      if(w is Component comp) ent.Add(comp);
      ChildMarker.Get(ent,currentParent);
      return null;
    });
  }
  internal static (Types, Level.EntityLoader) generateLoader_(EntityData d, LevelData ld, Level l){
    if(parsed.TryGetValue(d.Name, out var parse) && (l==null || parse.Item1!=Types.initerr)) return parse;
    return parsed[d.Name] = buildLoader(d,ld,l);
  }
  static (Types, Level.EntityLoader) buildLoader(EntityData d, LevelData ld, Level l){
    //DebugConsole.Write("built loader for", d.Name);
    using var fakeLock = new FakeLock();
    using var chainLock = new Template.ChainLock();
    var loader = Level.EntityLoaders.GetValueOrDefault(d.Name)?? skitzoGuess(d.Name);
    if(loader==null){
      DebugConsole.Write($"No loader found for {d.Name}");
      return new (Types.unable, null);
    }

    Entity entity;
    try{
      entity = loader(l??dummyLevel,ld??dummyLd,Vector2.Zero,d);
    } catch(Exception ex){
      DebugConsole.Write($"Entityloader generation for {d.Name} failed: \n{ex}");
      if(ex is DebugConsole.PassingException p) throw p;
      return new(l!=null? Types.unable:Types.initerr, null);
    }

    if(entity is Template) return new(Types.template, loader);
    if(entity is ITemplateChild) return new(Types.unwrapped, loader);
    
    if(entity is Actor){
      if(entity is Puffer) return directWrapper(loader, (e,d)=>new PufferW(e as Puffer));
      if(entity.Get<Holdable>() is {}) return directWrapper(loader, (e,d)=>{
        return new HoldableW(e as Actor, e.Get<Holdable>(), d.data.Bool("ausp_keepHoldingAlways",false));
      });
      return new(Types.unwrapped, loader);
    }

    if(entity is Platform){
      if(entity is MoveBlock) return directWrapper(loader, (e,d)=>new MoveBlockW(e as MoveBlock));
      return new(Types.platformbasic, loader);
    }

    if(entity is BadelineBoost) return directWrapper(loader, (e,d)=>new BoostW(e as BadelineBoost));
    if(entity is Booster) return directWrapper(loader, (e,d)=>new BoosterW(e as Booster));

    return new(Types.basic, loader);
  }
  static Dictionary<Type, Func<Level.EntityLoader, (Types,Level.EntityLoader)>> loaderBuilders = new(){
  };


  public static Template currentParent {get; private set;} = null;
  public static void createInto(EntityData d, Level l, LevelData ld, Vector2 simoffset, Template t, string path){
    ld=ld??DefaultLD;
    bool isTrigger = false;
    if(d is TriggerWrapping wrap){
      isTrigger = true;
      d=wrap.d;
    }
    EntityID eid = new(ld.Name,d.ID);
    if(l?.Session?.DoNotLoad.Contains(eid)??false) return;
    if(!parsed.TryGetValue(d.Name,out var parse) || parse.Item1==Types.initerr) parse=generateLoader_(d,ld,l);
    if(parse.Item1==Types.unable || parse.Item1==Types.initerr) return;

    var loader = parse.Item2;
    if(loader == null) return;
    if(parse.Item1 == Types.template){
      if(string.IsNullOrWhiteSpace(d.Attr("template")) && t.t.chain==null && d is not TemplateDisplacer.DisplacerData){
        DebugConsole.WriteFailure($"Empty template did not get culled from template room");
        return;
      }
    }
    
    currentParent = t;
    EntityData data = TemplateTemplate.withReplace(d);
    Entity e = null; 
    if(isTrigger) Finder.StartLoadTrigger(data, t.fullpath);
    else Finder.StartLoad(data, t.fullpath);
    
    using (new Template.ChainLock()) e = loader(l,ld,simoffset,data);
    
    currentParent = null;
    Finder.setLast(e);
    Finder.EndingLoad(data,l);
    if(e==null) return;
    e.SourceData = data;
    ChildMarker.Get(e,t);

    switch(parse.Item1){
      case Types.platformbasic:
        if(e is Platform p){
          t.addEnt(new BasicPlatform(p,t,simoffset+d.Position-t.roundLoc));
        }else{
          DebugConsole.Write("Wrongly classified!!! "+d.Name);
        }
        return;
      case Types.unwrapped: case Types.template:
        if(e is ITemplateChild child){
          t.addEnt(child);
        }
        else if(e!=null) l.Add(e);
        return;
      
      case Types.basic:
        foreach(Component c in e.Components) if(c is StaticMover sm){
          if(c is LiftspeedSm lsm) lsm.parent = t;
          if(sm.SolidChecker is {} sc){
            sm.SolidChecker = s=>(s.Get<ChildMarker>()?.inTreeOf(t)??false) && sc(s);
          }
          if(sm.JumpThruChecker is {} jc){
            sm.JumpThruChecker = jt=>(jt.Get<ChildMarker>()?.inTreeOf(t)??false) && jc(jt);
          }
          Action<Platform> old = sm.OnAttach;
          sm.OnAttach = p=>{
            //deliberately no null check because otherwise this is a Bad Situation
            (sm.Entity.Get<ChildMarker>().data as BasicMultient.EntEnt).detatched |= BasicMultient.Flags.Detatched;
            if(old!=null) old(p);
          };
        }
        t.AddBasicEnt(e,simoffset+d.Position-t.roundLoc);
        return;
    }
  }
  static EntityParser(){
    DefaultLD = (LevelData)RuntimeHelpers.GetUninitializedObject(typeof(LevelData));
    DefaultLD.Name = "ohmygoodnessiwanttodie";

    clarify("jumpThru",       Types.platformbasic, static (l,ld,offset,e) => new JumpThruW(e, offset));
    clarify("auspicioushelper/PortalGateH", Types.unwrapped, static (l,ld,o,e)=>{
      PortalFaceH.Pair.Load(l,ld,o,e);
      return null;
    }, true);

    clarify("spikesUp",            Types.basic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Up));
    clarify("spikesDown",          Types.basic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Down));
    clarify("spikesLeft",          Types.basic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Left));
    clarify("spikesRight",         Types.basic, static (l,ld,offset,e) => new Spikes(e, offset, Spikes.Directions.Right));

    clarify("triggerSpikesUp",     Types.basic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Up));
    clarify("triggerSpikesDown",   Types.basic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Down));
    clarify("triggerSpikesLeft",   Types.basic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Left));
    clarify("triggerSpikesRight",  Types.basic, static (l,ld,offset,e) => new TriggerSpikes(e, offset, TriggerSpikes.Directions.Right));
    
    clarify("spring", Types.basic, static (l,ld,offset,e)=>new Spring(e,offset,Spring.Orientations.Floor));
    clarify("wallSpringLeft", Types.basic, static (l,ld,offset,e)=>new Spring(e,offset,Spring.Orientations.WallLeft));
    clarify("wallSpringRight", Types.basic, static (l,ld,offset,e)=>new Spring(e,offset,Spring.Orientations.WallRight));
    clarify("spinner", Types.unwrapped, static (l,ld,offset,e)=>e.Bool("dust")?new Wrappers.DustSpinner(e,offset):new Wrappers.Spinner(e,offset));
    
    clarify("lamp",Types.basic,static (l,ld,offset,e)=>new Lamp(offset + e.Position, e.Bool("broken")));
    clarify("hangingLamp",Types.basic,static (l,ld,offset,e)=>new HangingLamp(e,offset+e.Position));
    clarify("seeker",Types.basic,static (l,ld,o,e)=>new Seeker(e, o));
    clarify("dashSwitchH",Types.unwrapped,static (l,ld,o,e)=>new DashSwitchW(e,o,new EntityID(ld.Name,e.ID)));
    clarify("dashSwitchV",Types.unwrapped,static (l,ld,o,e)=>new DashSwitchW(e,o,new EntityID(ld.Name,e.ID)));
    clarify("lightning", Types.unwrapped, static (l,ld,o,e)=>{
      if(!e.Bool("perLevel") && l.Session.GetFlag("disable_lightning")) return null;
      LightningW ent = null;
      //data.Position + levelOffset, data.Width, data.Height, data.FirstNodeNullable(levelOffset), 
      if(currentParent.Get<LightningW.LightningComp>() is not {} comp){
        currentParent.Add(comp = new());
        l.Tracker.GetEntity<LightningRenderer>()?.StartAmbience();
      }
      
      Vector2? node = e.FirstNodeNullable() is Vector2 v? v-e.Position:null;
      float movetime = e.Float("moveTime");
      //this is a really cursed way of doing this
      if(!comp.all.TryGetValue((node,movetime), out var group) || group.Item2.Count==0){
        comp.all[(node,movetime)] = group = new(e.Position+o, new());
        ent = new LightningW(e.Position+o, group.Item2, node, movetime);
      }
      group.Item2.Add(new FloatRect(e.Position+o - group.Item1, new(e.Width, e.Height)));
      return ent;
    });
    clarify("bigSpinner", Types.unwrapped, static (l,ld,o,e)=>new Bumperw(e,o));

    clarify("refill",Types.unwrapped,static (l,ld,offset,e)=>(Entity) new RefillW2(e,offset));
    clarify("cameraTargetTrigger", Types.basic, static (l,ld,o,e)=>{
      string text2 = e.Attr("deleteFlag");
      if(string.IsNullOrEmpty(text2) || !l.Session.GetFlag(text2)){
        return new CameraTargetTrigger(e,o);
      } else return null;
    });

    clarify("movingPlatform",Types.platformbasic,static (l,ld,o,e)=>{
      MovingPlatform movingPlatform = new MovingPlatform(e, o);
      if (e.Has("texture")) movingPlatform.OverrideTexture = e.Attr("texture");
      return movingPlatform;
    });
    clarify("blackGem",Types.basic,static (l,ld,o,e)=>new HeartGem(e,o));
    clarify("wire",Types.unwrapped,static (l,ld,o,e)=>new CWire(e,o));
    clarify("cliffflag",Types.unwrapped,static (l,ld,o,e)=>new CFlagline(e,o));
    clarify("clothesline",Types.unwrapped,static (l,ld,o,e)=>new CClothsline(e,o));
    clarify("lightningBlock",Types.unwrapped,static (l,ld,o,e)=>new LightningBreakerW(e,o));
    foreach(string c in new string[]{"Red","Yellow","Green"}){
      clarify(c.ToLower()+"Blocks", Types.platformbasic, (l,ld,o,e)=>{
        ClutterBlockGenerator.Init(l);
        var col = Enum.Parse<ClutterBlock.Colors>(c);
        ClutterBlockGenerator.Add((int)(e.Position.X/8f), (int)(e.Position.Y/8), e.Width/8, e.Height/8,col);
        //return new ClutterBlockBase(e.Position+o,e.Width,e.Height,ClutterBlockGenerator.enabled[(int)col],col);
        return null;
      });
    }
    clarify("introCar", Types.unwrapped, static (l,ld,o,e)=>new IntroCarW(e,o));
    clarify("crumbleBlock", Types.unwrapped, static (l,ld,o,e)=>new CrumbleBlockW(e,o));
    clarify("cassetteBlock", Types.unwrapped, static (l,ld,o,e)=>new CassetteW(e,o, new EntityID(ld.Name,e.ID)));
    clarify("zipMover", Types.unwrapped, static (l,ld,o,e)=>new ZipMoverW(e,o));
    clarify("swapBlock", Types.unwrapped, static (l,ld,o,e)=>new SwapBlockW(e,o));
    clarify("darkChaser", Types.unwrapped, static (l,ld,o,e)=>new ChaserW(e,o));

    defaultModdedSetup();
    foreach(string s in new string[]{"iceBlock","fireBarrier","FrostHelper/CustomFireBarrier"}){
      clarify(s, Types.unwrapped, static (l,ld,o,e)=>{
        currentParent.addEnt(new HookVanilla.FireIcePatch(origLoader(e.Name)(l,ld,o,e)));
        return null;
      });
    }
    clarify(["trackSpinner","rotateSpinner"],Types.unwrapped, static(l,ld,o,e)=>{
      string[] fields = e.Name switch {
        "trackSpinner"=>HookVanilla.AnchorLocMod._TrackSp,
        "rotateSpinner"=>HookVanilla.AnchorLocMod._RotateSp,
        _=>[]
      };
      Entity ent = origLoader(e.Name)(l,ld,o,e);
      currentParent.addEnt(new HookVanilla.AnchorLocMod(ent,fields));
      return null;
    });
    clarify(TemplateEmptyName,Types.template,static (l,ld,o,e)=>new Template(e,o),true);
    clarify("auspicioushelper/EntityMarkingFlag", Types.unwrapped, static (l,ld,o,e)=>{
      DebugConsole.Write($"Entity marking flag with ID {e.ID} created inside template with path {currentParent.fullpath}");
      return null;
    },true);
    clarify("water",Types.unwrapped,static (l,ld,o,e)=>new FancyWater(e,o));
    clarify("auspicioushelper/DieWater",Types.unwrapped,static (l,ld,o,e)=>new FancyWater(e,o), true);
  }
  public const string TemplateEmptyName = "auspicioushelper/templateEmpty";
  public static Level.EntityLoader origLoader(string name){
    if(Level.EntityLoaders.TryGetValue(name,out var loader)) return loader;
    return skitzoGuess(name);
  }

  static Level.EntityLoader skitzoGuess(string name){
    switch(name){
      case "jumpThru": return static (l,ld,o,e)=>new JumpthruPlatform(e, o);
      case "refill": return static (l,ld,o,e)=>new Refill(e, o);
      case "infiniteStar": return static (l,ld,o,e)=>new FlyFeather(e, o);
      case "strawberry": return static (l,ld,o,e)=>new Strawberry(e, o, new EntityID(ld.Name,e.ID));
      case "summitgem": return static (l,ld,o,e)=>new SummitGem(e, o, new EntityID(ld.Name,e.ID));
      case "fallingBlock": return static (l,ld,o,e)=>new FallingBlock(e, o);
      case "zipMover": return static (l,ld,o,e)=>new ZipMover(e, o);
      case "crumbleBlock": return static (l,ld,o,e)=>new CrumblePlatform(e, o);
      case "dreamBlock": return static (l,ld,o,e)=>new DreamBlock(e, o);
      case "touchSwitch": return static (l,ld,o,e)=>new TouchSwitch(e, o);
      case "switchGate": return static (l,ld,o,e)=>new SwitchGate(e, o);
      case "negaBlock": return static (l,ld,o,e)=>new NegaBlock(e, o);
      case "key": return static (l,ld,o,e)=>new Key(e, o, new EntityID(ld.Name,e.ID));
      case "lockBlock": return static (l,ld,o,e)=>new LockBlock(e, o, new EntityID(ld.Name,e.ID));
      case "movingPlatform": return static (l,ld,o,e)=>new MovingPlatform(e, o);
      case "blockField": return static (l,ld,o,e)=>new BlockField(e, o);
      case "cloud": return static (l,ld,o,e)=>new Cloud(e, o);
      case "booster": return static (l,ld,o,e)=>new Booster(e, o);
      case "moveBlock": return static (l,ld,o,e)=>new MoveBlock(e, o);
      case "light": return static (l,ld,o,e)=>new PropLight(e, o);
      case "swapBlock": return static (l,ld,o,e)=>new SwapBlock(e, o);
      case "torch": return static (l,ld,o,e)=>new Torch(e, o, new EntityID(ld.Name,e.ID));
      case "seekerBarrier": return static (l,ld,o,e)=>new SeekerBarrier(e, o);
      case "theoCrystal": return static (l,ld,o,e)=>new TheoCrystal(e, o);
      case "glider": return static (l,ld,o,e)=>new Glider(e, o);
      case "theoCrystalPedestal": return static (l,ld,o,e)=>new TheoCrystalPedestal(e, o);
      case "badelineBoost": return static (l,ld,o,e)=>new BadelineBoost(e, o);
      case "wallBooster": return static (l,ld,o,e)=>new WallBooster(e, o);
      case "bounceBlock": return static (l,ld,o,e)=>new BounceBlock(e, o);
      case "coreModeToggle": return static (l,ld,o,e)=>new CoreModeToggle(e, o);
      case "iceBlock": return static (l,ld,o,e)=>new IceBlock(e, o);
      case "fireBarrier": return static (l,ld,o,e)=>new FireBarrier(e, o);
      case "eyebomb": return static (l,ld,o,e)=>new Puffer(e, o);
      case "flingBird": return static (l,ld,o,e)=>new FlingBird(e, o);
      case "flingBirdIntro": return static (l,ld,o,e)=>new FlingBirdIntro(e, o);
      case "lightningBlock": return static (l,ld,o,e)=>new LightningBreakerBox(e, o);
      case "sinkingPlatform": return static (l,ld,o,e)=>new SinkingPlatform(e, o);
      case "friendlyGhost": return static (l,ld,o,e)=>new AngryOshiro(e, o);
      case "seeker": return static (l,ld,o,e)=>new Seeker(e, o);
      case "seekerStatue": return static (l,ld,o,e)=>new SeekerStatue(e, o);
      case "slider": return static (l,ld,o,e)=>new Slider(e, o);
      case "templeBigEyeball": return static (l,ld,o,e)=>new TempleBigEyeball(e, o);
      case "crushBlock": return static (l,ld,o,e)=>new CrushBlock(e, o);
      case "bigSpinner": return static (l,ld,o,e)=>new Bumper(e, o);
      case "starJumpBlock": return static (l,ld,o,e)=>new StarJumpBlock(e, o);
      case "floatySpaceBlock": return static (l,ld,o,e)=>new FloatySpaceBlock(e, o);
      case "glassBlock": return static (l,ld,o,e)=>new GlassBlock(e, o);
      case "goldenBlock": return static (l,ld,o,e)=>new GoldenBlock(e, o);
      case "fireBall": return static (l,ld,o,e)=>new FireBall(e, o);
      case "risingLava": return static (l,ld,o,e)=>new RisingLava(e, o);
      case "sandwichLava": return static (l,ld,o,e)=>new SandwichLava(e, o);
      case "killbox": return static (l,ld,o,e)=>new Killbox(e, o);
      case "fakeHeart": return static (l,ld,o,e)=>new FakeHeart(e, o);
      case "finalBoss": return static (l,ld,o,e)=>new FinalBoss(e, o);
      case "finalBossMovingBlock": return static (l,ld,o,e)=>new FinalBossMovingBlock(e, o);
      case "dashBlock": return static (l,ld,o,e)=>new DashBlock(e, o, new EntityID(ld.Name,e.ID));
      case "invisibleBarrier": return static (l,ld,o,e)=>new InvisibleBarrier(e, o);
      case "exitBlock": return static (l,ld,o,e)=>new ExitBlock(e, o);
      case "coverupWall": return static (l,ld,o,e)=>new CoverupWall(e, o);
      case "crumbleWallOnRumble": return static (l,ld,o,e)=>new CrumbleWallOnRumble(e, o, new EntityID(ld.Name,e.ID));
      case "tentacles": return static (l,ld,o,e)=>new ReflectionTentacles(e, o);
      case "playerSeeker": return static (l,ld,o,e)=>new PlayerSeeker(e, o);
      case "chaserBarrier": return static (l,ld,o,e)=>new ChaserBarrier(e, o);
      case "introCrusher": return static (l,ld,o,e)=>new IntroCrusher(e, o);
      case "bridge": return static (l,ld,o,e)=>new Bridge(e, o);
      case "bridgeFixed": return static (l,ld,o,e)=>new BridgeFixed(e, o);
      case "bird": return static (l,ld,o,e)=>new BirdNPC(e, o);
      case "introCar": return static (l,ld,o,e)=>new IntroCar(e, o);
      case "memorial": return static (l,ld,o,e)=>new Memorial(e, o);
      case "wire": return static (l,ld,o,e)=>new Wire(e, o);
      case "cobweb": return static (l,ld,o,e)=>new Cobweb(e, o);
      case "hahaha": return static (l,ld,o,e)=>new Hahaha(e, o);
      case "bonfire": return static (l,ld,o,e)=>new Bonfire(e, o);
      case "colorSwitch": return static (l,ld,o,e)=>new ClutterSwitch(e, o);
      case "resortmirror": return static (l,ld,o,e)=>new ResortMirror(e, o);
      case "towerviewer": return static (l,ld,o,e)=>new Lookout(e, o);
      case "picoconsole": return static (l,ld,o,e)=>new PicoConsole(e, o);
      case "wavedashmachine": return static (l,ld,o,e)=>new WaveDashTutorialMachine(e, o);
      case "oshirodoor": return static (l,ld,o,e)=>new MrOshiroDoor(e, o);
      case "templeMirrorPortal": return static (l,ld,o,e)=>new TempleMirrorPortal(e, o);
      case "reflectionHeartStatue": return static (l,ld,o,e)=>new ReflectionHeartStatue(e, o);
      case "resortRoofEnding": return static (l,ld,o,e)=>new ResortRoofEnding(e, o);
      case "gondola": return static (l,ld,o,e)=>new Gondola(e, o);
      case "birdForsakenCityGem": return static (l,ld,o,e)=>new ForsakenCitySatellite(e, o);
      case "whiteblock": return static (l,ld,o,e)=>new WhiteBlock(e, o);
      case "plateau": return static (l,ld,o,e)=>new Plateau(e, o);
      case "soundSource": return static (l,ld,o,e)=>new SoundSourceEntity(e, o);
      case "templeMirror": return static (l,ld,o,e)=>new TempleMirror(e, o);
      case "templeEye": return static (l,ld,o,e)=>new TempleEye(e, o);
      case "clutterCabinet": return static (l,ld,o,e)=>new ClutterCabinet(e, o);
      case "floatingDebris": return static (l,ld,o,e)=>new FloatingDebris(e, o);
      case "foregroundDebris": return static (l,ld,o,e)=>new ForegroundDebris(e, o);
      case "moonCreature": return static (l,ld,o,e)=>new MoonCreature(e, o);
      case "lightbeam": return static (l,ld,o,e)=>new LightBeam(e, o);
      case "door": return static (l,ld,o,e)=>new Door(e, o);
      case "trapdoor": return static (l,ld,o,e)=>new Trapdoor(e, o);
      case "resortLantern": return static (l,ld,o,e)=>new ResortLantern(e, o);
      case "water": return static (l,ld,o,e)=>new Water(e, o);
      case "waterfall": return static (l,ld,o,e)=>new WaterFall(e, o);
      case "bigWaterfall": return static (l,ld,o,e)=>new BigWaterfall(e, o);
      case "clothesline": return static (l,ld,o,e)=>new Clothesline(e, o);
      case "cliffflag": return static (l,ld,o,e)=>new CliffFlags(e, o);
      case "cliffside_flag": return static (l,ld,o,e)=>new CliffsideWindFlag(e, o);
      case "flutterbird": return static (l,ld,o,e)=>new FlutterBird(e, o);
      case "SoundTest3d": return static (l,ld,o,e)=>new _3dSoundTest(e, o);
      case "SummitBackgroundManager": return static (l,ld,o,e)=>new AscendManager(e, o);
      case "summitGemManager": return static (l,ld,o,e)=>new SummitGemManager(e, o);
      case "heartGemDoor": return static (l,ld,o,e)=>new HeartGemDoor(e, o);
      case "summitcheckpoint": return static (l,ld,o,e)=>new SummitCheckpoint(e, o);
      case "summitcloud": return static (l,ld,o,e)=>new SummitCloud(e, o);
      case "coreMessage": return static (l,ld,o,e)=>new CoreMessage(e, o);
      case "playbackTutorial": return static (l,ld,o,e)=>new PlayerPlayback(e, o);
      case "playbackBillboard": return static (l,ld,o,e)=>new PlaybackBillboard(e, o);
      case "cutsceneNode": return static (l,ld,o,e)=>new CutsceneNode(e, o);
      case "kevins_pc": return static (l,ld,o,e)=>new KevinsPC(e, o);
      case "templeGate": return static (l,ld,o,e)=>new TempleGate(e,o, ld.Name);
      case "payphone": return static (l,ld,o,e)=>new Payphone(e.Position+o);
      case "templeCrackedBlock": return static (l,ld,o,e)=>new TempleCrackedBlock(new EntityID(ld.Name,e.ID),e,o);

      case "eventTrigger": return static (l,ld,o,e)=>new EventTrigger(e, o);
      case "musicFadeTrigger": return static (l,ld,o,e)=>new MusicFadeTrigger(e, o);
      case "musicTrigger": return static (l,ld,o,e)=>new MusicTrigger(e, o);
      case "altMusicTrigger": return static (l,ld,o,e)=>new AltMusicTrigger(e, o);
      case "cameraOffsetTrigger": return static (l,ld,o,e)=>new CameraOffsetTrigger(e, o);
      case "lightFadeTrigger": return static (l,ld,o,e)=>new LightFadeTrigger(e, o);
      case "bloomFadeTrigger": return static (l,ld,o,e)=>new BloomFadeTrigger(e, o);
      case "cameraAdvanceTargetTrigger": return static (l,ld,o,e)=>new CameraAdvanceTargetTrigger(e, o);
      case "respawnTargetTrigger": return static (l,ld,o,e)=>new RespawnTargetTrigger(e, o);
      case "changeRespawnTrigger": return static (l,ld,o,e)=>new ChangeRespawnTrigger(e, o);
      case "windTrigger": return static (l,ld,o,e)=>new WindTrigger(e, o);
      case "windAttackTrigger": return static (l,ld,o,e)=>new WindAttackTrigger(e, o);
      case "oshiroTrigger": return static (l,ld,o,e)=>new OshiroTrigger(e, o);
      case "interactTrigger": return static (l,ld,o,e)=>new InteractTrigger(e, o);
      case "checkpointBlockerTrigger": return static (l,ld,o,e)=>new CheckpointBlockerTrigger(e, o);
      case "lookoutBlocker": return static (l,ld,o,e)=>new LookoutBlocker(e, o);
      case "stopBoostTrigger": return static (l,ld,o,e)=>new StopBoostTrigger(e, o);
      case "noRefillTrigger": return static (l,ld,o,e)=>new NoRefillTrigger(e, o);
      case "ambienceParamTrigger": return static (l,ld,o,e)=>new AmbienceParamTrigger(e, o);
      case "creditsTrigger": return static (l,ld,o,e)=>new CreditsTrigger(e, o);
      case "goldenBerryCollectTrigger": return static (l,ld,o,e)=>new GoldBerryCollectTrigger(e, o);
      case "moonGlitchBackgroundTrigger": return static (l,ld,o,e)=>new MoonGlitchBackgroundTrigger(e, o);
      case "blackholeStrength": return static (l,ld,o,e)=>new BlackholeStrengthTrigger(e, o);
      case "birdPathTrigger": return static (l,ld,o,e)=>new BirdPathTrigger(e, o);
      case "spawnFacingTrigger": return static (l,ld,o,e)=>new SpawnFacingTrigger(e, o);
      case "detachFollowersTrigger": return static (l,ld,o,e)=>new DetachStrawberryTrigger(e, o);
      case "rumbleTrigger": return static (l,ld,o,e)=>new RumbleTrigger(e,o,new EntityID(ld.Name,e.ID+10000000));
      case "minitextboxTrigger": return static (l,ld,o,e)=>new MiniTextboxTrigger(e,o,new EntityID(ld.Name,e.ID+10000000));

      case "rotateSpinner": return static (l,ld,o,e)=>{
        if(e.Bool("star",false)) return new StarRotateSpinner(e,o);
        if(e.Bool("dust",false)) return new DustRotateSpinner(e,o);
        return new BladeRotateSpinner(e,o);
      };
      case "trackSpinner": return static (l,ld,o,e)=>{
        if(e.Bool("star",false)) return new StarTrackSpinner(e,o);
        if(e.Bool("dust",false)) return new DustTrackSpinner(e,o);
        return new BladeTrackSpinner(e,o);
      };
    }
    return null;
  }
  public static void defaultModdedSetup(){
    FrostHelperStuff.setup();
    MaddiesStuff.setup();
    clarify("VortexHelper/PurpleBooster",Types.basic,static (l,ld,o,e)=>{
      if(e.Bool("lavender")) e.Name = "VortexHelper/LavenderBooster";
      return Level.EntityLoaders.GetValueOrDefault(e.Name)?.Invoke(l,ld,o,e);
    });
  }

  public class TriggerWrapping(EntityData data):EntityData{
    public EntityData d=>data;
  }
}
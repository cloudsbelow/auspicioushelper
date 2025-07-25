



using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.auspicioushelper.Wrappers;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

public class StaticmoverLock:MovementLock,IDisposable{
  static int locked=0;
  static List<Tuple<StaticMover,Vector2>> enq = new();
  public StaticmoverLock():base(false){
    locked++;
  }
  public override void Dispose(){
    if(--locked==0){
      if(enq.Count!=0){
        var t = enq;
        enq = new();
        foreach(var pair in t){
          pair.Item1.OnMove(pair.Item2);
        }
      }
    }
    base.Dispose();
  }
  static public bool tryol(StaticMover s, Vector2 move){
    if(locked==0)return false;
    enq.Add(new(s,move));
    return true;
  }
  static void SolidMove(On.Celeste.Solid.orig_MoveHExact orig, Solid s, int m){
    using(new StaticmoverLock()) orig(s,m);
  }
  static void SolidMove(On.Celeste.Solid.orig_MoveVExact orig, Solid s, int m){
    using(new StaticmoverLock()) orig(s,m);
  }
  public static HookManager hooks = new(()=>{
    On.Celeste.Solid.MoveHExact+=SolidMove;
    On.Celeste.Solid.MoveVExact+=SolidMove;
  },()=>{
    On.Celeste.Solid.MoveHExact-=SolidMove;
    On.Celeste.Solid.MoveVExact-=SolidMove;
  }, auspicioushelperModule.OnEnterMap);
}

[CustomEntity("auspicioushelper/TemplateStaticmover")]
public class TemplateStaticmover:TemplateDisappearer, IMaterialObject, ITemplateTriggerable{
  public override Vector2 gatheredLiftspeed=>ownLiftspeed;
  public override void relposTo(Vector2 loc, Vector2 liftspeed) {}
  int smearamount;
  Vector2[] pastLiftspeed;
  bool averageSmear;
  string channel="";
  bool ridingTrigger;
  bool enableUnrooted = false;
  bool conveyRiding = false;
  bool firstZeroAfter;
  
  public TemplateStaticmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateStaticmover(EntityData d, Vector2 offset, int depthoffset):base(d,d.Position+offset,depthoffset){
    smearamount = d.Int("liftspeed_smear",4);
    averageSmear = d.Bool("smear_average",false);
    channel = d.Attr("channel","");
    pastLiftspeed = new Vector2[smearamount];
    ridingTrigger = d.Bool("ridingTrigger",true);
    enableUnrooted = d.Bool("EnableUnrooted",false);
    conveyRiding = d.Bool("conveyRiding",false);
    hooks.enable();
    StaticmoverLock.hooks.enable();
    Add(new BeforeAfterRender(()=>{
      if(this.ownShakeVec == Vector2.Zero){
        if(!firstZeroAfter) return;
        firstZeroAfter = false;
        foreach(IChildShaker e in GetChildren<IChildShaker>(Propagation.Shake)) e.OnShakeFrame(Vector2.Zero);
        return;
      }
      firstZeroAfter=true;
      foreach(Entity e in GetChildren<Entity>(Propagation.Shake)){
        prevpos.TryAdd(e,e.Position);
        e.Position+=ownShakeVec;
        if(e is IChildShaker s) s.OnShakeFrame(ownShakeVec);
      }
    }));
    shakeHooks.enable();
    TemplateMoveCollidable.triggerHooks.enable();
  }
  void evalLiftspeed(bool precess = true){
    float mX=0;
    float mY=0;
    if(!averageSmear)foreach(Vector2 v in pastLiftspeed){
      if(Math.Abs(v.X)>Math.Abs(mX)) mX=v.X;
      if(Math.Abs(v.Y)>Math.Abs(mY)) mY=v.Y;
    } else foreach(Vector2 v in pastLiftspeed){
      mX+=v.X/smearamount; mY+=v.Y/smearamount;
    }
    ownLiftspeed = new Vector2(mX,mY);
    if(!precess) return; 
    for(int i=smearamount-1; i>=1; i--){
      pastLiftspeed[i]=pastLiftspeed[i-1];
    }
    pastLiftspeed[0]=Vector2.Zero;
  }
  List<Entity> todraw;
  internal StaticMover sm;
  HashSet<Platform> doNot = new();
  CassetteMaterialLayer layer = null;
  public override void addTo(Scene scene){
    base.addTo(scene);
    setVisCol(false,false);
    if(channel != "")CassetteMaterialLayer.layers.TryGetValue(channel,out layer);
    List<Entity> allChildren = new();
    AddAllChildren(allChildren);
    foreach(Entity e in allChildren){
      if(e is Platform p) doNot.Add(p);
    }
    Add(sm = new StaticMover(){
      OnEnable=()=>{
        childRelposTo(virtLoc,Vector2.Zero);
        setVisCol(true,true);
        if(string.IsNullOrWhiteSpace(channel)) remake();
        else if(layer != null) layer.removeTrying(this);
        this.ownShakeVec = Vector2.Zero;
      },
      OnDisable=()=>{
        setVisCol(false,false);
        cachedCol =false;
        if(string.IsNullOrWhiteSpace(channel)) destroyChildren(true);
        else if(layer !=null) layer.addTrying(this);
      },
      OnAttach=(Platform p)=>{
        setVisCol(true,true);
      },
      SolidChecker=(Solid s)=>{
        //DebugConsole.Write(s.ToString());
        bool check = !doNot.Contains(s) && s.CollidePoint(Position);
        if(!check) return false;
        if(!s.Collidable){
          setVisCol(false,false);
          cachedCol =false;
          if(layer !=null) layer.addTrying(this);
        }
        return true;
      },
      OnDestroy=()=>{
        setCollidability(false);
        destroy(true);
        if(layer!=null) layer.removeTrying(this);
      },
      OnMove=(Vector2 move)=>{
        if(StaticmoverLock.tryol(sm,move)) return;
        Position+=move;
        pastLiftspeed[0]+=move/Math.Max(Engine.DeltaTime,0.005f);
        evalLiftspeed();
        bool flag = false;
        if(cachedCol != getSelfCol() && move!=Vector2.Zero){
          flag = true;
          setCollidability(cachedCol);
        }
        childRelposSafe();
        if(flag)setCollidability(!cachedCol);
      },
      OnShake = (Vector2 shakevec)=>{
        this.ownShakeVec += shakevec;
      }
    });
    if(layer!=null){
      todraw = new List<Entity>();
      AddAllChildren(todraw);
    }
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(enableUnrooted && sm.Platform == null){
      setVisCol(true, true);
      Remove(sm);
    }
  }
  public override void Update(){
    base.Update();
    evalLiftspeed(true);
    if(ridingTrigger){
      if(hasRiders<Player>()) sm.TriggerPlatform();
    }
  }
  public void OnTrigger(TriggerInfo info){
    if(ridingTrigger){
      Template parent = sm?.Platform?.Get<ChildMarker>()?.parent;
      if(parent!=null) parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(info);
      else if(TriggerInfo.TestPass(info,this)) sm?.TriggerPlatform();
    }
  }
  public void renderMaterial(IMaterialLayer l, Camera c){
    foreach(Entity e in todraw){
      if(e.Scene != null && e.Depth<=l.depth){
        if(CassetteMaterialLayer.stupididiotdumbpompusthings.TryGetValue(e.GetType(),out var fn))fn(e);
        else e.Render();
      }
    } 
  }
  bool cachedCol = true;

  static bool MoveHPlatHook(On.Celeste.Platform.orig_MoveHExactCollideSolids orig, Platform p, 
    int moveH, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide
  ){
    foreach(StaticMover s in p.staticMovers){
      if(s.Entity is TemplateStaticmover s_){
        s_.cachedCol = s_.getSelfCol();
        s_.setCollidability(false);
      } 
    }
    bool res = orig(p, moveH, thruDashBlocks, onCollide);
    foreach(StaticMover s in p.staticMovers){
      if(s.Entity is TemplateStaticmover s_){
        s_.setCollidability(s_.cachedCol);
      } 
    }
    return res;
  }
  static bool MoveVPlatHook(On.Celeste.Platform.orig_MoveVExactCollideSolids orig, Platform p, 
    int moveH, bool thruDashBlocks, Action<Vector2, Vector2, Platform> onCollide
  ){
    foreach(StaticMover s in p.staticMovers){
      if(s.Entity is TemplateStaticmover s_){
        s_.cachedCol = s_.getSelfCol();
        s_.setCollidability(false);
      } 
    }
    bool res = orig(p, moveH, thruDashBlocks, onCollide);
    foreach(StaticMover s in p.staticMovers){
      if(s.Entity is TemplateStaticmover s_){
        s_.setCollidability(s_.cachedCol);
      } 
    }
    return res;
  }
  static Player Hook(On.Celeste.Solid.orig_GetPlayerRider orig, Solid s){
    var p = orig(s);
    if(p!=null) return p;
    //DebugConsole.Write("Here");
    foreach(StaticMover sm in s.staticMovers) if(sm.Entity is TemplateStaticmover tsm){
      if(tsm.conveyRiding && tsm.hasRiders<Player>()) return UpdateHook.cachedPlayer;
    } 
    return null;
  }
  public override void destroy(bool particles){
    base.destroy(particles);
    if(layer!=null) layer.removeTrying(this);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Platform.MoveHExactCollideSolids += MoveHPlatHook;
    On.Celeste.Platform.MoveVExactCollideSolids += MoveVPlatHook;
    On.Celeste.Solid.GetPlayerRider += Hook;
  },()=>{
    On.Celeste.Platform.MoveHExactCollideSolids -= MoveHPlatHook;
    On.Celeste.Platform.MoveVExactCollideSolids -= MoveVPlatHook;
    On.Celeste.Solid.GetPlayerRider -= Hook;

  },auspicioushelperModule.OnEnterMap);
}
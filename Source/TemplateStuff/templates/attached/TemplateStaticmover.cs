



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
public class TemplateStaticmover:TemplateDisappearer, ITemplateTriggerable, IOverrideVisuals, Template.IRegisterEnts{
  public override Vector2 gatheredLiftspeed=>ownLiftspeed;
  public override void relposTo(Vector2 loc, Vector2 liftspeed) {
    if(sm?.Platform==null) base.relposTo(loc,liftspeed);
  }
  int smearamount;
  Vector2[] pastLiftspeed;
  bool averageSmear;
  string channel="";
  bool ridingTrigger;
  bool enableUnrooted = false;
  bool conveyRiding = false;
  bool convertTriggering;
  bool firstZeroAfter;
  int hasTrigger=0;
  Vector2 smoffset = Vector2.Zero;
  
  public TemplateStaticmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateStaticmover(EntityData d, Vector2 offset, int depthoffset):base(d,d.Position+offset,depthoffset){
    smearamount = d.Int("liftspeed_smear",4);
    averageSmear = d.Bool("smear_average",false);
    channel = d.Attr("channel","");
    pastLiftspeed = new Vector2[smearamount];
    ridingTrigger = d.Bool("ridingTrigger",true);
    enableUnrooted = d.Bool("EnableUnrooted",false);
    conveyRiding = d.Bool("conveyRiding",false);
    convertTriggering = d.Bool("triggerAsRiding",false);
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
    if(d.Nodes?.Length>=1) smoffset = d.Nodes[0]-d.Position;
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
  internal StaticMover sm;
  HashSet<Platform> doNot = new();
  CassetteMaterialLayer layer = null;
  bool made=false;
  public void make(Scene s){
    if(made || shouldDie) return;
    addingScene = s;
    made = true;
    makeChildren(s,false);
    if(!getSelfCol()) parentChangeStatBypass(layer==null?-1:0,-1,0);
  }
  bool shouldDie=false;
  public override void addTo(Scene scene){
    //base.addTo(scene);
    if(parent!=null || t?.chain!=null)scene.Add(this);
    if(sm!=null) return;
    setTemplate(scene:scene);
    if(t==null){
      shouldDie=true;
      return;
    }
    if(channel != "")CassetteMaterialLayer.layers.TryGetValue(channel,out layer);
    if(enableUnrooted) make(scene);
    Add(sm = new StaticMover(){
      OnEnable=()=>{
        childRelposTo(virtLoc,Vector2.Zero);
        setVisCol(true,true);
        if(layer!=null)foreach(var c in comps)c.SetStealUse(layer,false,false);
        if(string.IsNullOrWhiteSpace(channel))remake();
        this.ownShakeVec = Vector2.Zero;
        cachedCol = true;
      },
      OnDisable=()=>{
        setVisCol(layer!=null,false);
        cachedCol =false;
        if(string.IsNullOrWhiteSpace(channel)) destroyChildren(true);
        else if(layer!=null)foreach(var c in comps)c.SetStealUse(layer,true,true);
      },
      OnAttach=(Platform p)=>{
        if(!enableUnrooted) UpdateHook.AddAfterUpdate(()=>make(Scene));
      },
      SolidChecker=(Solid s)=>{
        bool check = !doNot.Contains(s) && s.CollidePoint(Position+smoffset);
        if(!check) return false;
        if(!s.Collidable){
          setVisCol(layer!=null,false);
          cachedCol =false;
          if(layer!=null)foreach(var c in comps)c.SetStealUse(layer,true,true);
        }
        return true;
      },
      OnDestroy=()=>{
        setCollidability(false);
        destroy(true);
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
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(shouldDie) return;
    if(enableUnrooted && sm.Platform == null){
      setVisCol(true, true);
      Remove(sm);
      return;
    }
    if(sm.Platform == null){
      foreach(Solid s in Scene.Tracker.GetEntities<Solid>()){
        if(sm.SolidChecker(s)){
          s.staticMovers.Add(sm);
          sm.Platform=s;
          sm.OnAttach(s);
        }
      }
    }
  }
  public override void Update(){
    base.Update();
    if(shouldDie) return;
    evalLiftspeed(true);
    if(ridingTrigger){
      if(hasPlayerRider()) sm.TriggerPlatform();
    }
    if(hasTrigger>0)hasTrigger--;
  }
  public void OnTrigger(TriggerInfo info){
    if(ridingTrigger){
      Template parent = sm?.Platform?.Get<ChildMarker>()?.parent;
      if(parent!=null) parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(info);
      else if(TriggerInfo.TestPass(info,this)) sm?.TriggerPlatform();
    }
    if(convertTriggering){
      hasTrigger = 2;
    }
  }
  public HashSet<OverrideVisualComponent> comps = new();
  public void AddC(OverrideVisualComponent c)=>comps.Add(c);
  public void RemoveC(OverrideVisualComponent c)=>comps.Remove(c);
  public override void RegisterEnts(List<Entity> l) {
    foreach(var e in l)if(e is Platform p)doNot.Add(p);
    bool ghost = !getSelfCol();
    int tdepth = TemplateDepth();
    if(layer!=null){
      foreach(var e in l){
        var c = OverrideVisualComponent.Get(e);
        c.AddToOverride(new(this, -30000, false,true));
        c.AddToOverride(new(layer, -10000+tdepth, ghost, ghost));
        if(layer.fg!=null)c.AddToOverride(new(layer.fg, 1000-tdepth,true,true));
      }
    }
    base.RegisterEnts(l);
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
    foreach(StaticMover sm in s.staticMovers) if(sm.Entity is TemplateStaticmover tsm){
      if(tsm.conveyRiding && tsm.hasPlayerRider()) return UpdateHook.cachedPlayer;
      if(tsm.hasTrigger>0){
        tsm.hasTrigger = 0;
        return UpdateHook.cachedPlayer;
      }
    } 
    return null;
  }
  public override void destroy(bool particles){
    base.destroy(particles);
    shouldDie = true;
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
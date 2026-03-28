



using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Celeste.Mod.auspicioushelper.Wrappers;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Celeste.Mod.Helpers;

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
public class TemplateStaticmover:TemplateDisappearer, ITemplateTriggerable, IOverrideVisuals, Template.IRegisterEnts, IRelocateTemplates.IDontRelocate{
  public override Vector2 gatheredLiftspeed=>ownLiftspeed;
  public override void relposTo(Vector2 loc, Vector2 liftspeed) {
    if(sm?.Platform==null) base.relposTo(loc,liftspeed);
  }
  string channel="";
  bool ridingTrigger;
  bool enableUnrooted = false;
  bool conveyRiding = false;
  bool attachToJt = false;
  bool convertTriggering;
  bool firstZeroAfter;
  int hasTrigger=0;
  Vector2 smoffset = Vector2.Zero;
  
  public TemplateStaticmover(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateStaticmover(EntityData d, Vector2 offset, int depthoffset):base(d,d.Position+offset,depthoffset){
    channel = d.Attr("channel","");
    ridingTrigger = d.Bool("ridingTrigger",true);
    enableUnrooted = d.Bool("EnableUnrooted",false);
    conveyRiding = d.Bool("conveyRiding",false);
    convertTriggering = d.Bool("triggerAsRiding",false);
    attachToJt = d.Bool("attachToJumpthru",false);
    ResetEvents.LazyEnable(typeof(TemplateStaticmover));
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
  internal LiftspeedSm sm;
  HashSet<Platform> doNot = new();
  CassetteMaterialLayer layer = null;
  bool made=false;
  public void make(Scene s){
    if(made || shouldDie) return;
    addingScene = s;
    made = true;
    makeChildren(s);
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
    Add(sm = new LiftspeedSm(){
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
      JumpThruChecker=attachToJt?(JumpThru j)=>{
        bool check = !doNot.Contains(j) && 
          j.CollidePoint(Position+smoffset+Vector2.UnitY) && 
          !j.CollidePoint(Position+smoffset-Vector2.UnitY);
        if(!check) return false;
        if(!j.Collidable){
          setVisCol(layer!=null,false);
          cachedCol =false;
          if(layer!=null)foreach(var c in comps)c.SetStealUse(layer,true,true);
        }
        return true;
      }:null,
      OnDestroy=()=>{
        setCollidability(false);
        destroy(true);
      },
      OnMoveOther=(Vector2 move)=>{
        if(StaticmoverLock.tryol(sm,move)) return;
        Position+=move;
        ownLiftspeed = sm.getLiftspeed();
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
    if(sm.Platform == null) foreach(Solid s in Scene.Tracker.GetEntities<Solid>()){
      if(sm.SolidChecker(s)){
        s.staticMovers.Add(sm);
        sm.Platform=s;
        sm.OnAttach(s);
        break;
      }
    }
    if(attachToJt && sm.Platform == null) foreach(JumpThru j in Scene.Tracker.GetEntities<JumpThru>()){
      if(sm.JumpThruChecker(j)){
        j.staticMovers.Add(sm);
        sm.Platform=j;
        sm.OnAttach(j);
      }
    }
  }
  public override void Update(){
    base.Update();
    if(shouldDie) return;
    ownLiftspeed = sm.getLiftspeed();
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

  static void DisableSms(Platform p){
    foreach(StaticMover s in p.staticMovers){
      if(s.Entity is TemplateStaticmover s_){
        s_.cachedCol = s_.getSelfCol();
        s_.setCollidability(false);
      } 
    }
  }
  static void EnablewSms(Platform p){
    foreach(StaticMover s in p.staticMovers){
      if(s.Entity is TemplateStaticmover s_){
        s_.setCollidability(s_.cachedCol);
      } 
    }
  }
  [ResetEvents.ILHook(typeof(Platform),nameof(Platform.MoveVExactCollideSolids))]
  [ResetEvents.ILHook(typeof(Platform),nameof(Platform.MoveHExactCollideSolids))]
  static void CollideSolidsIL(ILContext ctx){
    ILCursor c = new(ctx);
    c.EmitLdarg0();
    c.EmitDelegate(DisableSms);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchCallvirt<Platform>(nameof(Platform.MoveHExact))||
           itr.MatchCallvirt<Platform>(nameof(Platform.MoveVExact))
    )){
      c.EmitLdarg0();
      c.EmitDelegate(EnablewSms);
      return;
    }
    DebugConsole.WriteFailure("Could not add moveCollideSolid hook",true);
  }
  static Player getRider(Platform p){
    foreach(StaticMover sm in p.staticMovers) if(sm.Entity is TemplateStaticmover tsm){
      if(tsm.conveyRiding && tsm.hasPlayerRider()) return UpdateHook.cachedPlayer;
      if(tsm.hasTrigger>0){
        tsm.hasTrigger = 0;
        return UpdateHook.cachedPlayer;
      }
    } 
    return null;
  }
  [ResetEvents.OnHook(typeof(Solid),nameof(Solid.GetPlayerRider))]
  static Player Hook(On.Celeste.Solid.orig_GetPlayerRider orig, Solid s)=>orig(s)??getRider(s);
  
  [ResetEvents.OnHook(typeof(JumpThru),nameof(JumpThru.GetPlayerRider))]
  static Player Hook(On.Celeste.JumpThru.orig_GetPlayerRider orig, JumpThru j)=>orig(j)??getRider(j);
  public override void destroy(bool particles){
    base.destroy(particles);
    shouldDie = true;
  }
}
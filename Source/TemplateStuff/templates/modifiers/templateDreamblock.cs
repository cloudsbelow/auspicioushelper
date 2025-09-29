



using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateDreamblockModifier")]
public class TemplateDreamblockModifier:Template,IOverrideVisuals{
  public HashSet<OverrideVisualComponent> comps  {get;set;}= new();
  public void AddC(OverrideVisualComponent c)=>comps.Add(c);
  public void RemoveC(OverrideVisualComponent c)=>comps.Remove(c);
  public bool dirty {get;set;}
  static Dictionary<Type, Collider> colTypes = new();
  bool triggerOnEnter;
  bool triggerOnLeave;
  bool useVisuals = true;
  bool reverse=false;
  bool conserve=false;
  public class DreamInfo:TriggerInfo{
    bool exiting;
    Vector2 dir;
    bool trigger=true;
    public DreamInfo(bool exiting, bool trigger, Vector2 dir){
      this.exiting=exiting;this.trigger=trigger;this.dir=dir;
    }
    public override bool shouldTrigger => trigger;
    public override string category=> $"Dream/{exiting}/{dir.X},{dir.Y}";
  }
  [Tracked]
  public class DreamMarkerComponent:Component,IFreeableComp{
    Collider Collider;
    TemplateDreamblockModifier dbm;
    public DreamMarkerComponent(TemplateDreamblockModifier t, Collider c = null):base(false,false){
      Collider = c; dbm = t;
    }
    public static DreamBlock CheckContinue(DreamBlock db, Player p){
      if(db!=null) return db;
      foreach(DreamMarkerComponent c in p.Scene.Tracker.GetComponents<DreamMarkerComponent>()){
        if(!c.dbm.dreaming || c.Entity.Collidable == false) continue;
        bool flag=false;
        if(c.Collider==null) flag = p.CollideCheck(c.Entity);
        else{
          Collider orig = c.Entity.Collider;
          c.Entity.Collider=c.Collider;
          flag = p.CollideCheck(c.Entity);
          c.Entity.Collider=orig;
        }
        if(flag){
          c.dbm.fake.lastEntity=c.Entity;
          return c.dbm.fake;
        } 
      }
      return null;
    }
    public static bool CheckDDiag(Player p){
      p.Position.Y+=1;
      bool flag=false;
      DebugConsole.Write($"here", p, p?.Scene);
      foreach(DreamMarkerComponent c in p.Scene.Tracker.GetComponents<DreamMarkerComponent>()){
        if(!c.dbm.dreaming || c.Entity.Collidable == false) continue;
        if(c.Collider==null) flag = p.CollideCheck(c.Entity);
        else{
          Collider orig = c.Entity.Collider;
          c.Entity.Collider=c.Collider;
          flag = p.CollideCheck(c.Entity);
          c.Entity.Collider=orig;
        }
        if(flag) break;
      }
      p.Position.Y-=1;
      return flag;
    }
  }
  public class SentinalDb:DreamBlock{
    public TemplateDreamblockModifier parent;
    public Entity lastEntity;
    public SentinalDb():base(null,Vector2.Zero){}
  }
  SentinalDb fake;
  string channel;
  public TemplateDreamblockModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateDreamblockModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    hooks.enable();
    triggerOnEnter = d.Bool("triggerOnEnter",true);
    triggerOnLeave = d.Bool("triggerOnLeave",true);
    channel = d.Attr("normalChannel","");
    useVisuals = d.Bool("useVisuals",true);
    reverse = d.Bool("reverse",false);
    conserve = d.Bool("conserve",false);
  }
  public override void addTo(Scene scene) {
    fake = (SentinalDb)RuntimeHelpers.GetUninitializedObject(typeof(SentinalDb));
    fake.parent = this;
    fake.Position = Vector2.Zero;
    fake.Collider = new Hitbox(2000000000,2000000000,-1000000000,-1000000000);
    if(useVisuals){
      if(reverse){
        renderer = (revrender??=new DreamRenderer(true));
      } else {
        renderer = (normrenderer??=new DreamRenderer(false));
      }
      MaterialPipe.addLayer(renderer);
    } 
    if(!string.IsNullOrWhiteSpace(channel)){
      ChannelTracker ct = new ChannelTracker(channel,(int val)=>{
        bool orig = dreaming;
        dreaming = val==0;
        if(dreaming==orig) return;
        if(useVisuals) foreach(var c in comps) { 
          c.SetStealUse(renderer,dreaming,dreaming);
        }
        if(orig && !dreaming && UpdateHook.cachedPlayer is Player p && p.StateMachine.State==Player.StDreamDash){
          UpdateHook.AddAfterUpdate(()=>{
            if(hasInside(p) && DreamMarkerComponent.CheckContinue(p.CollideFirst<DreamBlock>(),p)==null)p.Die(Vector2.Zero,true);
          },false);
        }
      });
      if(ct.value!=0)dreaming=false;
      Add(ct);
    }
    base.addTo(scene);
    setupEnts(GetChildren<Entity>());
  }
  void setupEnts(List<Entity> l){
    foreach(var e in l){
      foreach(var c in e.Components){
        if(c is PlayerCollider pc){
          var orig = pc.OnCollide;
          pc.OnCollide = (Player p)=>{
            if(!dreaming || !Eligible(p)){
              orig(p);
            }
          };
        }
      }
      if(e.Collider == null && e is not Wrappers.Spinner.SpinnerFiller) continue;
      if(e is Trigger t) continue;
      e.Add(new PlayerCollider((Player p)=>{
        if(DreamCheckStart(p,Vector2.Zero)){
          //DebugConsole.Write($"{e} {e.Collidable} {e.Scene}", e.Scene);
          fake.lastEntity = e;
        }
      }, colTypes.GetValueOrDefault(e.GetType())));
      e.Add(new DreamMarkerComponent(this,colTypes.GetValueOrDefault(e.GetType())));
      if(useVisuals && !(e is Template)){
        var comp = OverrideVisualComponent.Get(e);
        comp.AddToOverride(new(this,-30000,false,true));
        comp.AddToOverride(new(renderer,-999,dreaming,dreaming));
      }
    }
  }
  public override void OnNewEnts(List<Entity> l) {
    setupEnts(l);
    base.OnNewEnts(l);
  }
  public class DreamRenderer:BasicMaterialLayer{
    static VirtualShaderList effect = new VirtualShaderList{
      null,auspicioushelperGFX.LoadShader("misc/dream")
    };
    static VirtualShaderList reveffect = new VirtualShaderList{
      null,auspicioushelperGFX.LoadShader("misc/revdream")
    };
    bool reverse;
    public DreamRenderer(bool reverse):base(reverse?reveffect:effect,-11001){
      this.reverse=reverse;
    }
    public override void onRemove() {
      base.onRemove();
      if(reverse) revrender = null;
      else normrenderer = null;
    }
  }
  public static DreamRenderer normrenderer;
  public static DreamRenderer revrender;
  DreamRenderer renderer;
  public static TemplateDreamblockModifier speedSetter;
  public static Vector2 speedReplaceHook(Vector2 speed, Player p){
    if(speedSetter!=null){
      speed = speedSetter.getNewSpeed(speed, p);
      speedSetter = null;
    }
    return speed;
  }
  public Vector2 getNewSpeed(Vector2 speed, Player p){
    if(conserve) speed=p.Speed;
    return speed*(reverse? -1:1);
  }
  bool DreamCheckStart(Player p, Vector2 dir){
    bool flag = dreaming && p.Inventory.DreamDash && p.DashAttacking && (
      (dir == Vector2.Zero&&p.DashDir!=Vector2.Zero) || Vector2.Dot(dir, p.DashDir)>0
    );
    if(flag){
      speedSetter = this;
      p.StateMachine.State = Player.StDreamDash;
      speedSetter = null;
      p.dashAttackTimer = 0f;
      p.gliderBoostTimer = 0f;
      GetFromTree<ITemplateTriggerable>()?.OnTrigger(new DreamInfo(false, triggerOnEnter, p.DashDir));
      p.dreamBlock = fake;
    }
    return flag;
  }
  bool dreaming=true;
  static bool Eligible(Player p)=>p.StateMachine.state == Player.StDash || p.StateMachine.state == Player.StDreamDash || p.dashAttackTimer>0;
  static bool DDsh(Player p)=>p.StateMachine.state == Player.StDreamDash;
  public override void destroy(bool particles) {
    base.destroy(particles);
  }
  static void Hook(On.Celeste.Player.orig_OnCollideH orig, Player p, CollisionData d){
    if(!DDsh(p)){
      TemplateDreamblockModifier t = d.Hit.Get<ChildMarker>()?.parent.GetFromTree<TemplateDreamblockModifier>();
      while(t!=null){
        if(t.dreaming && t.DreamCheckStart(p,d.Direction)) return;
        t = t.parent?.GetFromTree<TemplateDreamblockModifier>();
      }
    }
    orig(p,d);
  }
  static void Hook(On.Celeste.Player.orig_OnCollideV orig, Player p, CollisionData d){
    if(!DDsh(p)){
      TemplateDreamblockModifier t = d.Hit.Get<ChildMarker>()?.parent.GetFromTree<TemplateDreamblockModifier>();
      while(t!=null){
        if(t.dreaming && t.DreamCheckStart(p,d.Direction)) return;
        t = t.parent?.GetFromTree<TemplateDreamblockModifier>();
      }
    }
    orig(p,d);
  }
  static void DDashIL(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After,
      i=>i.MatchStfld<Player>("dreamDashCanEndTimer"),
      i=>i.MatchCall<Entity>("CollideFirst")
    )){
      c.EmitLdarg0();
      c.EmitDelegate(DreamMarkerComponent.CheckContinue);
    } else DebugConsole.Write("failed to add hook",true);
  }
  static void DDashBeginIL(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      i=>i.MatchStfld<Player>(nameof(Player.Speed)),
      i=>i.MatchLdarg0(),
      i=>i.MatchLdcI4(1),
      i=>i.MatchStfld<Actor>(nameof(Player.TreatNaive))
    )){
      c.EmitLdarg0();
      c.EmitDelegate(speedReplaceHook);
    } else DebugConsole.WriteFailure("Failed to add hook",true);
  }
  static void DashDowndiag(ILContext ctx){
    ILCursor c = new (ctx);
    ILLabel target=null;
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<Entity>(nameof(CollideCheck)) &&
        itr.Operand is GenericInstanceMethod g && g.GenericArguments.Count==1 && g.GenericArguments[0].FullName==typeof(DreamBlock).FullName,
      itr=>itr.MatchBrtrue(out target)
    )){
      c.EmitLdloc1();
      c.EmitDelegate(DreamMarkerComponent.CheckDDiag);
      c.EmitBrtrue(target);
    } else DebugConsole.WriteFailure("Failed to add hook");
  }
  static void Hook(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock self, Player p){
    if(self is SentinalDb b){
      b.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new DreamInfo(true, b.parent.triggerOnLeave, p.DashDir));
      return;
    }
    orig(self,p);
  } 
  static ILHook ddhook;
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.OnCollideH+=Hook;
    On.Celeste.Player.OnCollideV+=Hook;
    On.Celeste.DreamBlock.OnPlayerExit+=Hook;
    IL.Celeste.Player.DreamDashUpdate+=DDashIL;
    IL.Celeste.Player.DreamDashBegin+=DDashBeginIL;
    MethodInfo dc = typeof(Player).GetMethod("DashCoroutine",BindingFlags.Instance | BindingFlags.NonPublic);
    MethodInfo dc2 = dc.GetStateMachineTarget();
    ddhook = new ILHook(dc2,DashDowndiag);
  },()=>{
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    On.Celeste.DreamBlock.OnPlayerExit-=Hook;
    IL.Celeste.Player.DreamDashUpdate-=DDashIL;
    IL.Celeste.Player.DreamDashBegin-=DDashBeginIL;
    ddhook?.Dispose();
  },auspicioushelperModule.OnEnterMap);
}
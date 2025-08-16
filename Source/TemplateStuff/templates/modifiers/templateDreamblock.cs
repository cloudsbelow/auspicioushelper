



using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
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
  public class DreamMarkerComponent:Component{
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
  }
  public override void addTo(Scene scene) {
    fake = (SentinalDb)RuntimeHelpers.GetUninitializedObject(typeof(SentinalDb));
    fake.parent = this;
    fake.Position = Vector2.Zero;
    fake.Collider = new Hitbox(2000000000,2000000000,-1000000000,-1000000000);
    if(renderer == null && useVisuals){
      MaterialPipe.addLayer(renderer = new DreamRenderer());
    } else if(useVisuals) MaterialPipe.addLayer(renderer);
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
      if(e.Collider == null) continue;
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
    public DreamRenderer():base(effect,-11001){}
    public override void onRemove() {
      base.onRemove();
      renderer = null;
    }
  }
  public static DreamRenderer renderer;
  bool DreamCheckStart(Player p, Vector2 dir){
    bool flag = dreaming && p.Inventory.DreamDash && p.DashAttacking && (
      (dir == Vector2.Zero&&p.DashDir!=Vector2.Zero) || Vector2.Dot(dir, p.DashDir)>0
    );
    if(flag){
      p.StateMachine.State = Player.StDreamDash;
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
      if(t!=null && t.dreaming && t.DreamCheckStart(p,d.Direction)) return;
    }
    orig(p,d);
  }
  static void Hook(On.Celeste.Player.orig_OnCollideV orig, Player p, CollisionData d){
    if(!DDsh(p)){
      TemplateDreamblockModifier t = d.Hit.Get<ChildMarker>()?.parent.GetFromTree<TemplateDreamblockModifier>();
      if(t!=null && t.dreaming && t.DreamCheckStart(p,d.Direction)) return;
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
    } else {
      DebugConsole.Write("failed to add hook");
    }
  }
  static void Hook(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock self, Player p){
    if(self is SentinalDb b){
      b.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new DreamInfo(true, b.parent.triggerOnLeave, p.DashDir));
      return;
    }
    orig(self,p);
  }
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.OnCollideH+=Hook;
    On.Celeste.Player.OnCollideV+=Hook;
    IL.Celeste.Player.DreamDashUpdate+=DDashIL;
    On.Celeste.DreamBlock.OnPlayerExit+=Hook;
  },()=>{
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    IL.Celeste.Player.DreamDashUpdate-=DDashIL;
    On.Celeste.DreamBlock.OnPlayerExit-=Hook;
  },auspicioushelperModule.OnEnterMap);
}
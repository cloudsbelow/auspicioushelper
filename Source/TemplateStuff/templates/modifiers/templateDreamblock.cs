



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
  public List<OverrideVisualComponent> comps  {get;set;}= new();
  public HashSet<OverrideVisualComponent> toRemove {get;} = new();
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
  public TemplateDreamblockModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateDreamblockModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    hooks.enable();
    triggerOnEnter = d.Bool("triggerOnEnter",true);
    triggerOnLeave = d.Bool("triggerOnLeave",true);
    dreaming = !d.Bool("useChannel",false);
  }
  public override void addTo(Scene scene) {
    fake = (SentinalDb)RuntimeHelpers.GetUninitializedObject(typeof(SentinalDb));
    fake.parent = this;
    fake.Position = Vector2.Zero;
    fake.Collider = new Hitbox(2000000000,2000000000,-1000000000,-1000000000);
    if(renderer == null){
      MaterialPipe.addLayer(renderer = new DreamRenderer());
    }

    base.addTo(scene);
    List<Entity> l = GetChildren<Entity>();
    foreach(var e in l){
      foreach(var c in e.Components){
        if(c is PlayerCollider pc){
          var orig = pc.OnCollide;
          pc.OnCollide = (Player p)=>{
            if(!Eligible(p)){
              orig(p);
            }
          };
        }
      }
      if(e.Collider == null) continue;
      e.Add(new PlayerCollider((Player p)=>{
        if(DreamCheckStart(p,Vector2.Zero)){
          DebugConsole.Write($"{e} {e.Collidable} {e.Scene}", e.Scene);
          fake.lastEntity = e;
        }
      }, colTypes.GetValueOrDefault(e.GetType())));
      e.Add(new DreamMarkerComponent(this,colTypes.GetValueOrDefault(e.GetType())));
      if(!(e is Template)){
        var c = new DreamRenderer.DreamOverride(this);
        e.Add(c);
        renderer.addEnt(c);
      }
    }
    (this as IOverrideVisuals).PrepareList(false);
  }
  public class DreamRenderer:BasicMaterialLayer{
    static VirtualShaderList effect = new VirtualShaderList{
      null,auspicioushelperGFX.LoadShader("misc/dream")
    };
    public class DreamOverride:OverrideVisualComponent{
      public bool dreaming=>((TemplateDreamblockModifier)parent).dreaming;
      public DreamOverride(TemplateDreamblockModifier parent):base(parent){}
    }
    public DreamRenderer():base(new VirtualShaderList{
      null,auspicioushelperGFX.LoadShader("misc/dream")
    },-11001){}
    public override void rasterMats(SpriteBatch sb, Camera c) {
      foreach(OverrideVisualComponent d in willdraw){
        if(d.shouldRemove)removeEnt(d);
        else if(d is DreamOverride m && m.dreaming) d.renderMaterial(this,c);
      }
    }
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
      DebugConsole.Write("entering");
      GetFromTree<ITemplateTriggerable>()?.OnTrigger(new DreamInfo(false, triggerOnEnter, p.DashDir));
      p.dreamBlock = fake;
    }
    return flag;
  }
  bool dreaming=true;
  static bool Eligible(Player p)=>p.StateMachine.state == Player.StDash || p.StateMachine.state == Player.StDreamDash;
  static bool DDsh(Player p)=>p.StateMachine.state == Player.StDreamDash;
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
      DebugConsole.Write("exiting");
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
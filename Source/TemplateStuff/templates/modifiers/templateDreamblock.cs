



using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
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
    public Collider Collider;
    public TemplateDreamblockModifier dbm;
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
  bool allowTransition;
  string customVisualGroup;
  bool priority;
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
    allowTransition = d.Bool("allowTransition",false);
    customVisualGroup = d.Attr("customVisualGroup", "");
    priority = d.Bool("priority",true);
  }
  public override void addTo(Scene scene) {
    fake = Util.GetUninitializedEntWithComp<SentinalDb>();
    fake.parent = this;
    fake.Position = Vector2.Zero;
    fake.Collider = new Hitbox(2000000000,2000000000,-1000000000,-1000000000);
    if(useVisuals){
      if(!string.IsNullOrWhiteSpace(customVisualGroup)){
        renderer = CustomDreamRenderer.Get(customVisualGroup);
      }else if(reverse){
        renderer = (revrender??=new DreamRenderer(true));
      } else {
        renderer = (normrenderer??=new DreamRenderer(false));
      }
      MaterialPipe.addLayer(renderer);
    } 
    if(!string.IsNullOrWhiteSpace(channel)){
      ChannelTracker ct = new ChannelTracker(channel,(double val)=>{
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
      if(e is Trigger t){
        if(t is DreamTrans dt && dt.assistSmuggle){
          dt.Add(new PlayerCollider((Player p)=>{
            if(p.StateMachine.State == Player.StNormal && p.Holding!=null && DreamCheckStart(p,Vector2.Zero)){
              fake.lastEntity = dt;
              dt.ManifestTemp();
            }
          }));
          dt.Add(dt.comp = new DreamMarkerComponent(this,dt.invalidHitbox));
        }
        continue;
      }
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
    public static VirtualShaderList cuseffect = new VirtualShaderList{
      null,auspicioushelperGFX.LoadShader("misc/customdream")
    };
    bool reverse;
    public DreamRenderer(bool reverse):base(reverse?reveffect:effect,-11001){
      this.reverse=reverse;
    }
    public override void onRemove() {
      base.onRemove();
      if(revrender == this) revrender = null;
      else if(normrenderer == this) normrenderer = null;
    }
  }
  [Import.SpeedrunToolIop.Static]
  public static BasicMaterialLayer normrenderer;
  [Import.SpeedrunToolIop.Static]
  public static BasicMaterialLayer revrender;
  BasicMaterialLayer renderer;
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
  static bool Eligible(Player p){
    int s = p.StateMachine.state;
    return s==Player.StDash || s==Player.StRedDash || s==Player.StDreamDash || s==Player.StStarFly || p.dashAttackTimer>0;
  }
  static bool DDsh(Player p)=>p.StateMachine.state == Player.StDreamDash;
  public override void destroy(bool particles) {
    base.destroy(particles);
  }
  static void LevelTransThing(Player p){
    if(p.dreamBlock is SentinalDb d && d.parent.allowTransition){
      goto yes;
    } else {
      // foreach(DreamMarkerComponent c in p.Scene.Tracker.GetComponents<DreamMarkerComponent>()){
      //   if(c.dbm.dreaming && !c.dbm.allowTransition && c.Entity.Collidable){
      //     bool flag;
      //     if(c.Collider!=null) flag = p.CollideCheck(c.Entity);
      //     else{
      //       Collider orig = c.Entity.Collider;
      //       c.Entity.Collider=c.Collider;
      //       flag = p.CollideCheck(c.Entity);
      //       c.Entity.Collider=orig;
      //     }
      //     if(flag) goto yes;
      //   }
      // }
    }
    foreach(DreamTrans t in p.Scene.Tracker.GetEntities<DreamTrans>()) if(p.CollideCheck(t)) goto yes;
    return;
    yes:
      var l = p.level;
      var b = p.level.Bounds;
      if(p.Right>b.Right && l.Session.MapData.GetAt(new Vector2(p.Right,p.CenterY)) is LevelData){
        p.Left=b.Right+1;
        l.NextLevel(p.Position,Vector2.One);
      }else if(p.Left<b.Left && l.Session.MapData.GetAt(new Vector2(p.Left,p.CenterY)) is LevelData){
        p.Right=b.Left-1;
        l.NextLevel(p.Position,Vector2.One);      
      }else if(p.Bottom>b.Bottom && l.Session.MapData.GetAt(new Vector2(p.CenterX,p.Bottom)) is LevelData){
        p.Top=b.Bottom+1;
        l.NextLevel(p.Position,Vector2.One);      
      }else if(p.Top<b.Top && l.Session.MapData.GetAt(new Vector2(p.CenterX,p.Top)) is LevelData){
        p.Bottom=b.Top-1;
        l.NextLevel(p.Position,Vector2.One);      
      }
  }
  [CustomEntity("auspicioushelper/DreamTransitionEnabler")]
  [Tracked]
  public class DreamTrans:Trigger{
    public bool assistSmuggle;
    public Hitbox invalidHitbox = new Hitbox(-100,-100,-10000,0);
    public DreamMarkerComponent comp;
    public bool prio;
    public DreamTrans(EntityData d, Vector2 o):base(d,o){
      hooks.enable();
      assistSmuggle = d.Bool("assistSmuggle",false);
      prio = d.Bool("prioTemplateDreamblocks",false);
    }
    public void ManifestTemp(){
      comp.Collider = Collider;
      Add(new Coroutine(ManifestRoutine()));
    }
    IEnumerator ManifestRoutine(){
      yield return 0.2f;
      comp.Collider = invalidHitbox;
    }
  }

  static bool prioThing(Player p, CollisionData d){
    if(!p.Inventory.DreamDash || !p.DashAttacking) return false;
    Vector2 cpos = p.Position+d.Direction;
    foreach(DreamTrans dt in p.Scene.Tracker.GetEntities<DreamTrans>()) if(dt.prio && p.CollideCheck(dt)){
      foreach(DreamMarkerComponent c in p.Scene.Tracker.GetComponents<DreamMarkerComponent>()){
        if(!c.dbm.dreaming || c.Entity.Collidable == false) continue;
        bool flag=false;
        if(c.Collider==null) flag = p.CollideCheck(c.Entity,cpos);
        else{
          Collider og = c.Entity.Collider;
          c.Entity.Collider=c.Collider;
          flag = p.CollideCheck(c.Entity);
          c.Entity.Collider=og;
        }
        if(flag && c.dbm.DreamCheckStart(p,d.Direction)) return true;
      }
      break;
    }
    return false;
  }
  static void Hook(On.Celeste.Player.orig_OnCollideH orig, Player p, CollisionData d){
    if(!DDsh(p)){
      TemplateDreamblockModifier t = d.Hit.Get<ChildMarker>()?.parent.GetFromTree<TemplateDreamblockModifier>();
      while(t!=null){
        if(t.dreaming && t.DreamCheckStart(p,d.Direction)) return;
        t = t.parent?.GetFromTree<TemplateDreamblockModifier>();
      }
      if(prioThing(p,d)) return;
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
      if(prioThing(p,d)) return;
    }
    orig(p,d);
  }
  static void Hook(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock self, Player p){
    if(self is SentinalDb b){
      b.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new DreamInfo(true, b.parent.triggerOnLeave, p.DashDir));
      return;
    }
    orig(self,p);
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
      i=>i.MatchStfld<Actor>(nameof(Actor.TreatNaive))
    )){
      c.EmitLdarg0();
      c.EmitDelegate(speedReplaceHook);
    } else DebugConsole.WriteFailure("Failed to add hook (dreamdash begin)",true);
  }
  static void DashDowndiag(ILContext ctx){
    ILCursor c = new (ctx);
    ILLabel target=null;
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<Entity>(nameof(CollideCheck)) &&
        itr.Operand is GenericInstanceMethod g && 
        g.GenericArguments.Count==1 && 
        g.GenericArguments[0].FullName==typeof(DreamBlock).FullName,
      itr=>itr.MatchBrtrue(out target)
    )){
      c.EmitLdloc1();
      c.EmitDelegate(DreamMarkerComponent.CheckDDiag);
      c.EmitBrtrue(target);
    } else if((c=new(ctx)).TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchLdfld<PlayerInventory>(nameof(PlayerInventory.DreamDash))
    ) && c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchBrtrue(out target),
      itr=>itr.MatchLdloc1(),
      itr=>itr.MatchLdflda<Player>(nameof(Player.DashDir))
    )) {
      c.EmitBrtrue(target);
      c.EmitLdloc1();
      c.EmitDelegate(DreamMarkerComponent.CheckDDiag);
    } else DebugConsole.WriteFailure("Failed to add hook (dreamdash downdiag)",true);
  }
  static ILHook ddhook;
  static void TransitionHook(ILContext ctx){    
    ILCursor c = new(ctx);
    ILLabel otarg = null;

    if(!c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<Level>(nameof(Level.EnforceBounds))
    )) goto bad;
    var d = c.Clone();
    if(!d.TryGotoPrevBestFit(MoveType.After,
      itr=>itr.MatchLdcI4(Player.StDreamDash),
      itr=>itr.MatchBeq(out otarg)
    )) goto bad;

    d.Index--;
    var itr = d.Instrs[d.Index];
    c.EmitBr(otarg);
    c.EmitLdarg0();
    c.EmitDelegate(LevelTransThing);
    itr.Operand = c.Instrs[c.Index-2];

    return;
    bad:
      DebugConsole.WriteFailure("Could not add room transition enabling hook",true);
  }
  static ILHook transhook;
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.OnCollideH+=Hook;
    On.Celeste.Player.OnCollideV+=Hook;
    On.Celeste.DreamBlock.OnPlayerExit+=Hook;
    IL.Celeste.Player.DreamDashUpdate+=DDashIL;
    IL.Celeste.Player.DreamDashBegin+=DDashBeginIL;
    MethodInfo dc = typeof(Player).GetMethod("DashCoroutine",BindingFlags.Instance | BindingFlags.NonPublic);
    MethodInfo dc2 = dc.GetStateMachineTarget();
    ddhook = new ILHook(dc2,DashDowndiag);
    transhook = new ILHook(typeof(Player).GetMethod(nameof(Player.orig_Update)),TransitionHook);
  },()=>{
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    On.Celeste.DreamBlock.OnPlayerExit-=Hook;
    IL.Celeste.Player.DreamDashUpdate-=DDashIL;
    IL.Celeste.Player.DreamDashBegin-=DDashBeginIL;
    ddhook?.Dispose();
    transhook?.Dispose();
  },auspicioushelperModule.OnEnterMap);

  [CustomEntity("auspicioushelper/CustomDreamlayer")]
  [CustomloadEntity]
  [MapenterEv(nameof(Setup))]
  public class CustomDreamRenderer:BasicMaterialLayer{
    Color[] colors = new Color[6];
    Color border;
    Color inside;
    float contentAlpha;
    float[] density;
    [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
    [Import.SpeedrunToolIop.Static]
    static Dictionary<string, CustomDreamRenderer> layers = new();
    public static BasicMaterialLayer Get(string ident){
      if(layers.TryGetValue(ident.Trim(), out var l)) return l;
      DebugConsole.MakePostcard($"No dreamblock visual settings with identifier "+ident+" in the map");
      return new DreamRenderer(false);
    }
    static void Setup(EntityData d){
      string ident = d.Attr("identifier","ident");
      if(layers.ContainsKey(ident)){
        if(auspicioushelperModule.InFolderMod) DebugConsole.MakePostcard(
          $"Found duplicate dreamblock configurator for identifier "+ident+". These are global for the map; use one per identifier.");
      } else {
        var l = new CustomDreamRenderer(d);
        if(ident == "normal") normrenderer = l;
        else if(ident == "reverse") revrender = l;
        layers.Add(ident,l);
      }
    }
    public CustomDreamRenderer(EntityData d):base(DreamRenderer.cuseffect,d.Int("depth")){
      for(int i=0; i<colors.Length; i++) colors[i] = Util.hexToColor(d.Attr("color_"+i,"aaa"));
      border = Util.hexToColor(d.Attr("edgeColor","#fff"));
      inside = Util.hexToColor(d.Attr("insideColor","#000"));
      density = Util.csparseflat(d.Attr("density"),3,-1,-1);
      if(density[1]==-1) density[1]=density[0];
      if(density[2]==-1) density[2]=density[0];
      contentAlpha = d.Float("contentAlpha",0.15f);
    }
    public override void render(SpriteBatch sb, Camera c) {
      for(int i=0; i<colors.Length; i++) passes.setparamvalex("color"+i, colors[i].ToVector4());
      passes.setparamvalex("edge", border.ToVector4());
      passes.setparamvalex("inside", inside.ToVector4());
      passes.setparamvalex("density", density);
      passes.setparamvalex("thru",contentAlpha);
      base.render(sb, c);
    }
  }
}
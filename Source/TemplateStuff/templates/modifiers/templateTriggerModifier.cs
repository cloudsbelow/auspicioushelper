


using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AsmResolver.Collections;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

public interface ITemplateTriggerable{
  void OnTrigger(TriggerInfo s);
}

public abstract class TriggerInfo{
  public Template parent;
  public Entity entity;
  public class SmInfo:TriggerInfo{
    public SmInfo(Template p, Entity e){
      this.parent = p; this.entity = e;
    }
    public static SmInfo getInfo(StaticMover sm){
      var smd = new DynamicData(sm);
      if(smd.TryGet<SmInfo>("__auspiciousSM", out var info)){
        return info;
      }
      return new SmInfo(null,sm.Entity);
    }
    public override string category => "staticmover/"+entity.ToString();
  }
  public class EntInfo:TriggerInfo{
    string ev;
    bool use = true;
    public EntInfo(string type, Entity e, bool use=true){
      ev = type;
      entity=e;
      this.use=use;
    }
    public override bool shouldTrigger => use;
    public override string category=>"entity/"+ev;
  }
  public virtual bool shouldTrigger=>true;
  public static bool TestPass(TriggerInfo info, Template t){
    bool use = info == null || info.shouldTrigger;
    if(!use) t.parent?.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(info);
    return use;
  }
  public static bool Test(TriggerInfo info)=>info == null || info.shouldTrigger;
  public void Pass(ITemplateChild t){
    t.parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(this);
  }
  public void PassTo(Template t){
    t?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(this);
  }
  public bool TestPass(Template t){
    return TestPass(this,t);
  }
  public abstract string category {get;}
}


[CustomEntity("auspicioushelper/TemplateTriggerModifier")]
[MapenterEv(nameof(Search))]
public class TemplateTriggerModifier:Template, ITemplateTriggerable{
  static void Search(EntityData d){
    foreach(var id in Util.listparseflat(d.Attr("collideWith","")))Finder.enqueueIdent(id);
  }
  bool triggerOnTouch;
  Util.Trie<bool> advtouch = new(true);
  bool passTrigger;
  bool hideTrigger;
  bool blockTrigger;
  string channel;
  float delay;
  bool log;
  Util.Trie blockManager;
  ChannelState.AdvancedSetter adv = null;
  ChannelState.BoolCh skip;
  bool neverTriggerOnAwake = false;
  List<string> onCollidePaths = null;
  static readonly List<string> list = new(){
    "Normal","Climb","Dash","Swim","Boost","RedDash","HitSquash","Launch","Pickup","DreamDash","SummitLaunch",
    "Dummy","IntroWalk","IntroJump","IntroRespawn","IntroWakeUp","BirdDashTutorial","Frozen","ReflectionFall",
    "StarFly","TempleFall","CassetteFly","Attract","IntroMoonJump","FlingBird","IntroThinkForABit"
  };
  public TemplateTriggerModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateTriggerModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    triggerOnTouch = d.Bool("triggerOnTouch",false);
    log = d.Bool("log",false);
    foreach(string s in Util.listparseflat(d.Attr("advancedTouchOptions",""),true)){
      if(Util.removeWhitespace(s)=="*"){
        triggerOnTouch = true;
        continue;
      }
      if(s.StartsWith('/')){
        advtouch.Add(s.Substring(1)+"*",true);
        continue;
      }
      advtouch.Add("touch/"+s+"*",true);
      string[] strs = s.Split('/');
      if(Enum.TryParse<TouchInfo.Type>(strs[0],out var res)){
        for(int i=1; i<strs.Length; i++) if(int.TryParse(strs[i], out int st))advtouch.Add("touch/"+strs[0]+"/"+list[st],true);
      }
    }
    if(d.Bool("seekersTrigger",false) && !triggerOnTouch) advtouch.Add("touch/SeekerSlam*",true);
    if(d.Bool("holdablesTrigger",false) && !triggerOnTouch) advtouch.Add("touch/HoldableHit*",true);
    if(triggerOnTouch || advtouch.hasStuff || log) hooks.enable();
    channel = d.Attr("channel",null);
    if(!d.Bool("propagateRiding",true)) prop &= ~Propagation.Riding;
    if(!d.Bool("propagateInside",true)) prop &= ~Propagation.Inside;
    if(!d.Bool("propagateShake",true)) prop &= ~Propagation.Shake;
    if(!d.Bool("propagateDashHit",true)) prop &= ~Propagation.DashHit;
    passTrigger = d.Bool("propagateTrigger",false);
    hideTrigger = d.Bool("hideTrigger",false);
    blockTrigger = d.Bool("blockTrigger",false);
    delay = d.Float("delay",-1);
    if(delay>=0) delayed = new();
    foreach(string s in Util.listparseflat(d.Attr("blockFilter"),true,true)){
      if(Util.removeWhitespace(s)=="*"){
        blockTrigger = true; continue;
      }
      if(blockManager == null) blockManager = new();
      blockManager.Add(s);
    }
    if(!string.IsNullOrWhiteSpace(d.Attr("setChannel",""))) adv = new(d.Attr("setChannel",""));
    OnDashCollide = handleDash;
    skip = d.ChannelBool("skipChannel",false);
    neverTriggerOnAwake = d.Bool("neverTriggerOnAwake",false);
    string paths = d.String("collideWith",null);
    if(paths!=null) onCollidePaths = Util.listparseflat(paths);
  }
  DashCollisionResults handleDash(Player player, Vector2 direction){
    if((prop&Propagation.DashHit) != Propagation.None && (parent!=null)){
      OnTrigger(new TouchInfo(player, direction.X!=0?TouchInfo.Type.dashH:TouchInfo.Type.dashV));
      if(parent.OnDashCollide != null) return parent.OnDashCollide(player, direction);
      return ((ITemplateChild)parent).propagateDashhit(player, direction);
    }
    return DashCollisionResults.NormalOverride;
  }
  ITemplateTriggerable triggerParent;
  TemplateTriggerModifier modifierParent;
  UpdateHook upd;
  ChannelTracker triggerCh;
  public override void addTo(Scene scene) {
    if(delay>=0) Add(upd = new UpdateHook());
    triggerParent = parent?.GetFromTree<ITemplateTriggerable>();
    modifierParent = parent?.GetFromTree<TemplateTriggerModifier>();
    if(!string.IsNullOrWhiteSpace(channel)){
      Add(triggerCh = new ChannelTracker(channel, (double val)=>{
        if(val!=0) OnTrigger(new ChannelInfo(channel));
      }));
    }
    base.addTo(scene);
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(!neverTriggerOnAwake && triggerCh!=null && triggerCh.value!=0){
      UpdateHook.AddAfterUpdate(()=>{
        if(triggerCh.value!=0) OnTrigger(new ChannelInfo(channel));
      });
    }
  }

  Queue<Tuple<float, TriggerInfo>> delayed;
  float activeTime = 0;
  public void HandleTrigger(TriggerInfo sm){
    if(sm is IUsable tinfo){
      bool has = advtouch.GetOrDefault(sm.category);
      if(has!=triggerOnTouch && (!triggerOnTouch || sm is TouchInfo)) tinfo.asUsable();
    }
    if(blockTrigger!=(blockManager?.Test(sm?.category)??false)) goto end;
    if(triggerParent == null) goto end;
    if(hideTrigger){
      modifierParent?.OnTrigger(sm);
      goto end;
    }
    if(TriggerInfo.Test(sm)){
      if(passTrigger)triggerParent.OnTrigger(sm);
    } else modifierParent?.OnTrigger(sm);
    end:
      if(TriggerInfo.Test(sm)) adv?.Apply();
  }
  public void OnTrigger(TriggerInfo sm){
    if(skip){
      triggerParent?.OnTrigger(sm);
      return;
    }
    if(log) DebugConsole.Write($"From trigger modifier: ",sm?.category);
    if(delay<0) HandleTrigger(sm);
    else{
      if(upd.updatedThisFrame) delayed.Enqueue(new(activeTime+delay,sm));
      else delayed.Enqueue(new(activeTime+delay+Engine.DeltaTime,sm));
    }
  }
  public override void Update() {
    base.Update();
    if(onCollidePaths != null){
      FloatRect bounds = FloatRect.empty;
      List<Collider> co = new();
      foreach(var v in GetChildren<Solid>(Propagation.Shake)) if(v.Collidable){
        bounds=bounds._union(new FloatRect(v));
        co.Add(v.Collider);
      }
      if(co.Count==0) goto endCC;
      Util.LazyList<Collider> cs = new();
      foreach(string s in onCollidePaths) if(FoundEntity.find(s) is {} comp){
        var e = comp.Entity;
        if(e is Template to) foreach(var en in to.GetChildren<Entity>()){
          var col = en.Collider;
          if(en.Collidable && bounds.CollideCollider(col) && col!=null) cs.Add(col);
        } else if(e.Collidable && bounds.CollideCollider(e.Collider)){
          if(e.Collider!=null)cs.Add(e.Collider);
        }
      }
      if(cs.Count==0) goto endCC;
      foreach(var o in co) foreach(var s in cs) if(o.Collide(s)){
        OnTrigger(null);
        goto endCC;
      }
      endCC:;
    }

    
    if(delay<0) return;
    activeTime+=Engine.DeltaTime;
    while(delayed.Count>0 && activeTime>delayed.Peek().Item1){
      HandleTrigger(delayed.Dequeue().Item2);
    }
  }
  interface IUsable{
    TriggerInfo asUsable();
  }
  public class TouchInfo:TriggerInfo, IUsable{
    public enum Type {
      collideV, collideH, jump, climbjump, walljump, wallbounce, super, grounded, climbing , dashH, dashV,
      invalid, FishExplosion, SeekerExplosion, bumper, SeekerSlam,HoldableHit
    }
    public Type ty;
    public bool use = false;
    public TouchInfo(Entity p, Type t){
      entity = p;
      ty=t;
    }
    public override bool shouldTrigger => use;
    public TriggerInfo asUsable(){
      use = ty!=Type.invalid;
      return this;
    }
    public override string category => "touch/"+ty.ToString()+(entity is Player p?$"/{p.StateMachine.GetCurrentStateName()}":"");
  }
  class ChannelInfo:TriggerInfo{
    public string channel;
    public ChannelInfo(string ch){
      channel = ch;
    }
    public override string category => "channel/"+channel;
  }
  static void triggerFromArr(List<Entity> l, TouchInfo t){
    foreach(Entity p in l) p.Get<ChildMarker>()?.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(t);
  }
  static void Hook(On.Celeste.Player.orig_Jump orig, Player p, bool a, bool b){
    bool useCoyote = p.jumpGraceTimer>0 && p.jumpGraceTimer<0.1;
    orig(p,a,b);
    if(a && b)triggerFromArr(p.temp,new TouchInfo(p,TouchInfo.Type.jump));
    if(useCoyote){
      p.Get<CoyotePlatformMarker>()?.p?.Get<ChildMarker>()?.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new TouchInfo(p,TouchInfo.Type.jump));
    }
  }
  static void Hook(On.Celeste.Player.orig_SuperJump orig, Player p){
    orig(p);
    triggerFromArr(p.temp, new TouchInfo(p,TouchInfo.Type.super));
    p.Get<CoyotePlatformMarker>()?.p?.Get<ChildMarker>()?.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new TouchInfo(p,TouchInfo.Type.super));
  }
  static void Hook(On.Celeste.Player.orig_SuperWallJump orig, Player p, int direction){
    orig(p,direction);
    triggerFromArr(p.temp, new TouchInfo(p, TouchInfo.Type.wallbounce));
  }
  static void Hook(On.Celeste.Player.orig_WallJump orig, Player p, int direction){
    orig(p,direction);
    triggerFromArr(p.temp, new TouchInfo(p, TouchInfo.Type.walljump));
  }
  static void Hook(On.Celeste.Player.orig_ClimbJump orig, Player p){
    orig(p);
    triggerFromArr(p.CollideAll<Platform>(p.Position+Vector2.UnitX*(float)p.Facing*4f),new TouchInfo(p,TouchInfo.Type.climbjump));
  }
  static void Hook(On.Celeste.Player.orig_OnCollideH orig, Player p, CollisionData c){
    orig(p,c);
    c.Hit.Get<ChildMarker>()?.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new TouchInfo(p,TouchInfo.Type.collideH));
  }
  static void Hook(On.Celeste.Player.orig_OnCollideV orig, Player p, CollisionData c){
    orig(p,c);
    c.Hit.Get<ChildMarker>()?.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new TouchInfo(p,TouchInfo.Type.collideV));
  }
  static void Hook(On.Celeste.Player.orig_Update orig, Player p){
    orig(p);
    if(p.onGround){
      triggerFromArr(p.CollideAll<Platform>(p.Position + Vector2.UnitY),new TouchInfo(p, TouchInfo.Type.grounded));
    }
    if(p.StateMachine.State == Player.StClimb){
      triggerFromArr(p.CollideAll<Platform>(p.Position + Vector2.UnitX*(float)p.Facing), new TouchInfo(p, TouchInfo.Type.climbing));
    }
  }
  public static void ExplodeThing(Entity e){
    foreach(Solid s in e.Scene.Tracker.GetEntities<Solid>()) if(e.CollideCheck(s)){
      s.Get<ChildMarker>()?.parent?.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new TouchInfo(e, e switch{
        Seeker=>TouchInfo.Type.SeekerExplosion,
        Puffer=>TouchInfo.Type.FishExplosion,
        _=>TouchInfo.Type.invalid
      }));
    }
  }
  [OnLoad.ILHook(typeof(Seeker),nameof(Seeker.RegenerateCoroutine),Util.HookTarget.Coroutine)]
  [OnLoad.ILHook(typeof(Puffer),nameof(Puffer.Explode))]
  static void ExplodeHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchCallvirt<Entity>("get_Scene")||itr.MatchCall<Entity>("get_Scene"),
      itr=>itr.MatchCallvirt<Scene>("get_Tracker"),
      itr=>itr.MatchCallvirt<Tracker>(nameof(Tracker.GetEntities)) &&
        itr.Operand is GenericInstanceMethod g && 
        g.GenericArguments.Count==1 && 
        g.GenericArguments[0].FullName==typeof(TempleCrackedBlock).FullName
    )){
      c.EmitDup();
      c.EmitDelegate(ExplodeThing);
    } else DebugConsole.WriteFailure("Failed to apply explosion hooks",true);
  }

  static void HitDelegate(Platform h, Actor a, bool vertical){
    List<TouchInfo> ts=new();
    if(a is Seeker seeker){
      bool attack = seeker.State.State==Seeker.StAttack||seeker.State.State==Seeker.StSkidding;
      if(Math.Abs(seeker.Speed.X)>100 && attack) {
        ts.Add(new TouchInfo(seeker,TouchInfo.Type.SeekerSlam));
      }
    }
    if(a.Get<Holdable>() is {} hold){
      ts.Add(new TouchInfo(a,TouchInfo.Type.HoldableHit));
    }

    if(ts.Count>0 && h.Get<ChildMarker>() is ChildMarker c) foreach(var t in ts){
      c.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(t);
    } 
  }
  static void HookMoveH(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After, i=>i.MatchLdloc3(), i=>i.MatchStfld<CollisionData>("Hit"))){
      c.EmitLdloc3();
      c.EmitLdarg0();
      c.EmitLdcI4(0);
      c.EmitDelegate(HitDelegate);
    } else DebugConsole.WriteFailure("Failed to make actor moveH IL hook for triggerModifier",true);
  }
  static void HookMoveV(ILContext ctx){
    ILCursor c = new(ctx);
    while(c.TryGotoNextBestFit(MoveType.After, i=>i.MatchLdloc3(), i=>i.MatchStfld<CollisionData>("Hit"))){
      c.EmitLdloc3();
      c.EmitLdarg0();
      c.EmitLdcI4(1);
      c.EmitDelegate(HitDelegate);
    }
  }
  public class CoyotePlatformMarker:Component{
    public Platform p = null;
    public CoyotePlatformMarker():base(false,false){}
    public void setPlatform(Platform p)=>this.p=p;
  }
  static void setCoyotePlatform(Player player, Platform platform){
    CoyotePlatformMarker m = player.Get<CoyotePlatformMarker>();
    if(m==null) player.Add(m = new CoyotePlatformMarker());
    m.setPlatform(platform);
  }
  static void HookCoyote(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After,i=>i.MatchLdcI4(1),i=>i.MatchStfld<Player>("onGround"))){
      //DebugConsole.DumpIl(c);
      c.EmitLdarg0();
      c.EmitLdloc1();
      c.EmitDelegate(setCoyotePlatform);
    } else DebugConsole.WriteFailure("\n\n Failed to add coyote hooks \n\n",true);
  }
  static ILHook coyoteHook;
  static HookManager hooks = new(()=>{
    On.Celeste.Player.Jump+=Hook;
    On.Celeste.Player.SuperJump+=Hook;
    On.Celeste.Player.WallJump+=Hook;
    On.Celeste.Player.SuperWallJump+=Hook;
    On.Celeste.Player.ClimbJump+=Hook;
    On.Celeste.Player.OnCollideH+=Hook;
    On.Celeste.Player.OnCollideV+=Hook;
    On.Celeste.Player.Update+=Hook;

    IL.Celeste.Actor.MoveHExact+=HookMoveH;
    IL.Celeste.Actor.MoveVExact+=HookMoveV;
    MethodInfo update = typeof(Player).GetMethod(
      "orig_Update", BindingFlags.Public |BindingFlags.Instance
    );
    coyoteHook = new ILHook(update, HookCoyote);
  },void ()=>{
    On.Celeste.Player.Jump-=Hook;
    On.Celeste.Player.SuperJump-=Hook;
    On.Celeste.Player.WallJump-=Hook;
    On.Celeste.Player.SuperWallJump-=Hook;
    On.Celeste.Player.ClimbJump-=Hook;
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    On.Celeste.Player.Update-=Hook;
    
    IL.Celeste.Actor.MoveHExact-=HookMoveH;
    IL.Celeste.Actor.MoveVExact-=HookMoveV;
    coyoteHook.Dispose();
  }, auspicioushelperModule.OnEnterMap);
}
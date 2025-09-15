


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
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
public class TemplateTriggerModifier:Template, ITemplateTriggerable{
  bool triggerOnTouch;
  Dictionary<TouchInfo.Type,HashSet<int>> advtouch = new();
  bool passTrigger;
  bool hideTrigger;
  bool blockTrigger;
  string channel;
  float delay;
  bool log;
  Util.Trie blockManager;
  string setCh;
  ChannelState.AdvancedSetter adv = null;
  bool seekersTrigger = false;
  bool throwablesTrigger = false;
  string skipCh;
  bool skip = false;

  public TemplateTriggerModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateTriggerModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    foreach(string s in Util.listparseflat(d.Attr("advancedTouchOptions",""),true)){
      string[] strs = s.Split('/');
      if(Enum.TryParse<TouchInfo.Type>(strs[0],out var res)){
        if(!advtouch.TryGetValue(res, out var n)) n = advtouch[res] = new();
        if(strs.Length == 1) n = advtouch[res] = null;
        if(n!=null){
          for(int i=1; i<strs.Length; i++) if(int.TryParse(strs[i], out int st)){
            DebugConsole.Write($"Add {res} {st}");
            n.Add(st);
          } 
        }
      }
    }
    triggerOnTouch = d.Bool("triggerOnTouch",false);
    seekersTrigger = d.Bool("seekersTrigger",false);
    throwablesTrigger = d.Bool("holdablesTrigger",false);
    if(triggerOnTouch || advtouch.Count>0 || seekersTrigger || throwablesTrigger) hooks.enable();
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
      if(blockManager == null) blockManager = new();
      blockManager.Add(s);
    }
    log = d.Bool("log",false);
    if(d.Bool("useAdvancedSetch") && !string.IsNullOrWhiteSpace(d.Attr("setChannel",""))) adv = new(d.Attr("setChannel",""));
    else setCh = d.Attr("setChannel","");
    OnDashCollide = handleDash;
    skipCh = d.Attr("skipChannel","");
  }
  DashCollisionResults handleDash(Player player, Vector2 direction){
    if((prop&Propagation.DashHit) != Propagation.None && (parent!=null)){
      OnTrigger(new TouchInfo(player, direction.X!=0?TouchInfo.Type.dashH:TouchInfo.Type.dashV));
      if(parent.OnDashCollide != null) return parent.OnDashCollide(player, direction);
      return ((ITemplateChild)parent).propagateDashhit(player, direction);
    }
    return DashCollisionResults.NormalCollision;
  }
  ITemplateTriggerable triggerParent;
  TemplateTriggerModifier modifierParent;
  UpdateHook upd;
  public override void addTo(Scene scene) {
    if(delay>=0) Add(upd = new UpdateHook());
    triggerParent = parent?.GetFromTree<ITemplateTriggerable>();
    modifierParent = parent?.GetFromTree<TemplateTriggerModifier>();
    if(!string.IsNullOrWhiteSpace(channel)){
      Add(new ChannelTracker(channel, (int val)=>{
        if(val!=0) OnTrigger(new ChannelInfo(channel));
      },true));
    }
    if(!string.IsNullOrWhiteSpace(skipCh)){
      Add(new ChannelTracker(skipCh, (int val)=>{
        skip = val!=0;
      }, true));
    }
    base.addTo(scene);
  }

  Queue<Tuple<float, TriggerInfo>> delayed;
  float activeTime = 0;
  public void HandleTrigger(TriggerInfo sm){
    if(sm is TouchInfo tinfo){
      bool has = advtouch.TryGetValue(tinfo.ty,out var l) && 
        (l==null || (sm.entity is Player p && l.Contains(p.StateMachine.state)));
      if(has!=triggerOnTouch)tinfo.asUsable();
    } 
    if(sm is HitInfo hinfo){
      if(seekersTrigger && hinfo.entity is Seeker seeker && Math.Abs(seeker.Speed.X)>100) {
        if(seeker.State.State==Seeker.StAttack||seeker.State.State==Seeker.StSkidding)hinfo.asUsable();
      }
      if(throwablesTrigger && hinfo.entity.Get<Holdable>()!=null) hinfo.asUsable();
    }
    if(triggerParent == null) goto end;
    if(hideTrigger){
      modifierParent?.OnTrigger(sm);
      goto end;
    }
    if(sm is TouchInfo tinf_){
      if(tinf_.use)triggerParent.OnTrigger(tinf_);
      else modifierParent?.OnTrigger(tinf_);
    } else {
      if(passTrigger)triggerParent.OnTrigger(sm);
    }
    end:
      if(!string.IsNullOrWhiteSpace(setCh) && TriggerInfo.Test(sm)) ChannelState.SetChannel(setCh,1);
      adv?.Apply();
  }
  public void OnTrigger(TriggerInfo sm){
    if(skip){
      triggerParent.OnTrigger(sm);
      return;
    }
    if(log) DebugConsole.Write($"From trigger modifier: ",sm?.category);
    if(blockTrigger!=(blockManager?.Test(sm?.category)??false)) return;
    if(delay<0) HandleTrigger(sm);
    else{
      if(upd.updatedThisFrame) delayed.Enqueue(new(activeTime+delay,sm));
      else delayed.Enqueue(new(activeTime+delay+Engine.DeltaTime,sm));
    }
  }
  public override void Update() {
    base.Update();
    if(delay<0) return;
    activeTime+=Engine.DeltaTime;
    while(delayed.Count>0 && activeTime>delayed.Peek().Item1){
      HandleTrigger(delayed.Dequeue().Item2);
    }
  }
  class TouchInfo:TriggerInfo{
    public enum Type {
      collideV, collideH, jump, climbjump, walljump, wallbounce, super, grounded, climbing , dashH, dashV
    }
    public Type ty;
    public bool use = false;
    public TouchInfo(Player p, Type t){
      entity = p;
      ty=t;
    }
    public override bool shouldTrigger => use;
    public TouchInfo asUsable(){
      use = true;
      return this;
    }
    public override string category => "touch/"+ty.ToString()+(entity is Player p?$"/{p.StateMachine.state}":"");
  }
  class HitInfo:TriggerInfo{
    public bool use = false;
    public override bool shouldTrigger => use;
    bool horizontal;
    public HitInfo(Template parent, Actor a, bool horizontal):base(){
      entity=a;this.parent=parent;this.horizontal=horizontal;
    }
    public HitInfo asUsable(){
      use=true;
      return this;
    }
    public override string category=>"hit/"+(horizontal?"h/":"v/")+entity;
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
    triggerFromArr(p.temp,new TouchInfo(p,TouchInfo.Type.climbjump));
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

  static void MoveHDelegate(Platform h, Actor a){
    if(h.Get<ChildMarker>() is ChildMarker c) c.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new HitInfo(c.parent,a,true));
  }
  static void HookMoveH(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After, i=>i.MatchLdloc3(), i=>i.MatchStfld<CollisionData>("Hit"))){
      c.EmitLdloc3();
      c.EmitLdarg0();
      c.EmitDelegate(MoveHDelegate);
    } else DebugConsole.WriteFailure("Failed to make actor moveH IL hook for triggerModifier");
  }
  static void MoveVDelegate(Platform h, Actor a){
    if(h.Get<ChildMarker>() is ChildMarker c) c.parent.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new HitInfo(c.parent,a,false));
  }
  static void HookMoveV(ILContext ctx){
    ILCursor c = new(ctx);
    while(c.TryGotoNextBestFit(MoveType.After, i=>i.MatchLdloc3(), i=>i.MatchStfld<CollisionData>("Hit"))){
      c.EmitLdloc3();
      c.EmitLdarg0();
      c.EmitDelegate(MoveVDelegate);
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
    } else DebugConsole.WriteFailure("\n\n Failed to add coyote hooks \n\n");
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
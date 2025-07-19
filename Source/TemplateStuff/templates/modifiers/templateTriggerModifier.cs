


using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
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
    public EntInfo(string type, Entity e){
      ev = type;
      entity=e;
    }
    public override string category=>"entity/"+ev;
  }
  public virtual bool shouldTrigger=>true;
  public static bool TestPass(TriggerInfo info, Template t){
    bool use = info == null || info.shouldTrigger;
    if(!use) t.parent?.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(info);
    return use;
  }
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
  HashSet<TouchInfo.Type> advtouch = new();
  bool passTrigger;
  bool hideTrigger;
  bool blockTrigger;
  string channel;
  float delay;
  bool log;
  Util.Trie blockManager;

  public TemplateTriggerModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateTriggerModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    foreach(string s in Util.listparseflat(d.Attr("advancedTouchOptions",""),true)){
      if(Enum.TryParse<TouchInfo.Type>(s,out var res))advtouch.Add(res);
    }
    triggerOnTouch = d.Bool("triggerOnTouch",false);
    if(triggerOnTouch || advtouch.Count>0) hooks.enable();
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
  }
  ITemplateTriggerable triggerParent;
  TemplateTriggerModifier modifierParent;
  UpdateHook upd;
  public override void addTo(Scene scene) {
    base.addTo(scene);
    triggerParent = parent?.GetFromTree<ITemplateTriggerable>();
    modifierParent = parent?.GetFromTree<TemplateTriggerModifier>();
    if(!string.IsNullOrWhiteSpace(channel)){
      Add(new ChannelTracker(channel, (int val)=>{
        if(val!=0) OnTrigger(new ChannelInfo(channel));
      },true));
    }
    if(delay>=0) Add(upd = new UpdateHook());
  }

  Queue<Tuple<float, TriggerInfo>> delayed;
  float activeTime = 0;
  public void HandleTrigger(TriggerInfo sm){
    if(triggerParent == null) return;
    if(hideTrigger){
      modifierParent?.OnTrigger(sm);
      return;
    }
    if(sm is TouchInfo tinfo){
      if(triggerOnTouch != advtouch.Contains(tinfo.ty)) triggerParent.OnTrigger(tinfo.asUsable());
      else modifierParent?.OnTrigger(tinfo);
    } else {
      if(passTrigger)triggerParent.OnTrigger(sm);
    }
  }
  public void OnTrigger(TriggerInfo sm){
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
      collideV, collideH, jump, climbjump, walljump, wallbounce, super, grounded, climbing 
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
    public override string category => "touch/"+ty.ToString();
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
    orig(p,a,b);
    if(a && b)triggerFromArr(p.temp,new TouchInfo(p,TouchInfo.Type.jump));
  }
  static void Hook(On.Celeste.Player.orig_SuperJump orig, Player p){
    orig(p);
    triggerFromArr(p.temp, new TouchInfo(p,TouchInfo.Type.super));
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
  static HookManager hooks = new(()=>{
    On.Celeste.Player.Jump+=Hook;
    On.Celeste.Player.SuperJump+=Hook;
    On.Celeste.Player.WallJump+=Hook;
    On.Celeste.Player.SuperWallJump+=Hook;
    On.Celeste.Player.ClimbJump+=Hook;
    On.Celeste.Player.OnCollideH+=Hook;
    On.Celeste.Player.OnCollideV+=Hook;
    On.Celeste.Player.Update+=Hook;
  },void ()=>{
    On.Celeste.Player.Jump-=Hook;
    On.Celeste.Player.SuperJump-=Hook;
    On.Celeste.Player.WallJump-=Hook;
    On.Celeste.Player.SuperWallJump-=Hook;
    On.Celeste.Player.ClimbJump-=Hook;
    On.Celeste.Player.OnCollideH-=Hook;
    On.Celeste.Player.OnCollideV-=Hook;
    On.Celeste.Player.Update-=Hook;
  }, auspicioushelperModule.OnEnterMap);
}
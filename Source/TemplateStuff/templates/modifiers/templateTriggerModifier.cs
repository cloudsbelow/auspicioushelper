


using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateTriggerModifier")]
public class TemplateTriggerModifier:Template, ITemplateTriggerable{
  bool triggerOnTouch;
  HashSet<TouchInfo.Type> advtouch = new();
  bool passTrigger;
  bool hideTrigger;
  bool blockTrigger;
  string channel;
  public TemplateTriggerModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateTriggerModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    foreach(string s in Util.listparseflat(d.Attr("advancedTouchOptions",""),true)){
      if(Enum.TryParse<TouchInfo.Type>(s,out var res))advtouch.Add(res);
    }
    triggerOnTouch = d.Bool("triggerOnTouch",false);
    if(triggerOnTouch || advtouch.Count>0) hooks.enable();
    channel = d.Attr("channel",null);
    if(!d.Bool("propegateRiding",true)) prop &= ~Propagation.Riding;
    if(!d.Bool("propegateInside",true)) prop &= ~Propagation.Inside;
    if(!d.Bool("propegateShake",true)) prop &= ~Propagation.Shake;
    if(!d.Bool("propegateDashHit",true)) prop &= ~Propagation.DashHit;
    passTrigger = d.Bool("propegateTrigger",false);
    hideTrigger = d.Bool("hideTrigger",false);
    blockTrigger = d.Bool("blockTrigger",false);
  }
  ITemplateTriggerable triggerParent;
  TemplateTriggerModifier modifierParent;
  public override void addTo(Scene scene) {
    base.addTo(scene);
    triggerParent = parent?.GetFromTree<ITemplateTriggerable>();
    modifierParent = parent?.GetFromTree<TemplateTriggerModifier>();
    if(!string.IsNullOrWhiteSpace(channel)){
      Add(new ChannelTracker(channel, (int val)=>{
        if(val!=0) OnTrigger(new ChannelInfo(channel));
      },true));
    }
  }

  public void OnTrigger(TriggerInfo sm){
    if(triggerParent == null || blockTrigger) return;
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
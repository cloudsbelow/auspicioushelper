


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper.Wrappers;
public class MoveBlockW:Component, ITemplateChild{
  public Template parent {get;set;}
  MoveBlock m;
  Vector2 posdelta;
  List<MoveBlock.Debris> currentDebris;
  Template.Propagation prop=Template.Propagation.All;
  Template.Propagation ITemplateChild.prop => prop;
  public MoveBlockW(MoveBlock m):base(true, false){
    OverrideVisualComponent.Get(m).AddToOverride(new(IOverrideVisuals.blocker, -29999, false, true));
    posdelta = m.Position;
    this.m=m;
    ResetEvents.Hooks<MoveBlockW>.enable();
  }
  public override void Update() {
    base.Update();
    var comp = OverrideVisualComponent.TryGet(m);
    if(m.border != null) OverrideVisualComponent.EnsureParity(comp,m.border);
    if(currentDebris!=null) foreach(var d in currentDebris) OverrideVisualComponent.EnsureParity(comp,d);

    var ib = m.state==MoveBlock.MovementState.Breaking && currentDebris!=null && m.Collidable;
    if(m.state==MoveBlock.MovementState.Idling || ib) prop=Template.Propagation.All;
    else prop = Template.Propagation.All&~Template.Propagation.Shake;
  }
  bool ITemplateChild.hasPlayerRider()=> UpdateHook.cachedPlayer?.IsRiding(m)??false;
  void ITemplateChild.parentChangeStat(int vis, int col, int act) {
    if(act!=0) m.Active=act>0;
    if(col!=0){
      BlockingCollider.SetCol(m, col>0);
      if(col>0) m.EnableSms(vis);
      else m.DisableSms(vis);
    }
    if(vis!=0){
      OverrideVisualComponent.Get(m).SetStealUse(IOverrideVisuals.blocker, vis<0, true);
      if(m.border != null) OverrideVisualComponent.EnsureParity(m,m.border);
    }
  }
  bool ITemplateChild.hasInside(Actor a)=> m.Collidable && BlockingCollider.WihtoutBlock(m).Collide(a.Collider);
  void ITemplateChild.setOffset(Vector2 ppos)=> posdelta-=ppos;
  void ITemplateChild.AddAllChildren(List<Entity> list){
    list.Add(m);
    if(m.border!=null) list.Add(m.border);
  }
  void ITemplateChild.relposTo(Vector2 loc, Vector2 parentLiftspeed) {
    m.startPosition = loc+posdelta;
    if(m.state == MoveBlock.MovementState.Idling) m.MoveTo(m.startPosition, parentLiftspeed);
    else if(m.state == MoveBlock.MovementState.Breaking && (!m.Visible||currentDebris!=null)){
      var delta = m.startPosition-m.Position;
      m.MoveStaticMovers(delta);
      m.Position=m.startPosition;
      if(currentDebris!=null) foreach(var d in currentDebris){
        d.home+=delta;
        d.returnCurve.End=d.home;
      }
    }
  }
  void ITemplateChild.destroy(bool particles) {
    m.RemoveSelf();
    m.border?.RemoveSelf();
    if(currentDebris!=null) foreach(var d in currentDebris) d.RemoveSelf();
  }
  void ITemplateChild.addTo(Scene s)=>s.Add(m);

  static void breakDelegate(MoveBlock m, List<MoveBlock.Debris> debris){
    if(m.Get<MoveBlockW>() is {} comp){
      comp.currentDebris=debris;
      var c = OverrideVisualComponent.TryGet(m);
      if(c!=null) foreach(var d in debris) OverrideVisualComponent.EnsureParity(c,d);
    }
  }
  static void reformDelegate(MoveBlock m){
    if(m.Get<MoveBlockW>() is {} comp) comp.currentDebris=null;
  }
  [ResetEvents.ILHook(typeof(MoveBlock),nameof(MoveBlock.Controller),Util.HookTarget.Coroutine)]
  static void HookController(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<Platform>(nameof(Platform.DisableStaticMovers))
    )){
      // c.EmitLdarg0();
      // c.EmitLdfld()
      MethodInfo m = typeof(MoveBlock).GetMethod(nameof(MoveBlock.Controller),Util.GoodBindingFlags).GetStateMachineTarget();
      Type sm = m.DeclaringType;
      FieldInfo r = sm.GetField("<debris>5__5",Util.GoodBindingFlags);
      c.EmitLdloc1();
      c.EmitLdarg0();
      c.EmitLdfld(r);
      c.EmitDelegate(breakDelegate);
    }
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchCallvirt<Platform>(nameof(Platform.EnableStaticMovers))
    )){
      c.EmitLdloc1();
      c.EmitDelegate(reformDelegate);
    }
  }
}




using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
[CustomEntity("auspicioushelper/TemplateBlock")]
class TemplateBlock:TemplateDisappearer, ITemplateTriggerable{
  public TemplateBlock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  bool uvis, ucol, uact;
  bool candash, persistent, isExitBlock;
  string breaksfx;
  bool triggerable, triggerOnBreak, triggerTouching;
  public bool breakableByBlocks = false;
  public TemplateBlock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    uvis = d.Bool("visible",true);
    ucol = d.Bool("collidable",true);
    uact = d.Bool("active",true);
    candash = !d.Bool("only_redbubble_or_summit_launch",false);
    persistent = d.Bool("persistent",false);
    breaksfx = d.Attr("breaksfx","event:/game/general/wall_break_stone");
    if(d.Bool("canbreak",true)){
      OnDashCollide = (Player p, Vector2 dir)=>{
        if (!candash && p.StateMachine.State != 5 && p.StateMachine.State != 10){
          return ((ITemplateChild) this).propagateDashhit(p,dir);
        }
        using(new DebrisSource(p.Position, dir*40, high:75))breakBlock();
        return DashCollisionResults.Rebound;
      };
      prop &= ~Propagation.DashHit;
    }
    if(!d.Bool("propagateRiding",true))prop&=~Propagation.Riding;
    if(!d.Bool("propagateShaking",true))prop&=~Propagation.Shake;
    triggerable=d.Bool("triggerable",false);
    triggerOnBreak=d.Bool("triggerOnBreak",false);
    isExitBlock = d.Bool("exitBlockBehavior",false);
    if(breakableByBlocks = d.Bool("breakableByBlocks",false)) ResetEvents.Hooks<TemplateBlock>.enable();
    triggerTouching = d.Bool("triggerTouching",false);
  }
  bool broken=false;
  class CrumbleHit(Template t):TriggerInfo{
    public override string category => "crumbleHit/"+t.fullpath;
  }
  public void breakBlock(){
    if(broken) return;
    broken=true;
    if(triggerTouching){
      var q = TemplateMoveCollidable.getq(this, Vector2.Zero, false, false, false);
      foreach(var c in q.q.colliders) if(q.s.Collide(c.c)){
        if(c.c.Entity?.Get<ChildMarker>() is not {} cm) continue;
        if(cm.propagatesTo(this)) continue;
        cm.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new CrumbleHit(this));
      }
    }
    if(triggerOnBreak) new TriggerInfo.EntInfo("TemplateBlock",this).Pass(this); 
    Audio.Play(breaksfx,Position);
    if(persistent) auspicioushelperModule.Session.brokenTempaltes.Add(fullpath);
    destroy(true);
  }
  public void OnTrigger(TriggerInfo info){
    if(!triggerable){
      info.Pass(this);
      return;
    }
    if(TriggerInfo.TestPass(info,this)) breakBlock();
  }
  public override void addTo(Scene scene){
    if(persistent && auspicioushelperModule.Session.brokenTempaltes.Contains(fullpath)){
      RemoveSelf();
    } else {
      base.addTo(scene);
      if(isExitBlock) setVisColAct(false,false,false);
      else setVisColAct(uvis,ucol,uact);
      Active = true;
    }
  }

  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(isExitBlock){
      Player p = Scene.Tracker.GetEntity<Player>();
      if(p!=null && !hasInside(p))setVisColAct(uvis,ucol,uact);
      else Add(new Coroutine(appearSequence()));
    }
    //MiptileCollider mtc = MiptileCollider.fromGrid(fgt.Grid);
    //DebugConsole.Write("res", mtc.CollideMipTileOffset(MiptileCollider.fromGrid((scene as Level).SolidTiles.Grid),Vector2.Zero));
  }

  IEnumerator appearSequence(){
    while(true){
      Player p = Scene.Tracker.GetEntity<Player>();
      if(p!=null && !hasInside(p)) break;
      yield return null;
    }
    Audio.Play("event:/game/general/passage_closed_behind", Position);
    List<Entity> c = new();
    AddAllChildren(c);
    setVisColAct(uvis,ucol,uact);
    if(!uvis) yield break;
    FadeMaterialLayer f = new FadeMaterialLayer(8000);
    MaterialPipe.addLayer(f);
    foreach(var e in GetChildren<Entity>()){
      OverrideVisualComponent.Get(e).AddToOverride(new(f,-20000,true,true));
    }
    f._alpha=0;
    yield return null;
    while((f._alpha = f._alpha+Engine.DeltaTime)<1){
      yield return null;
    }
    foreach(var e in GetChildren<Entity>()){
      OverrideVisualComponent.Get(e).RemoveFromOverride(f);
    }
    MaterialPipe.removeLayer(f);
  }

  static Solid refineSolid(Solid orig, Platform self, Vector2 loc){
    if(orig==null) return null;
    var cur = orig;
    var idx=0;
    var ls = self.Scene.Tracker.GetEntities<Solid>();
    using var restore = Util.WithRestore(ref self.Position, loc);
    check:
      if(cur.Get<ChildMarker>() is not {} cm) return cur;
      Template block = cm.parent;
      while(block!=null && (block is not TemplateBlock blk || !blk.breakableByBlocks)) block=block.parent;
      if(block==null) return cur;
      (block as TemplateBlock).breakBlock();
    
      for(; idx<ls.Count; idx++) if(Collide.Check(self, ls[idx])){
        cur = ls[idx++] as Solid;
        goto check;
      }
      return null;
  }
  [ResetEvents.ILHook(typeof(Platform),nameof(Platform.MoveHExactCollideSolids))]
  [ResetEvents.ILHook(typeof(Platform),nameof(Platform.MoveVExactCollideSolids))]
  static void SolidMoverHook(ILContext ctx){
    ILCursor c = new(ctx);
    VariableDefinition v = new(c.Context.Import(typeof(Vector2)));
    c.Body.Variables.Add(v);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=> itr.MatchCall<Entity>(nameof(CollideFirst)) && 
        itr.Operand is GenericInstanceMethod g && 
        g.GenericArguments.Count==1 && 
        g.GenericArguments[0].FullName==typeof(Solid).FullName
    )){
      c.EmitDup();
      c.EmitStloc(v);
      c.Index++;
      c.EmitLdarg0();
      c.EmitLdloc(v);
      c.EmitDelegate(refineSolid);
    } else DebugConsole.Write("Could not add moveCollideSolids hook");
  }
}
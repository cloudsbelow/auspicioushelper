



using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
[CustomEntity("auspicioushelper/TemplateBlock")]
class TemplateBlock:TemplateDisappearer, ITemplateTriggerable{
  public TemplateBlock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  bool uvis;
  bool ucol;
  bool uact;
  bool candash;
  bool persistent;
  string breaksfx;
  bool isExitBlock;
  bool triggerable;
  bool triggerOnBreak;
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
          return DashCollisionResults.NormalCollision;
        }
        breakBlock();
        return DashCollisionResults.Rebound;
      };
      prop &= ~Propagation.DashHit;
    }
    if(!d.Bool("propagateRiding",true))prop&=~Propagation.Riding;
    if(!d.Bool("propagateShaking",true))prop&=~Propagation.Shake;
    triggerable=d.Bool("triggerable",false);
    triggerOnBreak=d.Bool("triggerOnBreak",false);
    isExitBlock = d.Bool("exitBlockBehavior",false);
    breakableByBlocks = d.Bool("breakableByBlocks",false);
  }
  bool broken=false;
  public void breakBlock(){
    if(broken) return;
    setCollidability(false);
    Audio.Play(breaksfx,Position);
    destroy(true);
    if(persistent) auspicioushelperModule.Session.brokenTempaltes.Add(fullpath);
    broken=true;
    if(triggerOnBreak) new TriggerInfo.EntInfo("TemplateBlock",this).Pass(this); 
  }
  public void OnTrigger(TriggerInfo info){
    if(!triggerable){
      info.Pass(this);
      return;
    }
    if(!info.TestPass(this)) return;
    breakBlock();
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
    setVisColAct(false,ucol,uact);
    if(!uvis) yield break;
    FadeMaterialLayer f = new FadeMaterialLayer(c,8000);
    f._alpha=0;
    MaterialPipe.addLayer(f);
    MaterialPipe.indicateImmidiateAddition();
    yield return null;
    while((f._alpha = f._alpha+Engine.DeltaTime)<1){
      yield return null;
    }
    MaterialPipe.removeLayer(f);
    setVisibility(true);
  }
}
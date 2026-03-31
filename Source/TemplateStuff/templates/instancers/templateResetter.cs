

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/TemplateResetter")]
[Tracked]
public sealed class TemplateResetter:TemplateDisappearer,ITemplateTriggerable{
  string resetCh;
  string destroyCh;
  bool particles=true;
  bool keepOld=false;
  bool triggerable=false;
  bool startWith=true;
  bool blockable=false;
  public TemplateResetter(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public TemplateResetter(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    resetCh = d.Attr("resetChannel","");
    destroyCh = d.Attr("destroyChannel","");
    particles = d.Bool("particles",true);
    keepOld = d.Bool("resetKeepsOld",false);
    triggerable = d.Bool("resetOnTrigger",false);
    startWith = d.Bool("startWith",true);
    blockable = d.Bool("blockedByPlayer",false);
  }
  void ITemplateTriggerable.OnTrigger(TriggerInfo s) {
    if(!triggerable){
      s.Pass(this);
      return;
    }
    if(!s.TestPass(this)) return;
    Reset();
  }
  enum SetTo{
    None, Reset, Destroy
  }
  SetTo state;
  bool reforming = false;
  void Reset(){
    state=SetTo.Reset;
  }
  void Destroy(){
    state=SetTo.Destroy;
  }
  public override void addTo(Scene scene) {
    if(startWith) base.addTo(scene);
    else reducedAdd(scene);
    if(resetCh.HasContent()) Add(new ChannelTracker(resetCh, x=>{if(x!=0)Reset();}));
    if(destroyCh.HasContent()) Add(new ChannelTracker(destroyCh, x=>{if(x!=0)Destroy();}));
  }
  public override void Update() {
    base.Update();
    if(state!=SetTo.None){
      if(isExpanded) destroyChildren(particles && getSelfVis());
      if(state == SetTo.Reset)remake(()=>{
        if(PlayerIsInside() && blockable){
          reforming = true;
          setVisCol(false,false);
        }
      });
      state=SetTo.None;
    } else if(reforming && !PlayerIsInside()){
      reforming=false;
      setVisCol(true,true);
    }
  }
}
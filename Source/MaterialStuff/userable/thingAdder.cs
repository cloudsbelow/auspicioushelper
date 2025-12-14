


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/MaterialApplier")]
[MapenterEv(nameof(Search))]
public class MaterialAdder:Entity{

  static void Search(EntityData d){
    if(d.Bool("can_be_ID_path",true)){
      string str = d.Attr("identifier");
      Finder.watch(str,(e)=>FoundEntity.addIdent(e,str));
    }
  }
  string ident;
  OverrideVisualComponent comp;
  ChannelTracker ct=null;
  EntityData dat;
  IOverrideVisuals l;

  public MaterialAdder(EntityData d, Vector2 o):base(d.Position+o){
    dat = d;
    ident = d.Attr("identifier");
  }
  void Begin(Entity e){
    l = MaterialController.getLayer(dat.Attr("materialLayer")) as IOverrideVisuals;
    if(l==null){
      DebugConsole.WriteFailure($"Looking for non-added layer \"{dat.Attr("materialLayer")}\" in material adder!");
      return;
    }
    comp = OverrideVisualComponent.Get(e);
    bool active = ct==null?true:ct.value!=0;
    comp.AddToOverride(new(l,(short)dat.Int("priority",-1),dat.Bool("dontNormalRender",true) && active, active));
    Active = false;
  }
  public override void Awake(Scene scene) {
    if(dat.tryGetStr("toggleChannel", out var ch)) ct = new(ch,(nval)=>{
      if(comp==null) return;
      if(nval!=0) comp.SetStealUse(l, dat.Bool("dontNormalRender",true), true);
      else comp.SetStealUse(l,false,false);
    });
    if(FoundEntity.findEnt(ident) is {} e)Begin(e);
  }
  public override void Update() {
    if(comp!=null){
      Active = false; return;
    }
    if(FoundEntity.findEnt(ident) is {} e)Begin(e);
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    comp?.SetStealUse(l,false,false);
  }
}



using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/MaterialApplier")]
[MapenterEv(nameof(Search))]
public class MaterialAdder:Entity{

  static void Search(EntityData d){
    if(d.Bool("can_be_ID_path",true)) foreach(var i in Util.listparseflat(d.Attr("identifier"))){
      Finder.watch(i,(e)=>FoundEntity.addIdent(e,i));
    }
  }
  List<OverrideVisualComponent> comps  =new();
  ChannelTracker ct=null;
  EntityData dat;
  IOverrideVisuals l;
  HashSet<string> needs;
  bool dnr;

  public MaterialAdder(EntityData d, Vector2 o):base(d.Position+o){
    dat = d;
    needs = new(Util.listparseflat(d.Attr("identifier")));
    dnr = dat.Bool("dontNormalRender",true);
  }
  void Begin(Entity e, string from){
    needs.Remove(from);
    l = MaterialController.getLayer(dat.Attr("materialLayer")) as IOverrideVisuals;
    if(l==null){
      DebugConsole.WriteFailure($"Looking for non-added layer \"{dat.Attr("materialLayer")}\" in material adder!");
      return;
    }
    var comp = OverrideVisualComponent.Get(e);
    comps.Add(comp);
    bool active = ct==null?true:ct.value!=0;
    comp.AddToOverride(new(l,(short)dat.Int("priority",-1),dnr && active, active));
    if(needs.Count == 0) Active=false;
  }
  public override void Awake(Scene scene) {
    if(dat.tryGetStr("toggleChannel", out var ch)) ct = new(ch,(nval)=>{
      foreach(var comp in comps){
        if(nval!=0) comp.SetStealUse(l, dnr, true);
        else comp.SetStealUse(l,false,false);
      }
    });
    foreach(var s in needs) if(FoundEntity.findEnt(s) is {} e) Begin(e,s);
  }
  public override void Update() {
    foreach(var s in needs) if(FoundEntity.findEnt(s) is {} e) Begin(e,s);
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    foreach(var comp in comps) comp.RemoveFromOverride(l);
  }
}
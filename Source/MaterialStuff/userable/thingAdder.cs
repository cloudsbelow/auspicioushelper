


using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/MaterialApplier")]
[MapenterEv(nameof(Search))]
public class MaterialAdder:Entity, IOverrideVisualsEasy{
  static void Search(EntityData d){
    if(d.Bool("can_be_ID_path",true)) foreach(var i in Util.listparseflat(d.Attr("identifier"))){
      Finder.enqueueIdent(i.Split(':')[0].TrimEnd());
    }
  }
  public HashSet<OverrideVisualComponent> comps{get;} = new();
  ChannelTracker ct=null;
  EntityData dat;
  IOverrideVisuals l;
  List<(string,string)> needs;
  List<(string,string)> types;
  bool dnr;

  public MaterialAdder(EntityData d, Vector2 o):base(d.Position+o){
    dat = d;
    needs = Util.listparseflat(d.Attr("identifier")).Map(str=>{
      var split = str.Split(':');
      return (split[0].TrimEnd(), split.Length>1? split[1].Trim() : null);
    });
    types = Util.listparseflat(d.Attr("types")).Map(str=>{
      var split = str.Split(':');
      return (split[0].TrimEnd(), split.Length>1? split[1].Trim() : null);
    });
    dnr = dat.Bool("dontNormalRender",true);
    Visible = false;
  }
  void AddEntity(Entity e)=>AddEntity(e,null);
  void AddEntity(Entity e, string options){
    l = MaterialController.getLayer(dat.Attr("materialLayer")) as IOverrideVisuals;
    if(l==null){
      DebugConsole.WriteFailure($"Looking for non-added layer \"{dat.Attr("materialLayer")}\" in material adder!");
    } else {
      var comp = OverrideVisualComponent.Get(e);
      if(options != null) comp=comp.withOptions(options);
      bool active = ct==null?true:ct.value!=0;
      comp.AddToOverride(new(l, (short)dat.Int("priority",-1), dnr && active, active));
      comp.AddToOverride(new(this, -30000, false, true));
    }
  }
  void Try(){
    needs.RemoveAll(s=>{
      if(FoundEntity.findEnt(s.Item1) is not {} e) return false;
      AddEntity(e, s.Item2);
      return true;
    });
    if(needs.Count==0) Active=false;
  }
  public override void Awake(Scene scene) {
    if(dat.tryGetStr("toggleChannel", out var ch)) ct = new(ch,(nval)=>{
      foreach(var comp in comps){
        if(nval!=0) comp.SetStealUse(l, dnr, true);
        else comp.SetStealUse(l,false,false);
      }
    });
    Try();
    if(types.Count>0){
      var thing = new Finder.TypeHandlerCallback(AddEntity);
      Add(thing);
      foreach(var (s1,s2) in types){
        if(string.IsNullOrEmpty(s2)) thing.RegisterTo(s1);
        else Add(new Finder.TypeHandlerCallback(e=>AddEntity(e,s2)).RegisterTo(s1, scene));
      }
      thing.Retroactive(scene as Level);
    }
  }
  public override void Update() {
    Try();
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    foreach(var comp in comps.ToList()){
      comp.RemoveFromOverride(l);
      comp.RemoveFromOverride(this);
    }
  }
}
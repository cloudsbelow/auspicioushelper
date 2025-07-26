



using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked(true)]
public class OverrideVisualComponent:Component, IMaterialObject{
  public bool ovis;
  public bool nvis;
  public bool overriden;
  public IOverrideVisuals parent;
  public OverrideVisualComponent(IOverrideVisuals parent):base(false,false){
    this.parent=parent;
  }
  public override void Added(Entity entity) {
    base.Added(entity);
    parent.AddC(this);
  }
  public override void EntityRemoved(Scene scene) {
    base.EntityRemoved(scene);
    parent.RemoveC(this);
  }
  public override void Removed(Entity entity) {
    base.Removed(entity);
    parent.RemoveC(this);
  }
  public static void Override(Scene s){
    foreach(OverrideVisualComponent v in s.Tracker.GetComponents<OverrideVisualComponent>()){
      if(!v.overriden) v.ovis = v.Entity.Visible;
      v.Entity.Visible = v.nvis && v.ovis;
      v.overriden = true;
    }
  }
  public static void Restore(Scene s){
    foreach(OverrideVisualComponent v in s.Tracker.GetComponents<OverrideVisualComponent>()){
      if(v.overriden) v.Entity.Visible = v.ovis;
      v.overriden = false;
    }
  }
  public float _depth=>(float)Entity.actualDepth;
  public bool shouldRemove=>Entity.Scene==null;
  public void renderMaterial(IMaterialLayer l, Camera c){
    if(ovis && Entity.Scene!=null)Entity.Render();
  }
}
public interface IOverrideVisuals{
  List<OverrideVisualComponent> comps {get;set;}
  HashSet<OverrideVisualComponent> toRemove {get;} 
  bool dirty {set;}
  void AddC(OverrideVisualComponent comp) {comps.Add(comp); dirty=true;}
  void RemoveC(OverrideVisualComponent comp) {toRemove.Add(comp); dirty=true; if(toRemove.Count>64)FixList();}
  void FixList(){
    List<OverrideVisualComponent> nlist = new();
    foreach(OverrideVisualComponent v in comps){
      if(v.Entity.Scene != null && !toRemove.Contains(v)) nlist.Add(v);
    }
    toRemove.Clear();
    comps = nlist;
  }
  void PrepareList(bool newvisibility){
    if(toRemove.Count>0)FixList();
    double ldepth=double.PositiveInfinity;
    bool nsort=false;
    foreach(var v in comps){
      if(!v.overriden) v.ovis = v.Entity.Visible;
      v.nvis = newvisibility;
      if(v.Entity.actualDepth>ldepth) nsort=true;
    }
    if(nsort) comps.Sort((a,b)=>b.Entity.actualDepth.CompareTo(a.Entity.actualDepth));
    dirty = false;
  }
  void OverrideRender(){
    foreach(var comp in comps) if(comp.ovis) comp.Entity.Render();
  }
}
[CustomEntity("auspicioushelper/MaterialTemplate")]
public class MaterialTemplate:TemplateDisappearer, IOverrideVisuals{
  public List<OverrideVisualComponent> comps  {get;set;}= new();
  public HashSet<OverrideVisualComponent> toRemove {get;} = new();
  public bool dirty {get;set;}
  bool invis;
  bool collidable = true;
  public MaterialTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public MaterialTemplate(EntityData d, Vector2 offset, int depthoffset):base(d,offset+d.Position,depthoffset){
    invis = d.Bool("dontNormalRender",true);
    lident = d.Attr("identifier","");
    if(string.IsNullOrWhiteSpace(lident)) DebugConsole.Write("No layer specified for material template");
    collidable = d.Bool("collidable",true);
  }
  string lident;
  IMaterialLayer layer;
  public override void addTo(Scene scene) {
    base.addTo(scene);
    List<Entity> l = new();
    AddAllChildren(l);
    layer = MaterialController.getLayer(lident);
    if(layer == null){
      DebugConsole.Write($"Layer {lident} not found");
    }
    foreach(var e in l) if(!(e is Template)){
      var c = new OverrideVisualComponent(this);
      e.Add(c);
      layer.addEnt(c);
    }
    (this as IOverrideVisuals).PrepareList(!invis);
    if(!collidable)setCollidability(false);
  }
}
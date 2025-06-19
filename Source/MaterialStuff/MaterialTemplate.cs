



using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class OverrideVisualComponent:Component, IMaterialObject{
  public bool ovis;
  public bool nvis;
  public bool overriden;
  IOverrideVisuals parent;
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
      v.Entity.Visible = v.nvis;
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
}
public interface IOverrideVisuals{
  List<OverrideVisualComponent> comps {get;set;}
  HashSet<OverrideVisualComponent> toRemove {get;} 
  bool dirty {set;}
  void AddC(OverrideVisualComponent comp) {comps.Add(comp); dirty=true;}
  void RemoveC(OverrideVisualComponent comp) {toRemove.Add(comp); dirty=true;}
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
public class MaterialTemplate:TemplateDisappearer, IOverrideVisuals, IMaterialEnt{
  public List<OverrideVisualComponent> comps {get;set;}
  public HashSet<OverrideVisualComponent> toRemove {get;}
  public bool dirty {get;set;}
  public MaterialTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public MaterialTemplate(EntityData d, Vector2 offset, int depthoffset):base(d,offset,depthoffset){}
  string lident;
  IMaterialLayer l;
  public override void addTo(Scene scene) {
    base.addTo(scene);
    List<Entity> l = new();
    AddAllChildren(l);
    foreach(var e in l) if(!(e is Template)) e.Add(new OverrideVisualComponent(this));
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    l = MaterialController.getLayer(lident);
    if(l!=null) l.
  }
  public void renderMaterial(IMaterialLayer l, SpriteBatch sb, Camera c){
    if(dirty) (this as IOverrideVisuals).FixList();
    (this as IOverrideVisuals).OverrideRender();
  }
}
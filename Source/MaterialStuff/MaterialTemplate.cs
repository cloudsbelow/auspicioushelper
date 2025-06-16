



using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class OverrideVisualComponent:Component{
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
    double ldepth=double.PositiveInfinity;
    bool nsort=false;
    foreach(var v in comps){
      if(!v.overriden) v.ovis = v.Entity.Visible;
      v.nvis = newvisibility;
      if(v.Entity.actualDepth>ldepth) nsort=true;
    }
    if(nsort) comps.Sort((a,b)=>b.Entity.actualDepth.CompareTo(a.Entity.actualDepth));
  }
}
public class MaterialTemplate:TemplateDisappearer, IOverrideVisuals{
  public List<OverrideVisualComponent> comps {get;set;}
  public HashSet<OverrideVisualComponent> toRemove {get;}
  public bool dirty {get;set;}
  public MaterialTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public MaterialTemplate(EntityData d, Vector2 offset, int depthoffset):base(d,offset,depthoffset){}
  public override void addTo(Scene scene) {
    base.addTo(scene);
    List<Entity> l = new();
    AddAllChildren(l);
    foreach(var e in l) if(!(e is Template)) e.Add(new OverrideVisualComponent(this));
  }
}
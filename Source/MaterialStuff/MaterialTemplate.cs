



using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using FMOD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked(true)]
public class OverrideVisualComponent:Component, IMaterialObject, IFreeableComp{
  public struct VisualOverrideDescr{
    public IOverrideVisuals o;
    public short order;
    public bool steal=true;
    public bool use=true;
    public VisualOverrideDescr(IOverrideVisuals overrider, short prio, bool steal=true, bool use=true){
      o=overrider; order=prio; this.steal=steal; this.use=use;
    }
    public VisualOverrideDescr(IOverrideVisuals overrider, int prio, bool steal=true, bool use=true){
      o=overrider; order=(short)Math.Clamp(prio,short.MinValue,short.MaxValue); this.steal=steal; this.use=use;
    }
  }
  public bool ovis;
  public bool nvis=true;
  public bool overriden;
  //tracker woes (kill me)
  public Entity ent;
  public List<VisualOverrideDescr> parents = new();
  public static OverrideVisualComponent Get(Entity e){
    if(e.Get<OverrideVisualComponent>() is {} o) return o;
    OverrideVisualComponent comp;
    if(custom.TryGetValue(e.GetType(),out var fn)){
      comp = fn(e);
    }else comp = new OverrideVisualComponent();
    e.Add(comp);
    comp.ent = e;
    return comp;
  }
  public OverrideVisualComponent():base(false,false){}
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public int GetOverriderIdx(IOverrideVisuals v){
    for(int i=0; i<parents.Count; i++) if(v==parents[i].o) return i;
    return -1;
  }
  public int GetOverriderIdx(IOverrideVisuals v, out bool stolen){
    stolen = false;
    for(int i=0; i<parents.Count; i++){
      if(v==parents[i].o) return i;
      else stolen|=parents[i].steal;
    } 
    return -1;
  }
  public virtual void setNvis(bool newvis){
    nvis = newvis;
  }
  public void AddToOverride(VisualOverrideDescr v){
    if(GetOverriderIdx(v.o)!=-1){
      SetStealUse(v.o,v.steal,v.use);
      return;
    }
    int idx = 0;
    bool stolen = false;
    while(idx<parents.Count && parents[idx].order<v.order){
      stolen |= parents[idx].steal;
      idx++;
    }
    parents.Insert(idx, v);
    if(stolen) return;
    if(v.use)v.o.AddC(this);
    if(!v.steal)return;
    while(++idx<parents.Count){
      if(v.use)parents[idx].o.RemoveC(this);
      if(parents[idx].steal)break;
    }
    setNvis(false);
  }
  public void RemoveFromOverride(IOverrideVisuals v){
    int idx = GetOverriderIdx(v);
    if(idx==-1)return;
    SetStealUse(v,false,false);
    parents.RemoveAt(idx);
  }
  public void SetStealUse(IOverrideVisuals v, bool nsteal, bool nuse){
    int idx = GetOverriderIdx(v, out var stolen);
    if(idx==-1) throw new Exception("StealUse set not in thing");
    var desc = parents[idx];
    bool origuse = desc.use;
    bool origsteal = desc.steal;
    desc.use=nuse;
    desc.steal=nsteal;
    parents[idx]=desc;
    if(stolen) return;
    //This overrider is 'active'
    if(origuse!=nuse){
      if(nuse)desc.o.AddC(this);
      else desc.o.RemoveC(this);
    } 
    if(origsteal==nsteal) return;
    //Overriders after this have their status change
    while(++idx<parents.Count){
      VisualOverrideDescr descr = parents[idx];
      if(descr.use){
        if(nsteal)descr.o.RemoveC(this);
        else descr.o.AddC(this);
      }
      if(descr.steal)return;
    }
    setNvis(!nsteal);
  }
  public override void EntityRemoved(Scene scene) {
    base.EntityRemoved(scene);
    foreach(var p in parents)p.o.RemoveC(this);
  }
  public override void SceneEnd(Scene scene) {
    base.SceneEnd(scene);
    foreach(var p in parents)p.o.RemoveC(this);
  }
  public override void Removed(Entity entity) {
    base.Removed(entity);
    foreach(var p in parents)p.o.RemoveC(this);
  }
  public bool shouldRemove=>false;
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
  public virtual void renderMaterial(IMaterialLayer l, Camera c){
    if(ovis && Entity.Scene!=null)Entity.Render();
  }
  public static Dictionary<Type,Func<Entity,OverrideVisualComponent>> custom = new();
  public class PatchedRenderComp:OverrideVisualComponent{
    public Action render;
    public Action<bool> onChangeNvis;
    public Func<bool> isVis;
    public bool doRender => isVis?.Invoke()??ovis;
    public override void renderMaterial(IMaterialLayer l, Camera c) {
      if(doRender && Entity.Scene!=null) render();
    }
    public override void setNvis(bool newvis) {
      if(nvis == newvis) return;
      base.setNvis(newvis);
      if(onChangeNvis!=null) onChangeNvis(newvis);
    }
  }
}

public interface IOverrideVisuals{
  void AddC(OverrideVisualComponent c);
  void RemoveC(OverrideVisualComponent c);
}
public interface IOverrideVisualsEasy:IOverrideVisuals{
  HashSet<OverrideVisualComponent> comps {get;}
  void IOverrideVisuals.AddC(OverrideVisualComponent c)=>comps.Add(c);
  void IOverrideVisuals.RemoveC(OverrideVisualComponent c)=>comps.Remove(c);
}

[CustomEntity("auspicioushelper/MaterialTemplate")]
public class MaterialTemplate:TemplateDisappearer, IOverrideVisualsEasy{
  public HashSet<OverrideVisualComponent> comps {get;} = new();
  bool invis;
  bool collidable = true;
  string channel;
  bool active=true;
  public MaterialTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public MaterialTemplate(EntityData d, Vector2 offset, int depthoffset):base(d,offset+d.Position,depthoffset){
    invis = d.Bool("dontNormalRender",true);
    lident = d.Attr("identifier","");
    if(string.IsNullOrWhiteSpace(lident)) DebugConsole.Write("No layer specified for material template");
    collidable = d.Bool("collidable",true);
    channel = d.Attr("channel","");
  }
  string lident;
  IOverrideVisuals layer;
  public override void addTo(Scene scene) {
    base.addTo(scene);
    List<Entity> l = new();
    AddAllChildren(l);
    var mlayer = MaterialController.getLayer(lident);
    if(mlayer is IOverrideVisuals qlay){
      layer = qlay;
      int tdepth = -TemplateDepth(); 
      foreach(var e in l) if(!(e is Template)){
        OverrideVisualComponent.Get(e).AddToOverride(new(layer,tdepth,invis));
      }
    } else {
      DebugConsole.Write($"Layer {lident} not found");
    }
    if(!collidable)setCollidability(false);
  }
  public override void OnNewEnts(List<Entity> l) {
    int tdepth = -TemplateDepth(); 
    if(layer!=null)foreach(var e in l)OverrideVisualComponent.Get(e).AddToOverride(new(layer,tdepth,invis));
    base.OnNewEnts(l);
  }
}
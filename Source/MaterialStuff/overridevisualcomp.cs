using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked(true)]
public class OverrideVisualComponent:OnAnyRemoveComp, IFreeableComp{
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
  public List<VisualOverrideDescr> parents = new();
  public static OverrideVisualComponent Get(Entity e){
    if(e.Get<OverrideVisualComponent>() is {} o) return o;
    OverrideVisualComponent comp;
    if(custom.TryGetValue(e.GetType(),out var fn)){
      comp = fn(e);
    } else if(e is ICustomMatRender){
      comp = new ICustomMatRender.CustomOverrider();
    } else comp = new OverrideVisualComponent();
    e.Add(comp);
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
    DebugConsole.Write("Set stealuse", Entity, v.ToString().RemovePrefix("Celeste.Mod.auspicioushelper."), nsteal, nuse);
    int idx = GetOverriderIdx(v, out var stolen);
    if(idx==-1) throw new Exception("StealUse set not in thing");
    var desc = parents[idx];
    bool origuse = desc.use;
    bool origsteal = desc.steal;
    desc.use=nuse;
    desc.steal=nsteal;
    parents[idx]=desc;
    if(stolen) {
      DebugConsole.Write("Return erarly stolen");
      return;
    }
    //This overrider is 'active'
    if(origuse!=nuse){
      if(nuse){
        DebugConsole.Write("Addc",v);
        desc.o.AddC(this);
      }
      else {
        DebugConsole.Write("Removec",v);
        desc.o.RemoveC(this);
      }
    } 
    if(origsteal==nsteal) {
      DebugConsole.Write("Return erarly equiv");
      return;
    }
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
  public override void OnRemove() {
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
public interface ICustomMatRender{
  void MatRender();
  public class CustomOverrider:OverrideVisualComponent{
    public override void renderMaterial(IMaterialLayer l, Camera c) {
      if(ovis && Entity.Scene!=null) ((ICustomMatRender)Entity).MatRender();
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
  public class Class:IOverrideVisualsEasy{
    public HashSet<OverrideVisualComponent> comps {get;}=new();
  }
}
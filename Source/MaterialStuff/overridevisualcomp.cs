using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked(true)]
public class OverrideVisualComponent:OnAnyRemoveComp{
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
    Type t = e.GetType();
    while(t!=null && t!=typeof(Entity)){
      if(custom.TryGetValue(t, out var fn)){
        comp = fn(e);
        goto done;
      }
      t=t.BaseType;
    }
    if(e is ICustomMatRender) comp = new ICustomMatRender.CustomOverrider();
    else comp = new OverrideVisualComponent();
    done:
      e.Add(comp);
      return comp;
  }
  public virtual OverrideVisualComponent withOptions(string options)=>this;
  public static OverrideVisualComponent TryGet(Entity e)=>e.Get<OverrideVisualComponent>();
  
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
  protected virtual void AddedOverride(IOverrideVisuals o, short prio){}
  public void AddToOverride(VisualOverrideDescr v){
    int alr = GetOverriderIdx(v.o);
    if(alr != -1){
      if(parents[alr].order != v.order) RemoveFromOverride(v.o);
      else{
        SetStealUse(v.o,v.steal,v.use);
        return;
      }
    }
    int idx = 0;
    bool stolen = false;
    while(idx<parents.Count && parents[idx].order<v.order){
      stolen |= parents[idx].steal;
      idx++;
    }
    VisualOverrideDescr v_ = new(v.o,v.order, false,false);
    parents.Insert(idx, v_);
    AddedOverride(v.o,v.order);
    SetStealUse(v.o, v.steal, v.use);
  }
  protected virtual void RemovedOverride(IOverrideVisuals o){}
  public void RemoveFromOverride(IOverrideVisuals v){
    int idx = GetOverriderIdx(v);
    if(idx==-1)return;
    SetStealUse(v,false,false);
    parents.RemoveAt(idx);
    RemovedOverride(v);
  }
  protected const int NOSTEALPRIO = 40000;
  protected int GetPrio(IOverrideVisuals v){
    for(int i=0; i<parents.Count; i++) if(parents[i].o==v) return parents[i].order;
    return NOSTEALPRIO;
  }
  protected virtual void NewStealPrio(bool stolen, int prio){}
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
      if(nuse) desc.o.AddC(this);
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
      if(descr.steal){
        NewStealPrio(true, nsteal? desc.order : descr.order);
        return;
      }
    }
    NewStealPrio(nsteal, nsteal? desc.order : NOSTEALPRIO);
    setNvis(!nsteal);
  }
  public void CopyOther(OverrideVisualComponent c){
    OnRemove();
    if(c is null) return;
    bool hasBeenStolen = false;
    foreach(var p in c.parents){
      parents.Add(new(p.o,p.order,p.steal,p.use));
      if(p.use && !hasBeenStolen) p.o.AddC(this);
      if(p.steal) hasBeenStolen=true;
    }
    setNvis(!hasBeenStolen);
  }
  public static void EnsureParity(Entity fromEnt, Entity toEnt){
    if(TryGet(fromEnt) is not {} src){
      if(TryGet(toEnt) is not {} r) return;
      toEnt.Remove(r);
      return;
    }
    var dst = Get(toEnt);
    var srcp = src.parents;
    var dstp = dst.parents;
    if(srcp.Count!=dstp.Count) goto align;
    for(int i=0; i<srcp.Count; i++){
      if(srcp[i].o!=dstp[i].o){
        goto align;
      }
    }
    return;
    align:
      dst.CopyOther(src);
  }
  public override void OnRemove() {
    foreach(var p in parents)p.o.RemoveC(this);
    parents.Clear();
    nvis=true;
    overriden=false;
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
  public bool checkRenderStatus=>ovis && Entity.Scene!=null;
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
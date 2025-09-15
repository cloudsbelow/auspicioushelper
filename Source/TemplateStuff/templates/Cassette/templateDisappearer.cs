



using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class TemplateDisappearer:Template{

  bool selfVis = true;
  bool selfCol = true;
  bool selfAct = true;
  bool parentVis = true;
  bool parentCol = true;
  bool parentAct = true;
  public TemplateDisappearer(EntityData data, Vector2 pos, int depthoffset):base(data,pos,depthoffset){
  }
  public TemplateDisappearer(Vector2 pos, int depthoffset=0):base(pos,depthoffset){}
  public override void Added(Scene scene){
    base.Added(scene);
    if(children.Count>0)UpdateHook.AddAfterUpdate(enforce,true,false);
  }
  int permute(bool action, ref bool activator, bool other){
    if(action == activator){
      DebugConsole.WriteFailure("THIS SHOULD NOT HAPPEN - CODE IS BORKED");
      return 0;
    }
    activator = action;
    if(other == false) return 0;
    return action?1:-1;
  }
  public override void parentChangeStat(int vis, int col, int act){
    int nvis = 0; int ncol=0; int nact = 0;
    if(vis!=0 && parentVis!=vis>0) nvis = permute(vis>0, ref parentVis, selfVis);
    if(col!=0 && parentCol!=col>0) ncol = permute(col>0, ref parentCol, selfCol);
    if(act!=0 && parentAct!=act>0) nact = permute(col>0, ref parentAct, selfAct);
    if(nvis!=0 || ncol!=0 || nact!=0){
      base.parentChangeStat(nvis,ncol,nact);
    }
  }
  public void parentChangeStatBypass(int vis, int col, int act){
    base.parentChangeStat(vis, col, act);
  }
  public virtual void setCollidability(bool n){
    if(n == selfCol) return;
    int ncol = permute(n, ref selfCol, parentCol);
    if(ncol != 0) base.parentChangeStat(0,ncol,0);
  }
  public virtual void setVisibility(bool n){
    if(n == selfVis) return;
    int nvis = permute(n, ref selfVis, parentVis);
    if(nvis != 0) base.parentChangeStat(nvis,0,0);
  }
  public virtual void setAct(bool n){
    if(n == selfAct) return;
    int nact = permute(n, ref selfAct, parentAct);
    if(nact != 0) base.parentChangeStat(0,0,nact);
  }
  public virtual void setVisCol(bool vis, bool col){
    int nvis = 0;
    if(vis != selfVis) nvis = permute(vis, ref selfVis, parentVis);
    int ncol = 0;
    if(col != selfCol) ncol = permute(col, ref selfCol, parentCol);
    if(nvis!=0 || ncol!=0) base.parentChangeStat(nvis,ncol,0);
  }
  public virtual void setVisColAct(bool vis, bool col, bool act){
    int nvis = 0;
    if(vis != selfVis) nvis = permute(vis, ref selfVis, parentVis);
    int ncol = 0;
    if(col != selfCol) ncol = permute(col, ref selfCol, parentCol);
    int nact = 0;
    if(act != selfAct) nact = permute(act, ref selfAct, parentAct);
    //DebugConsole.Write($" set {vis} {col} {selfVis}");
    if(nvis!=0 || ncol!=0 || nact!=0) base.parentChangeStat(nvis,ncol,nact);
  }
  public bool getParentCol(){
    return parentCol;
  }
  public bool getSelfCol(){
    return selfCol;
  }
  public bool getSelfVis()=>selfVis;
  public bool getSelfAct()=>selfAct;
  public void enforce(){
    bool Vis = selfVis&&parentVis; 
    bool Col = selfCol&&parentCol; 
    bool Act = selfAct&&parentAct;
    if(Vis && Col && Act) return;
    parentChangeStatBypass(Vis?0:-1,Col?0:-1,Act?0:-1);
  }
}

[CustomEntity("auspicioushelper/TemplateEntityModifier")]
public class TemplateEntityModifier:TemplateDisappearer{
  public TemplateEntityModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  string actCh;
  string colCh;
  string visCh;
  string shakeCh;
  HashSet<string> only;
  GroupTracker.TrackedGroupComp ents;
  bool log;
  public TemplateEntityModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    actCh = d.Attr("activeChannel");
    colCh = d.Attr("collidableChannel");
    visCh = d.Attr("visibleChannel");
    shakeCh = d.Attr("shakeChannel");
    log = d.Bool("log",false);
    if(d.tryGetStr("only", out string lis)){
      only = new(Util.listparseflat(lis,false,true));
    }
  }
  void AddChwatcher(string s, bool vis, bool col, bool act){
    if(string.IsNullOrWhiteSpace(s)) return;
    Add(new ChannelTracker(s, (int nval)=>{
      bool num = nval!=0;
      if(ents==null) setVisColAct(vis?num:getSelfVis(), col?num:getSelfCol(), act?num:getSelfAct());
      else foreach(Entity e in ents){
        if(vis) e.Visible = num;
        if(col) e.Collidable = num;
        if(act) e.Active = num;
      }
    }, true));
  }
  public override void OnNewEnts(List<Entity> l) {
    if(only!=null || log) foreach(Entity e in l){
      string name = e.GetType().FullName;
      if(log) DebugConsole.Write($"In {this}: adding extra {name}");
      if(only?.Contains(e.GetType().FullName)??false) ents.Add(e);
    }
    base.OnNewEnts(l);
  }
  public override void addTo(Scene scene) {
    base.addTo(scene);
    if(only!=null || log){
      List<Entity> l = new();
      AddAllChildren(l);
      if(only!=null)Add(ents = new());
      foreach(Entity e in l){
        string name = e.GetType().FullName;
        if(log) DebugConsole.Write($"In {this}: contains {name}");
        if(only?.Contains(e.GetType().FullName)??false) ents.Add(e);
      }
    }
    //feels like a crime
    foreach(string s in new HashSet<string>(){actCh?.Trim(),colCh?.Trim(),visCh?.Trim()}){
      AddChwatcher(s, s==visCh, s==colCh, s==actCh);
    }
    if(!string.IsNullOrWhiteSpace(shakeCh)){
      Add(new ChannelTracker(shakeCh,(int n)=>{
        if(n!=0) shake(100000);
        else EndShake();
      }));
    }
  }
}
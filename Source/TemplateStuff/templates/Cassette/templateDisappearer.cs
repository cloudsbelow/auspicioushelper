



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
  public struct vcaTracker{
    public bool Active=true; 
    public bool Collidable=true; 
    public bool Visible=true;
    public vcaTracker(){}
    public void Align(int vis, int col, int act){
      if(vis!=0) Visible=vis>0;
      if(col!=0) Collidable=col>0;
      if(act!=0) Active=act>0; 
    }
    public override string ToString()=>$"VCA:{{vis:{Visible},col:{Collidable},act:{Active}}}";
  }
  public TemplateDisappearer(EntityData data, Vector2 pos, int depthoffset):base(data,pos,depthoffset){
  }
  public TemplateDisappearer(Vector2 pos, int depthoffset=0):base(pos,depthoffset){}
  public override void Added(Scene scene){
    base.Added(scene);
    if(children.Count>0)UpdateHook.AddAfterUpdate(enforce,false,true);
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
    if(nact != 0) using(new Util.AutoRestore<bool>(ref Active)) base.parentChangeStat(0,0,nact);
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
    if(nvis!=0 || ncol!=0 || nact!=0) using(new Util.AutoRestore<bool>(ref Active)) base.parentChangeStat(nvis,ncol,nact);
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
    using(new Util.AutoRestore<bool>(ref Active))parentChangeStatBypass(Vis?0:-1,Col?0:-1,Act?0:-1);
  }
}

[CustomEntity("auspicioushelper/TemplateEntityModifier")]
public class TemplateEntityModifier:TemplateDisappearer, Template.IRegisterEnts{
  public TemplateEntityModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  string actCh;
  string colCh;
  string visCh;
  string shakeCh;
  bool customSpeed;
  ChannelTracker speedTracker;
  HashSet<string> only;
  GroupTracker.TrackedGroupComp ents;
  bool log;
  public TemplateEntityModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    actCh = d.Attr("activeChannel");
    colCh = d.Attr("collidableChannel");
    visCh = d.Attr("visibleChannel");
    customSpeed = d.Bool("allowCustomSpeed",false);
    shakeCh = d.Attr("shakeChannel");
    log = d.Bool("log",false);
    if(d.tryGetStr("only", out string lis)){
      only = new(Util.listparseflat(lis,false,true));
    }
  }
  void AddChwatcher(string s, bool vis, bool col, bool act){
    if(string.IsNullOrWhiteSpace(s)) return;
    Add(new ChannelTracker(s, (double nval)=>{
      bool num = nval!=0;
      if(ents==null) setVisColAct(vis?num:getSelfVis(), col?num:getSelfCol(), act?num:getSelfAct());
      else foreach(Entity e in ents){
        if(vis) e.Visible = num;
        if(col) e.Collidable = num;
        if(act) e.Active = num;
      }
    }, true));
  }
  public override void RegisterEnts(List<Entity> l) {
    if(only!=null || log || customSpeed) foreach(Entity e in l){
      string name = e.GetType().FullName;
      if(log) DebugConsole.Write($"In {this}: adding extra {name}");
      if((only?.Contains(e.GetType().FullName)??false || customSpeed) && e!=this) ents.Add(e);
    }
    base.RegisterEnts(l);
  }
  public override void addTo(Scene scene) {
    if(only!=null || customSpeed) ents = new();
    base.addTo(scene);
    //feels like a crime
    if(visCh.HasContent()) Add(new ChannelTracker(visCh,(double n)=>{
      bool nv = n!=0;
      if(only==null) setVisibility(nv);
      else foreach(var ent in ents) ent.Visible = nv; 
    }, true));
    if(colCh.HasContent()) Add(new ChannelTracker(colCh,(double n)=>{
      bool nv = n!=0;
      if(only==null) setCollidability(nv);
      else foreach(var ent in ents) ent.Collidable = nv; 
    }, true));
    if(actCh.HasContent()){
      if(!customSpeed) Add(new ChannelTracker(colCh,(double n)=>{
        bool nv = n!=0;
        if(only==null) setAct(nv);
        else foreach(var ent in ents) ent.Active = nv; 
      }, true));
      else {
        setAct(false);
        Add(speedTracker = new ChannelTracker(actCh));
      }
    }
    if(!string.IsNullOrWhiteSpace(shakeCh)){
      Add(new ChannelTracker(shakeCh,(double n)=>{
        if(n!=0) shake(100000);
        else EndShake();
      },true));
    }
  }
  public override void Update() {
    base.Update();
    if(speedTracker != null){
      var old = Engine.DeltaTime;
      Engine.DeltaTime = old*speedTracker.Float;
      foreach(var ent in ents) ent.Update();
      Engine.DeltaTime = old;
    }
  }
}
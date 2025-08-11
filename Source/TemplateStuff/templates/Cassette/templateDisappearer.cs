



using System;
using System.Collections.Generic;
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
    unenforced.Add(this);
    hooks.enable();
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
    if(vis!=0) nvis = permute(vis>0, ref parentVis, selfVis);
    if(col!=0) ncol = permute(col>0, ref parentCol, selfCol);
    if(act!=0) nact = permute(col>0, ref parentAct, selfAct);
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
  static List<TemplateDisappearer> unenforced = new();
  public void enforce(){
    bool Vis = selfVis&&parentVis; 
    bool Col = selfCol&&parentCol; 
    bool Act = selfAct&&parentAct;
    if(Vis && Col && Act) return;
    parentChangeStatBypass(Vis?0:-1,Col?0:-1,Act?0:-1);
  }
  static void enforceHook(On.Celeste.Level.orig_Update orig, Level self){
    foreach(var a in unenforced) a.enforce();
    unenforced.Clear();
    orig(self);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Level.Update += enforceHook;
  },()=>{
    On.Celeste.Level.Update -= enforceHook;
  }, auspicioushelperModule.OnEnterMap);
}
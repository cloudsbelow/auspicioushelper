


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class IopControlled:ITemplateChild{
  public class IopCbs{
    public Type ioptype;
    public Util.FieldHelper<bool> ParentVisible;
    public Util.FieldHelper<bool> ParentCollidable;
    public Util.FieldHelper<bool> ParentActive;
    public Util.FieldHelper<Entity> parent;
    public Util.FieldHelper<Action<Vector2, Vector2>> RepositionCB;
    public Util.FieldHelper<Action<Vector2>> SetOffsetCB;
    public Util.FieldHelper<Action<int, int, int>> ChangeStatusCB;
    public Util.FieldHelper<Action<bool>> DestroyCB;
    public Util.FieldHelper<Action<List<Entity>>> AddSelf;
    public Util.FieldHelper<Action<Scene>> AddTo;
    public IopCbs(Type ty){
      ioptype = ty;
      ParentVisible = new Util.FieldHelper<bool>(ty,"ParentVisible");
      ParentCollidable = new Util.FieldHelper<bool>(ty,"ParentCollidable");
      ParentActive = new Util.FieldHelper<bool>(ty,"ParentActive");
      parent = new Util.FieldHelper<Entity>(ty,"parent");

      RepositionCB = new(ty, "RepositionCB", true);
      SetOffsetCB = new(ty, "SetOffsetCB", true);
      ChangeStatusCB = new(ty, "ChangeStatusCB", true);
      DestroyCB = new(ty, "DestroyCB", true);
      AddSelf = new(ty, "AddSelf", true);
      AddTo = new(ty, "AddTo", true);
    }
  }
  static Dictionary<Type, IopCbs> cbdict=new();
  Action<Scene> AddTo=>own.AddTo.get(iopTarget);
  Action<List<Entity>> AddSelf=>own.AddSelf.get(iopTarget);
  Action<Vector2,Vector2> RelposTo=>own.RepositionCB.get(iopTarget);
  Action<Vector2> SetOffset=>own.SetOffsetCB.get(iopTarget);
  Action<bool> Destroy=>own.DestroyCB.get(iopTarget);
  Action<int,int,int> ParentChangeStat=>own.ChangeStatusCB.get(iopTarget);
  public Template parent {get;set;}
  Component iopTarget;
  IopCbs own;
  public class EntEnt{
    public Vector2 offset;
    public Vector2 lpos;
    public Entity e;
    public EntEnt(Entity e, Vector2 o){
      offset=o; this.e=e;
      if(e is Decal) offset = o.Round();
      lpos = e.Position;
    }
  }
  List<EntEnt> ents = new List<EntEnt>();
  Entity e;
  public IopControlled(Component c){
    if(!cbdict.TryGetValue(c.GetType(),out own)){
      cbdict.Add(c.GetType(),own = new IopCbs(c.GetType()));
    }
    iopTarget = c;
    e=c.Entity;
  }
  public void addTo(Scene s){
    if(AddTo!=null)AddTo(s);
    own.parent.set(iopTarget,parent);
    List<Entity> l_ = new();
    lloc = parent.roundLoc;
    if(AddSelf!=null)AddSelf(l_);
    else l_.Add(e);
    foreach(Entity e in l_){
      e.Depth+=parent.depthoffset;
      ChildMarker.Get(e,parent);
      ents.Add(new(e,e.Position-lloc));
    }
    if(AddTo==null)foreach(Entity e in l_)s.Add(e);
  }
  public void setOffset(Vector2 ppos){
    if(SetOffset!=null) SetOffset(ppos);
  }
  Vector2 lloc;
  public void relposTo(Vector2 loc, Vector2 ls){
    if(RelposTo!=null){
      if(SetOffset!=null || ents.Count==0)RelposTo(loc,ls);
      else {
        var en = ents[0];
        if(en.e.Position!=en.lpos){
          en.offset = en.e.Position-lloc;
        }
        RelposTo(loc+en.offset,ls);
        en.lpos = en.e.Position;
      }
    } else {
      Vector2 nloc = loc.Round();
      if(nloc == lloc) return;
      for(var i=0; i<ents.Count; i++){
        var en = ents[i];
        if(en.e is Decal){
          en.e.Position=nloc+en.offset;
        }else {
          if(en.e.Position != en.lpos){
            en.offset = en.e.Position-lloc;
          }
          if(en.e is Platform p){
            p.MoveTo(nloc+en.offset,ls);
          } else en.e.Position = nloc+en.offset;
          en.lpos = en.e.Position;
        }
      }
      lloc = nloc;
    }
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0) own.ParentVisible.set(iopTarget,vis>0);
    if(col!=0) own.ParentVisible.set(iopTarget,col>0);
    if(act!=0) own.ParentVisible.set(iopTarget,act>0);
    if(ParentChangeStat!=null) ParentChangeStat(vis,col,act);
    else {
      foreach(EntEnt ent in ents){
        if(vis!=0) ent.e.Visible = vis>0; 
        if(col!=0){
          ent.e.Collidable = col>0; 
          foreach(Component c in ent.e.Components){
            if(c is PlayerCollider cl) cl.Active=col>0;
          }
        }
        if(act!=0) ent.e.Active = act>0;
      }
    }
  }
  public void destroy(bool particles){
    if(Destroy!=null) Destroy(particles);
    else foreach(var e in ents){
      e.e.RemoveSelf();
    }
  }
  public void AddAllChildren(List<Entity> l){
    if(AddSelf!=null)AddSelf(l);
    else foreach(var e in ents){
      l.Add(e.e);
    }
  }
  public bool hasRiders<T>() where T:Actor{
    if(typeof(T)==typeof(Player)) return hasPlayerRider();
    throw new NotImplementedException();
  }
  public bool hasPlayerRider(){
    foreach(var en in ents){
      if(en.e is Platform p){
        if(p is Solid s && s.HasPlayerRider()) return true;
        if(p is JumpThru j && j.HasPlayerRider()) return true; 
      }
    }
    return false;
  }
}
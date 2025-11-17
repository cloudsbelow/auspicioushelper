



using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public interface ISimpleEnt:ITemplateChild{
  Template.Propagation ITemplateChild.prop => Template.Propagation.Shake;
  bool detatched=>false;
  void ITemplateChild.AddAllChildren(List<Entity> l){
    if(detatched) return;
    l.Add((Entity)this);
  }
  void ITemplateChild.addTo(Scene s){
    if(this is Entity e){
      s.Add(e);
    } else {
      DebugConsole.WriteFailure($"{this} implements simpleent wihtout being entity");
    }
  }
  void ITemplateChild.destroy(bool particles){
    if(detatched) return;
    ((Entity) this).RemoveSelf();
  }
  Vector2 toffset {get;set;}
  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    if(detatched) return;
    Entity s = (Entity)this;
    if(vis!=0) s.Visible=vis>0;
    if(col!=0) s.Collidable = col>0;
    if(act!=0) s.Active = act>0;
  }
  void defaultPCS(int vis, int col, int act){
    Entity s = (Entity)this;
    if(vis!=0) s.Visible=vis>0;
    if(col!=0) s.Collidable = col>0;
    if(act!=0) s.Active = act>0;
  }
  void ITemplateChild.setOffset(Vector2 ppos){
    toffset = ((Entity) this).Position-ppos;
    ((Entity)this).Depth+=parent.depthoffset;
  }
  void ITemplateChild.relposTo(Vector2 vector, Vector2 liftspeed){
    if(detatched) return;
    ((Entity) this).Position = vector+toffset;
  }
}

public interface ISimpleWrapper:ITemplateChild{
  Entity wrapped {get;}
  Vector2 toffset {get;set;}
  void ITemplateChild.AddAllChildren(List<Entity> list) {
    list.Add(wrapped);
  }
  void ITemplateChild.addTo(Scene s) {
    s.Add(wrapped);
  }
  void ITemplateChild.destroy(bool particles) {
    wrapped.RemoveSelf();
  }
  void ITemplateChild.parentChangeStat(int vis, int col, int act) {
    if(vis!=0) wrapped.Visible = vis>0;
    if(col!=0) wrapped.Collidable = col>0;
    if(act!=0) wrapped.Active = col>0;
  }
  void ITemplateChild.setOffset(Vector2 ppos) {
    toffset = wrapped.Position-ppos;
  }
  void ITemplateChild.relposTo(Vector2 loc, Vector2 liftspeed) {
    wrapped.Position = (loc+toffset).Round();
  }
}

internal class BasicPlatform:ITemplateChild{
  public Template parent{get;set;}
  public Template.Propagation prop {get;} = Template.Propagation.All;
  public Platform p;
  public Vector2 toffset;
  public BasicPlatform(Platform p, Template t, Vector2 offset){
    p.Depth += t.depthoffset;
    this.p=p;
    parent = t;
    toffset = offset;
    if(p.OnDashCollide == null && !(p is DreamBlock))
      p.OnDashCollide = (Player p, Vector2 dir)=>((ITemplateChild) this).propagateDashhit(p,dir);
    lpos = p.Position;
    p.Add(new ChildMarker(t));
  }
  public Vector2 lpos;
  public virtual void relposTo(Vector2 loc, Vector2 liftspeed){
    if(p == null||p.Scene==null)return;
    if(lpos!=p.Position){
      //DebugConsole.Write($"changing tpos {lpos} {p.Position}     {toffset} {toffset+p.Position-lpos}");
      toffset+=p.Position-lpos;
    }
    p.MoveTo(loc+toffset, liftspeed);
    lpos = p.Position;
  }
  public void addTo(Scene scene){
    scene.Add(p);
  }
  public bool hasRiders<T>() where T:Actor{
    if(p == null || p.Scene==null) return false;
    if(p is Solid s){
      if(typeof(T) == typeof(Player)) return s.HasPlayerRider();
      if(typeof(T) == typeof(Actor)) return s.HasRider();
      return false;
    } else if(p is JumpThru j){
      if(typeof(T) == typeof(Player)) return j.HasPlayerRider();
      if(typeof(T) == typeof(Actor)) return j.HasRider();
    }
    return false;
  }
  public bool hasInside(Actor a){
    if(p == null)return false;
    return (p is Solid) && p.Collider.Collide(a.Collider);
  }
  public void AddAllChildren(List<Entity> l){
    l.Add(p);
  }
  public void parentChangeStat(int vis, int col, int act){
    if(p == null||p.Scene==null)return;
    if(vis!=0)p.Visible = vis>0;
    if(col!=0)p.Collidable = col>0;
    if(act!=0)p.Active = act>0;
    if(col>0) p.EnableStaticMovers();
    else if(col<0) p.DisableStaticMovers();
  }
  public void destroy(bool particles){
    p.RemoveSelf();
  }
}

public class ChildMarker:Component,IFreeableComp{
  public Template parent;
  public ChildMarker(Template parent):base(false,false){
    this.parent=parent;
  }
  public bool propagatesTo(Template other){
    return ((ITemplateChild) parent).propagatesTo(other)!=Template.Propagation.None;
  }
  void IFreeableComp.Free()=>Entity.Remove(this);
}
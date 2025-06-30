



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
      e.Scene = s;
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
}





using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

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
    if(p == null||p.Scene==null)return false;
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

internal class BasicPlatformDisobedient:BasicPlatform{
  Vector2 origpos;
  Vector2 origtoffset;
  public BasicPlatformDisobedient(Platform p, Template t, Vector2 offset):base(p,t,offset){
    origpos = p.Position;
    origtoffset = toffset;
  }
  public override void relposTo(Vector2 loc, Vector2 liftspeed){
    if(lpos!=p.Position){
      toffset=origtoffset+(p.Position-origpos);
    }
    p.MoveTo(loc+toffset, liftspeed);
    lpos = p.Position;
  }
}

public class ChildMarker:Component{
  public Template parent;
  public ChildMarker(Template parent):base(false,false){
    this.parent=parent;
  }
  public bool propagatesTo(Template other){
    return ((ITemplateChild) parent).propagatesTo(other)!=Template.Propagation.None;
  }
}

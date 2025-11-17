




using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class BasicMultient:ITemplateChild{
  public Template parent{get;set;}
  public Template.Propagation prop {get;} = Template.Propagation.Shake;
  public class EntEnt{ //entity entry, say it faster
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
  int depthoffset;
  Scene Scene = null;
  Vector2 lloc;
  static Util.FieldHelper<List<Solid>> decalSolids= new(typeof(Decal),"solids",false);
  public BasicMultient(Template t){
    parent = t;
    depthoffset = t.depthoffset;
    lloc = t.roundLoc;
  }
  public void add(Entity e, Vector2 offset){
    ents.Add(new EntEnt(e,offset));
    e.Depth+=depthoffset;
    if(e.Scene == null && this.Scene != null) Scene.Add(e);
    if(e is Decal d){
      hooks.enable();
      d.Add(new ChildMarker(parent));
    }
  }
  public void relposTo(Vector2 loc, Vector2 liftspeed){
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
        en.e.Position = nloc+en.offset;
        en.lpos = en.e.Position;
      }
    }
    lloc = nloc;
  }
  public void sceneadd(Scene scene){
    this.Scene = scene;
    foreach(EntEnt en in ents){
      scene.Add(en.e);
    }
  }
  public void AddAllChildren(List<Entity> l){
    foreach(EntEnt ent in ents)l.Add(ent.e);
  }
  public void parentChangeStat(int vis, int col, int act){
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
  public void destroy(bool particles){
    foreach(EntEnt ent in ents){
      ent.e.RemoveSelf();
    }
    ents.Clear();
  }
  static void Hook(On.Celeste.Decal.orig_Added orig, Decal d, Scene s){
    orig(d,s);
    if(d.Get<ChildMarker>()?.parent is Template t){
      if(decalSolids.get(d) is {} l)foreach(Solid solid in l){
        t.addEnt(new BasicPlatform(solid,t,solid.Position-t.roundLoc));
      }
    }
  }
  static HookManager hooks = new(()=>{
    On.Celeste.Decal.Added+=Hook;
  },()=>{
    On.Celeste.Decal.Added-=Hook;
  },auspicioushelperModule.OnEnterMap);
}
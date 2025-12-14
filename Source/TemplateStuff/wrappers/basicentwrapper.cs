




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
    public bool detatched = false;
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
    EntEnt en = new EntEnt(e,offset);
    ents.Add(en);
    e.Depth+=depthoffset;
    if(e.Scene == null && this.Scene != null) Scene.Add(e);
    hooks.enable();
    ChildMarker.Get(e,parent).data = en;
  }
  public void relposTo(Vector2 loc, Vector2 liftspeed){
    Vector2 nloc = loc.Round();
    if(nloc == lloc) return;
    for(var i=0; i<ents.Count; i++){
      var en = ents[i];
      if(en.e is Decal){
        en.e.Position=nloc+en.offset;
      }else {
        if(en.detatched) continue;
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
    foreach(EntEnt ent in ents) if(!ent.detatched) l.Add(ent.e);
  }
  public void parentChangeStat(int vis, int col, int act){
    foreach(EntEnt ent in ents){
      if(ent.detatched) continue;
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
      if(ent.detatched) continue;
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
  static void Hook(On.Celeste.Follower.orig_OnGainLeaderUtil orig, Follower f, Leader l){
    orig(f,l);
    if(f.Entity?.Get<ChildMarker>()?.data is EntEnt ent) ent.detatched = true;
  }
  static void Hook(On.Celeste.Follower.orig_OnLoseLeaderUtil orig, Follower f){
    orig(f);
    if(f.Entity?.Get<ChildMarker>()?.data is EntEnt ent) ent.detatched = false;
  }
  static HookManager hooks = new(()=>{
    On.Celeste.Decal.Added+=Hook;
    On.Celeste.Follower.OnGainLeaderUtil += Hook;
    On.Celeste.Follower.OnLoseLeaderUtil += Hook;
  },()=>{
    On.Celeste.Decal.Added-=Hook;
    On.Celeste.Follower.OnGainLeaderUtil -= Hook;
    On.Celeste.Follower.OnLoseLeaderUtil -= Hook;
  },auspicioushelperModule.OnEnterMap);
}
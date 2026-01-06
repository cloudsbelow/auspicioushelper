


using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/water")]
[TrackedAs(typeof(Water))]
public class FancyWater:Water, ISimpleEnt{
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Template.Propagation ITemplateChild.prop=>Template.Propagation.Riding | Template.Propagation.Shake;
  float drag = 1;
  public FancyWater(EntityData d, Vector2 o):base(d.Position+o,false,false,d.Width,d.Height){

  }
  void ITemplateChild.relposTo(Vector2 loc, Vector2 ls){
    Vector2 delta = (loc+toffset)-Position;
    foreach(Actor a in Scene.Tracker.GetEntities<Actor>()) if(a.CollideCheck(this)){
      a.MoveH(delta.X*drag);
      a.MoveV(delta.Y*drag);
      a.LiftSpeed = ls*drag;
    }
    Position = loc+toffset;
  }
  bool ITemplateChild.hasPlayerRider()=>UpdateHook.cachedPlayer?.CollideCheck(this)??false;
  void Build(List<FancyWater> things, SolidTiles occlude){

  }
  bool searched = false;
  void ITemplateChild.templateAwake(){
    if(!searched){
      List<FancyWater> l = new();
      foreach(var c in parent.children) if(c is FancyWater w && !w.searched) l.Add(w);
      Build(l, parent.fgt);
    }
  }
  public override void Awake(Scene scene){
    base.Awake(scene);
    if(!searched){
      List<FancyWater> l = new();
      foreach(var w in scene.Tracker.GetEntities<Water>()) if(w is FancyWater fw && fw.parent==null && !fw.searched) l.Add(fw);
      Build(l, (scene as Level).SolidTiles);
    }
  }
}
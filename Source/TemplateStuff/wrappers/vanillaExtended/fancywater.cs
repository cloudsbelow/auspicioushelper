


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
  [Flags]
  enum Edges{
    none=0, left=1, right=2, top=4, bottom=8
  }
  Edges edges;
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
  class Edgepoint{
    Vector2 point;
    Vector2 normal;
  }
  Int2 edgeToVec(Edges e)=> e switch{
    Edges.left=>new(-1,0),
    Edges.right=>new(1,0),
    Edges.top=>new(0,-1),
    Edges.bottom=>new(0,1),
    _=>throw new Exception("non singular edge")
  };
  Edges nextCCWInner(Edges e)=> e switch{
    Edges.left=>Edges.bottom,
    Edges.right=>Edges.top,
    Edges.top=>Edges.left,
    Edges.bottom=>Edges.right,
    _=>throw new Exception("non singular edge")
  };
  Edges nextCCWOuter(Edges e)=> e switch{
    Edges.left=>Edges.top,
    Edges.right=>Edges.bottom,
    Edges.top=>Edges.right,
    Edges.bottom=>Edges.left,
    _=>throw new Exception("non singular edge")
  };
  bool CollectCCW(Dictionary<Int2, Edges> e, MipGrid.Layer cull, Int2 at, Edges edge, List<Edgepoint> into){
    Int2 dir = edgeToVec(edge);
    e[at] = e[at]&~edge;
    if(cull.conta)
    if(cull.collidePoint(at+dir)){
      var n = nextCCWInner(edge);
      if(e.GetValueOrDefault(at+dir).HasFlag(n)) return CollectCCW(e,cull,at,n,into);
    }
    return false;
  }
  List<Edgepoint> extractEdges(Dictionary<Int2, Edges> e, MipGrid.Layer cull){
    return null;
  }
  void Build(List<FancyWater> things, SolidTiles occlude){
    MiptileCollider.fromGrid(occlude.Collider as Grid);
    List<IntRect> bounds = things.Map(x=>new IntRect(x));
    Int2 tlc = bounds.ReduceMap(x=>x.tlc,Int2.Min,Int2.One*int.MaxValue);
    Int2 extents = bounds.ReduceMap(x=>x.brc-tlc, Int2.Max, Int2.Zero);
    MipGrid.Layer inside = new((extents.x+7)/8,(extents.y+7)/8);
    foreach(var b in bounds)inside.SetRect(true, b.tlc-tlc, b.brc-tlc);
    Dictionary<Int2, Edges> e = new();
    for(int i=0; i<things.Count; i++){
      var f = things[i];
      var b = bounds[i];
      if(f.edges.HasFlag(Edges.left)) for(int j=0; j<b.h; j++){
        Int2 loc = new Int2(b.x,b.y+j)-tlc;
        e[loc] = e.GetValueOrDefault(loc) | Edges.left;
      }
      if(f.edges.HasFlag(Edges.right)) for(int j=0; j<b.h; j++){
        Int2 loc = new Int2(b.x+b.w-1,b.y+j)-tlc;
        e[loc] = e.GetValueOrDefault(loc) | Edges.right;
      }
      if(f.edges.HasFlag(Edges.top)) for(int j=0; j<b.h; j++){
        Int2 loc = new Int2(b.x+j,b.y)-tlc;
        e[loc] = e.GetValueOrDefault(loc) | Edges.top;
      }
      if(f.edges.HasFlag(Edges.left)) for(int j=0; j<b.h; j++){
        Int2 loc = new Int2(b.x+j,b.y+b.h-1)-tlc;
        e[loc] = e.GetValueOrDefault(loc) | Edges.bottom;
      }
    }
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
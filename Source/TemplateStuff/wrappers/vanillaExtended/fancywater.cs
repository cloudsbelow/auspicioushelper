


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

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
    none=0, left=1, right=2, top=4, bottom=8, all=15
  }
  Edges edges = Edges.none;
  FloatRect relativeDraw;
  public FancyWater(EntityData d, Vector2 o):base(d.Position+o,false,false,d.Width,d.Height){
    if(d.Bool("hasTop",true))edges|=Edges.top;
    if(d.Bool("hasBottom",false))edges|=Edges.bottom;
    if(d.Bool("hasLeft",false))edges|=Edges.left;
    if(d.Bool("hasRight",false))edges|=Edges.right;
    Vector2 tlc = new(edges.HasFlag(Edges.left)?8:0,edges.HasFlag(Edges.top)?8:0);
    Vector2 brc = new Vector2(Width,Height)-new Vector2(edges.HasFlag(Edges.right)?8:0,edges.HasFlag(Edges.bottom)?8:0);
    relativeDraw = FloatRect.fromCorners(tlc,brc);
    addSurfacesSimple(new(Width,Height));
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
  public override void Render(){
    Draw.Rect(Position.X+relativeDraw.x,Position.Y+relativeDraw.y, relativeDraw.w,relativeDraw.h,Color.Red);
    if(surfaces.Count>0){
      GameplayRenderer.End();
      foreach(var surface in surfaces) surface.Render(Position);
      GameplayRenderer.Begin();
    }
  }
  bool ITemplateChild.hasPlayerRider()=>UpdateHook.cachedPlayer?.CollideCheck(this)??false;
  struct Edgepoint{
    public Vector2 point;
    public Vector2 normal;
    public Edgepoint(Vector2 p, Vector2 n){
      point = p; normal = n;
    }
  }
  List<BentSurface> surfaces = new();
  class BentSurface{
    Edgepoint[] points;
    VertexPositionColor[] mesh;
    int surfaceidx;
    public BentSurface(List<Edgepoint> arr, bool loop){
      points = arr.ToArray();
      surfaceidx = (arr.Count-1)*2*3;
      mesh = new VertexPositionColor[surfaceidx*2];
      for(int i=0; i<surfaceidx; i++){
        mesh[i].Color = FillColor;
        mesh[i+surfaceidx].Color = SurfaceColor;
      }
    }
    public void Render(Vector2 loc){
      Vector3 p11 = (points[0].point+loc).Expand();
      Vector3 p12 = (points[0].point+loc+points[0].normal*4).Expand();
      Vector3 p13 = (points[0].point+loc+points[0].normal*(4+1)).Expand();
      for(int i=0; i<points.Length-1; i++){
        int o = i*6;
        Vector3 p21 = (points[i+1].point+loc).Expand();
        Vector3 p22 = (points[i+1].point+loc+points[i].normal*4).Expand();
        Vector3 p23 = (points[i+1].point+loc+points[i].normal*(4+1)).Expand();
        mesh[o].Position = p11;
        mesh[o+1].Position = p12;
        mesh[o+2].Position = p21;
        mesh[o+3].Position = p21;
        mesh[o+4].Position = p12;
        mesh[o+5].Position = p22;
        int o2 = o+surfaceidx;
        mesh[o2].Position = p12;
        mesh[o2+1].Position = p13;
        mesh[o2+2].Position = p22;
        mesh[o2+3].Position = p22;
        mesh[o2+4].Position = p13;
        mesh[o2+5].Position = p23;
        p11 = p21;
        p12 = p22;
        p13 = p23;
      }
      GFX.DrawVertices((Engine.Instance.scene as Level).Camera.matrix, mesh, mesh.Length);
    }

  }
  void addSurfacesSimple(Vector2 size){
    List<Edgepoint> current = new();
    Edges dir = Edges.top;
    Vector2 center = size/2;
    bool loop=true;
    do{
      bool has = edges.HasFlag(dir);
      if(!has){
        if(current.Count!=0) surfaces.Add(new(current,loop=false));
        current.Clear();
        continue;
      }
      bool lasthas = edges.HasFlag(nextCCWInner(dir));
      var norm = edgeToVec(dir);
      var lvec = edgeToVec(nextCCWInner(dir));
      if(has && lasthas){
        Vector2 pivot = center+(norm+lvec)*(size/2-Vector2.One*8);
        current.Add(new(pivot, sixty(lvec, norm)));
        current.Add(new(pivot, sixty(norm,lvec)));
      }
      float stop = (size*lvec).L1()-(edges.HasFlag(nextCCWOuter(dir))?8:0);
      Vector2 start = center+lvec*size/2+norm*(size/2-Vector2.One*8);
      for(int i=(has&&lasthas)?8:0; i<=stop; i+=4){
        current.Add(new(start-lvec*i,norm));
      }
      
    }while((dir=nextCCWOuter(dir))!=Edges.top);
    if(current.Count>0) surfaces.Add(new(current,loop));
  }
  static float sixtyfac = MathF.Sqrt(3)/2;
  static Vector2 sixty(Vector2 m, Vector2 s)=>m*sixtyfac+s/2;
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
  // bool CollectCCW(Dictionary<Int2, Edges> e, MipGrid.Layer cull, Int2 at, Edges edge, List<Edgepoint> into){
  //   Int2 dir = edgeToVec(edge);
  //   e[at] = e[at]&~edge;
  //   Edges no = nextCCWOuter(edge);
  //   Edges ni = nextCCWInner(edge);
  //   if(!e.GetValueOrDefault(at+edgeToVec(no)).HasFlag(edge))
  //   if(cull.collidePoint(at+dir)){
  //     if(e.GetValueOrDefault(at+dir).HasFlag(ni)) return CollectCCW(e,cull,at,ni,into);
  //   }
  //   return false;
  // }
  // List<Edgepoint> extractEdges(Dictionary<Int2, Edges> e, MipGrid.Layer cull){
  //   return null;
  // }
  // void Build(List<FancyWater> things, SolidTiles occlude){
  //   MiptileCollider.fromGrid(occlude.Collider as Grid);
  //   List<IntRect> bounds = things.Map(x=>new IntRect(x));
  //   Int2 tlc = bounds.ReduceMap(x=>x.tlc,Int2.Min,Int2.One*int.MaxValue);
  //   Int2 extents = bounds.ReduceMap(x=>x.brc-tlc, Int2.Max, Int2.Zero);
  //   MipGrid.Layer inside = new((extents.x+7)/8,(extents.y+7)/8);
  //   foreach(var b in bounds)inside.SetRect(true, b.tlc-tlc, b.brc-tlc);
  //   Dictionary<Int2, Edges> e = new();
  //   for(int i=0; i<things.Count; i++){
  //     var f = things[i];
  //     var b = bounds[i];
  //     if(f.edges.HasFlag(Edges.left)) for(int j=0; j<b.h; j++){
  //       Int2 loc = new Int2(b.x,b.y+j)-tlc;
  //       e[loc] = e.GetValueOrDefault(loc) | Edges.left;
  //     }
  //     if(f.edges.HasFlag(Edges.right)) for(int j=0; j<b.h; j++){
  //       Int2 loc = new Int2(b.x+b.w-1,b.y+j)-tlc;
  //       e[loc] = e.GetValueOrDefault(loc) | Edges.right;
  //     }
  //     if(f.edges.HasFlag(Edges.top)) for(int j=0; j<b.h; j++){
  //       Int2 loc = new Int2(b.x+j,b.y)-tlc;
  //       e[loc] = e.GetValueOrDefault(loc) | Edges.top;
  //     }
  //     if(f.edges.HasFlag(Edges.left)) for(int j=0; j<b.h; j++){
  //       Int2 loc = new Int2(b.x+j,b.y+b.h-1)-tlc;
  //       e[loc] = e.GetValueOrDefault(loc) | Edges.bottom;
  //     }
  //   }
  // }
  // bool searched = false;
  // void ITemplateChild.templateAwake(){
  //   if(!searched){
  //     List<FancyWater> l = new();
  //     foreach(var c in parent.children) if(c is FancyWater w && !w.searched) l.Add(w);
  //     Build(l, parent.fgt);
  //   }
  // }
  // public override void Awake(Scene scene){
  //   base.Awake(scene);
  //   if(!searched){
  //     List<FancyWater> l = new();
  //     foreach(var w in scene.Tracker.GetEntities<Water>()) if(w is FancyWater fw && fw.parent==null && !fw.searched) l.Add(fw);
  //     Build(l, (scene as Level).SolidTiles);
  //   }
  // }
}
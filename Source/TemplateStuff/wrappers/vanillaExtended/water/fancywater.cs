


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

[CustomEntity("auspicioushelper/water","auspicioushelper/waterCopy")]
[TrackedAs(typeof(Water))]
[Tracked]
public partial class FancyWater:Water,ISimpleEnt{
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Template.Propagation ITemplateChild.prop=>Template.Propagation.Riding | Template.Propagation.Shake;
  float drag = 1;
  [Flags]
  enum Edges{
    none=0, left=1, right=2, top=4, bottom=8, all=15
  }
  Edges edges = Edges.none;
  Color fillColor, surfaceColor;
  List<FloatRect> fills;//VertexPositionColor[] inner;
  bool jumpOut, verticalShrink,copy;
  public FancyWater(EntityData d, Vector2 o):base(d.Position+o,false,false,d.Width,d.Height){
    drag = d.Float("tempalteDrag",1);
    jumpOut = d.Bool("jumpOutSides",true);
    TopSurface = new FakeTopsurface(this);
    Collider = new Hitbox(d.Width,d.Height);
    Remove(Get<DisplacementRenderHook>());
    Depth = -8987; 
    if(d.Bool("hasTop",true))edges|=Edges.top;
    if(d.Bool("hasBottom",false))edges|=Edges.bottom;
    if(d.Bool("hasLeft",false))edges|=Edges.left;
    if(d.Bool("hasRight",false))edges|=Edges.right;
    verticalShrink = d.Bool("verticalShrink",true);
    if(d.Name == "auspicioushelper/waterCopy"){
      copy=true;
      return;
    }

    var colors = Util.listparseflat(d.Attr("colors","6CA4C8CC")).Map(Util.hexToColor);
    if(colors.Count==0) colors.Add(Util.hexToColor("6CA4C8CC"));
    if(colors.Count==1) colors.Add(colors[0]*0.375f);
    if(colors.Count==2) colors.Add((colors[0].ToVector4()*0.6f+colors[1].ToVector4()*0.4f).toColor());
    surfaceColor = colors[0];
    fillColor = colors[1];
    rayColors = colors.Skip(2).ToArray();
    rayLengthRange = d.optionalRange("rayLength", new Vector2(16,64), Vector2.One*4, Vector2.One*128);
    rayWidthRange = d.optionalRange("rayWidth", new Vector2(4,12), Vector2.One*2, Vector2.One*20);
    maxRaySegs = (int)Math.Ceiling(rayWidthRange.Y/4+1);
    doInvRays = d.Bool("backwardsRays",false);
    rayDir = d.ChannelFloat("rayDirection", 60);
    rayDensity = Util.Clamp(d.Float("rayDensity", 0.7f), 0, 2);
  }
  void ITemplateChild.relposTo(Vector2 loc, Vector2 ls){
    if(Scene==null || !Collidable) return;
    Vector2 delta = (loc+toffset)-Position;
    foreach(Actor a in Scene.Tracker.GetEntities<Actor>()) if(a.CollideCheck(this)){
      a.MoveH(delta.X*drag);
      a.MoveV(delta.Y*drag);
      a.LiftSpeed = ls*drag;
    }
    Position = loc+toffset;
  }
  bool ITemplateChild.hasPlayerRider()=>UpdateHook.cachedPlayer?.CollideCheck(this)??false;
  public override void Update(){
    if(leader!=null || copy){
      RemoveSelf();
      return;
    }
    foreach(var surface in surfaces) surface.Update(Engine.DeltaTime);
    foreach(WaterInteraction w in Scene.Tracker.GetComponents<WaterInteraction>()){
      bool f1 = contains.Contains(w);
      var old = Collider;
      bool f2 = fills.Any(x=>{
        Collider = new Hitbox(x.w,x.h,x.x,x.y);
        return w.Check(this);
      });
      Collider = old;
      if(f1!=f2){
        var loc = w.AbsoluteCenter;
        DoRipple(loc,1f);
        if (f1){
          if(w.IsDashing()) Audio.Play("event:/char/madeline/water_dash_out", loc, "deep", 0);
          else Audio.Play("event:/char/madeline/water_out", loc, "deep", 0);
          w.DrippingTimer = 2f;
        } else {
          if (w.IsDashing()) Audio.Play("event:/char/madeline/water_dash_in", loc, "deep", 0);
          else Audio.Play("event:/char/madeline/water_in", loc, "deep", 0);
          w.DrippingTimer = 0f;
        }
        if(!f1) contains.Add(w);
        else contains.Remove(w);
      }
    }
  }
  public override void Render(){
    if(leader!=null || copy) return;
    // var cs = new List<Color>(){Color.White, Color.Yellow, Color.Green, Color.LightGray};
    // for(int i=0; i<s.Count; i++){
    //   var c = cs[i%cs.Count];
    //   foreach(var e in s[i].Item1){
    //     Draw.Line(e.a,e.b,c);
    //   }
    // }
    foreach(var r in fills) Draw.Rect(r.tlc+Position, (int)r.w, (int)r.h, fillColor);
    if(surfaces.Count>0){
      GameplayRenderer.End();
      foreach(var surface in surfaces) surface.Render(Position);
      GameplayRenderer.Begin();
    }
  }
  FancyWater leader = null;
  public override void Awake(Scene scene) {
    base.Awake(scene);
    Displacement.For(scene);
    if(copy) RemoveSelf();
    if(leader!=null || copy) return;
    var allThings = Scene.Tracker.GetEntities<FancyWater>().MapAndFilter(o=>{
      var ow = (FancyWater)o;
      return((new IntRect(ow),ow),ow.parent==parent && ow!=this && ow.leader==null);
    });
    List<(IntRect,FancyWater)> things = new(){(new(this),this)};
    int idx = 0;
    while(idx<things.Count){
      int nidx=things.Count;
      allThings.RemoveAll(x=>{
        for(int i=idx; i<nidx; i++) if(things[i].Item1.CollideIr(x.Item1)){
          things.Add(x);
          return true;
        }
        return false;
      });
      idx=nidx;
    } 
    Int2 min = things.ReduceMapI(a=>a.Item1.tlc,Int2.Min);
    Int2 max = things.ReduceMapI(a=>a.Item1.brc,Int2.Max);
    var l = MipGrid.Layer.fromAreasize(max.x-min.x, max.y-min.y);


    List<FloatRect> bounds = new();
    List<Edges> edges = new();
    foreach(var (bound, fw) in things){
      if(fw!=this){
        fw.leader=this;
        fw.RemoveSelf();
        if(parent!=null) parent.children.Remove(fw);
      }
      l.SetRect(true, bound.tlc-min, bound.brc-min);
      var rect = FloatRect.RelativeTo(fw,Position);
      if(fw.verticalShrink && fw.edges.HasFlag(Edges.top)) rect.expandUp(-1);
      if(fw.verticalShrink && fw.edges.HasFlag(Edges.bottom)) rect.expandDown(-1);
      bounds.Add(rect);
      edges.Add(fw.edges);
    }
    var clipped = EdgeFinder.Find(bounds,edges);
    fills = clipped.Item2;
    foreach(var (seg,loop) in clipped.Item1) surfaces.Add(ParseSurface(seg,loop));

    Collider = new MiptileCollider(new MipGrid(l), Vector2.One){Position=min-Position};
  }
}
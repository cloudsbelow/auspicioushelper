


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
  Color[] rayColors;
  List<FloatRect> fills;//VertexPositionColor[] inner;
  public FancyWater(EntityData d, Vector2 o):base(d.Position+o,false,false,d.Width,d.Height){
    if(d.Bool("hasTop",true))edges|=Edges.top;
    if(d.Bool("hasBottom",false))edges|=Edges.bottom;
    if(d.Bool("hasLeft",false))edges|=Edges.left;
    if(d.Bool("hasRight",false))edges|=Edges.right;
    fillColor = Util.hexToColor(d.Attr("fillColor","293E4B4D"));
    surfaceColor = Util.hexToColor(d.Attr("surfaceColor","6CA5C8CC"));
    TopSurface = new FakeTopsurface(this);
    Collider = new Hitbox(d.Width,d.Height);
    Remove(Get<DisplacementRenderHook>());
    Depth = -8987; 
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
  public override void Update(){
    if(leader!=null) return;
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
    if(leader!=null) return;
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

    if(leader!=null) return;
    List<FloatRect> bounds = new();
    List<Edges> edges = new();
    foreach(FancyWater fw in scene.Tracker.GetEntities<FancyWater>()){
      if(fw!=this) fw.leader=this;
      bounds.Add(FloatRect.RelativeTo(fw,Position));
      edges.Add(fw.edges);
    }
    var thing = EdgeFinder.Find(bounds,edges);
    fills = thing.Item2;
  
    foreach(var (s,l) in thing.Item1) surfaces.Add(ParseSurface(s,l));
  }
  




  






  

  
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/water","auspicioushelper/waterCopy")]
[TrackedAs(typeof(Water))]
[Tracked]
public partial class FancyWater:Water,ISimpleEnt,ConnectedBlocks.ICustomCheckCollider{
  Collider ConnectedBlocks.ICustomCheckCollider.Get => mtc??Collider;
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
  List<FloatRect> fills;
  FloatRect renderBounds;
  float storedTime;
  bool verticalShrink,copy,die,inView=true;
  public bool jumpOut;
  MiptileCollider mtc;
  public FancyWater(EntityData d, Vector2 o):base(d.Position+o,false,false,d.Width,d.Height){
    ResetEvents.Hooks<FancyWater>.enable();
    drag = d.Float("templateDrag",1);
    jumpOut = d.Bool("jumpOutSides",false);
    die = d.Bool("die",false) || d.Name=="auspicioushelper/DieWater";
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
    storedTime+=Engine.DeltaTime;
    if(!inView) return;
    foreach(var surface in surfaces) surface.Update(storedTime);
    storedTime=0;
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
    if(!inView || leader!=null || copy) return;
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
    Int2 dieFac = die? new(1,3):new(0,0);
    foreach(var (bound, fw) in things){
      if(fw!=this){
        fw.leader=this;
        fw.RemoveSelf();
        if(parent!=null) parent.children.Remove(fw);
      }
      l.SetRect(true, bound.tlc-min+dieFac, bound.brc-min-dieFac);
      var rect = FloatRect.RelativeTo(fw,Position);
      if(fw.verticalShrink && fw.edges.HasFlag(Edges.top)) rect.expandUp(-1);
      if(fw.verticalShrink && fw.edges.HasFlag(Edges.bottom)) rect.expandDown(-1);
      bounds.Add(rect);
      edges.Add(fw.edges);
    }
    var clipped = EdgeFinder.Find(bounds,edges);
    fills = clipped.Item2;
    foreach(var (seg,loop) in clipped.Item1) surfaces.Add(ParseSurface(seg,loop));
    
    mtc = new MiptileCollider(new MipGrid(l), Vector2.One){Position=min-Position};
    Collider = die? null:mtc;
    if(die) Add(new PlayerCollider(p=>p.Die(Vector2.Zero),mtc));
    foreach(var s in surfaces) s.Update(0.01f);
    Anti0fZone.SolidAnti0fComp.AddReason(Scene.Tracker.GetEntity<Player>(), "fancyWater", ()=>UpdateHook.cachedPlayer.CollideCheck<FancyWater>());

    renderBounds = FloatRect.fromCorners(min-Position, max-Position);
  }

  static bool checkFancy(Player p)=>p.CollideFirst<FancyWater>()!=null;
  [ResetEvents.ILHook(typeof(Player),nameof(Player.orig_Update))]
  static void TpHook(ILContext ctx){
    ILCursor c = new(ctx);
    c.GotoNext(itr=>itr.MatchCall<Player>(nameof(Player._IsOverWater)));
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchLdcI4(1),
      itr=>itr.MatchLdnull(), itr=>itr.MatchLdnull(),
      itr=>itr.MatchCall<Actor>(nameof(Actor.MoveVExact))
    )){
      c.EmitLdarg0();
      c.EmitDelegate(checkFancy);
      c.EmitOr();
    } else DebugConsole.WriteFailure("Could not add water teleport prevention hook");
  }

  [ResetEvents.OnHook(typeof(Player),nameof(Player.SwimUpdate))]
  static int swimHook(On.Celeste.Player.orig_SwimUpdate orig, Player p){
    var fws = p.CollideAll<FancyWater>();
    if(fws.Count>0){
      var og = p.Collider;
      var nh = Math.Max(og.Height-8,3);
      p.Collider = new Hitbox(1,nh, og.Left, og.Top);
      bool leftFlag = fws.Any(p.CollideCheck);
      p.Collider = new Hitbox(1,nh, og.Left+og.Width-1, og.Top);
      bool rightFlag = fws.Any(p.CollideCheck);
      p.Collider = og;

      if(!(leftFlag && rightFlag) && Input.Jump.Pressed && fws.Any(fw=>((FancyWater)fw).jumpOut)){
        bool can = p.CanUnDuck;
        if((leftFlag || rightFlag) && can){
          int dir = leftFlag? 1:-1;
          if((int)p.Facing*dir<0 && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && p.Stamina>0 && p.Holding==null){
            p.ClimbJump();
          } else p.WallJump(dir);
          var ripplePos = p.Center + Vector2.UnitX*p.Width/2;
          foreach(FancyWater fw in fws) fw.DoRipple(ripplePos,1);
          return Player.StNormal;
        }
      }
    }
    return orig(p);
  }

  [ResetEvents.OnHook(typeof(Player),nameof(Player.SwimCheck))]
  static bool swimCheck(On.Celeste.Player.orig_SwimCheck orig, Player p){
    bool flag = orig(p);
    if(flag && !p.Ducking && p.CollideCheck<FancyWater>()){
      var og = p.Collider;
      p.Collider = new Hitbox(og.Width,og.Height-8, og.Left, og.Top);
      flag &= p.CollideCheck<Water>();
      p.Collider = og;
    }
    return flag;
  } 
}
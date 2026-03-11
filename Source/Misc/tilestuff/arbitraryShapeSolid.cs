


using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using CKey = (string, float, byte, Microsoft.Xna.Framework.Vector2);
namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/ArbitrarySolid")]
[MapenterEv(nameof(precache))]
public class ArbitraryShapeSolid:Solid{
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  static Dictionary<CKey, (Vector2, MipGrid, TileOccluder)> cache = new();
  MTexture imag;
  float rot;
  Vector2 scale;
  Color col;
  static Vector2 getScale(EntityData d){
    return new(
      d.Float("scaleX",d.Bool("flipH",false)?-1:1), 
      d.Float("scaleY",d.Bool("flipV",false)?-1:1)
    );
  }
  public ArbitraryShapeSolid(EntityData d, Vector2 o):base(d.Position+o,0,0,d.Bool("safe")){
    scale = getScale(d);
    rot = Calc.DegToRad*d.Float("rotation",0);
    Collider = MakeCollider(makeKey(d), out var occluder);
    if(d.Bool("occludeLight",true)) Add(new TileOccluder(occluder));
    Depth  = (int)d.Float("depth",-100);
    if(d.tryGetStr("image", out var str)){
      imag = GetTextureAtPath(str);
    } else Visible =false;
    col = Util.hexToColor(d.Attr("color","fff"));
    SurfaceSoundIndex = d.Int("soundIndex",8);
  }
  static MTexture GetTextureAtPath(string path){
    if(GFX.Game.textures.ContainsKey(path)) return GFX.Game[path];
    else return GFX.Game.GetAtlasSubtextureFromAtlasAt(path, 0);
  }
  static Collider precache(EntityData d)=>MakeCollider(makeKey(d), out var _);
  static CKey makeKey(EntityData d){
    string path = d.Attr("CustomColliderPath","");
    if(string.IsNullOrWhiteSpace(path)) path = d.Attr("image");
    return new(
      path, Calc.DegToRad*d.Float("rotation",0), 
      (byte)Math.Clamp((int)(d.Float("alphaCutoff",0.5f)*255),0,255), 
      getScale(d)
    );
  }
  static Collider MakeCollider(CKey path, out TileOccluder occluder){
    if(cache.TryGetValue(path,out var cmg)){
      occluder=cmg.Item3;
      return new MiptileCollider(cmg.Item2,Vector2.One){Position=cmg.Item1};
    } else {
      MTexture tex = GetTextureAtPath(path.Item1);
      Vector2 textlc = (tex.DrawOffset-tex.Center)/tex.ScaleFix;
      var f = new FloatRect(textlc.X,textlc.Y,tex.ClipRect.Width,tex.ClipRect.Height);
      List<Vector2> corners = new(){f.tlc,f.trc,f.blc,f.brc};
      corners.MapInplace(x=>(x*path.Item4).Rotate(path.Item2));
      
      Int2 tlc = Int2.Floor(corners.Reduce(Vector2.Min, Vector2.One*float.PositiveInfinity));
      Int2 brc = Int2.Ceil(corners.Reduce(Vector2.Max, Vector2.One*float.NegativeInfinity));

      bool[] dat = Util.TexData(tex, out int texw, out int texh).Map(x=>x.A>=path.Item3);
      
      MipGrid.Layer l = new((brc.x-tlc.x+8-1)/8, (brc.y-tlc.y+8-1)/8);
      for(int x=tlc.x; x<brc.x; x++) for(int y=tlc.y; y<brc.y; y++){
        Vector2 loc = (new Vector2(x,y)+Vector2.One/2).Rotate(-path.Item2)/path.Item4-textlc;
        if(Util.InterpolateNearest(dat, texw, loc, Util.CpuEdgeSampleMode.defaul)){
          l.SetPoint(true, new Int2(x,y)-tlc);
        }
      } 
      MipGrid m=new(l);
      var t=new TileOccluder(Vector2.One,tlc).Build(l);
      cache.Add(path, new(tlc, m, occluder = t));
      return new MiptileCollider(m,Vector2.One){Position = tlc};
    }
  }
  public override void Render() {
    base.Render();
    if(imag == null) Visible=false;
    else imag.DrawCentered(Position, col, scale, rot);
  }
  public override void MoveHExact(int move) {
    GetRiders();
    Player player = UpdateHook.cachedPlayer;
    //What the heck does this do?
    if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && 
    !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY)){
      player.MoveV(1f);
    }

    base.X += move;
    MoveStaticMovers(Vector2.UnitX * move);
    if(!Collidable) return;
    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()){
      if (!entity.AllowPushing) continue;
      bool collidable = entity.Collidable;
      entity.Collidable = true;
      if (!entity.TreatNaive && CollideCheck(entity, Position)){
        Collidable = false;
        for(int i=0; i<Math.Abs(move); i++){
          if(!CollideCheck(entity, Position) || entity.MoveHExact(Math.Sign(move), entity.SquishCallback, this)) break;
        }
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      } else if (riders.Contains(entity)) {
        Collidable = false;
        if (entity.TreatNaive) entity.NaiveMove(Vector2.UnitX * move);
        else entity.MoveHExact(move);
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      }
      entity.Collidable = collidable;
    }  
    riders.Clear();
  }
  public override void MoveVExact(int move) {
    GetRiders();
    base.Y += move;
    MoveStaticMovers(Vector2.UnitY * move);
    if(!Collidable) return;
    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()){
      if (!entity.AllowPushing) continue;
      bool collidable = entity.Collidable;
      entity.Collidable = true;
      if (!entity.TreatNaive && CollideCheck(entity, Position)){
        Collidable = false;
        for(int i=0; i<Math.Abs(move); i++){
          if(!CollideCheck(entity, Position) || entity.MoveVExact(Math.Sign(move), entity.SquishCallback, this)) break;
        }
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      } else if (riders.Contains(entity)){
        Collidable = false;
        if (entity.TreatNaive) entity.NaiveMove(Vector2.UnitY * move);
        else entity.MoveVExact(move);
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      }
      entity.Collidable = collidable;
    }
    riders.Clear();
  }


  [CustomEntity("auspicioushelper/ArbitraryDie")]
  [MapenterEv(nameof(precache))]
  public class ArbitraryShapeKillbox:Entity{
    MTexture imag;
    float rot;
    Vector2 scale;
    Color col;
    static Collider precache(EntityData d)=>MakeCollider(makeKey(d),out var _);
    public ArbitraryShapeKillbox(EntityData d, Vector2 o):base(d.Position+o){
      scale = new Vector2(d.Bool("flipH",false)?-1:1,d.Bool("flipV",false)?-1:1);
      rot = Calc.DegToRad*d.Float("rotation",0);
      Collider = MakeCollider(makeKey(d), out var _);
      Depth  = (int)d.Float("depth",-100);
      if(d.tryGetStr("image", out var str)){
        imag = GetTextureAtPath(str);
      } else Visible =false;
      col = Util.hexToColor(d.Attr("color","fff"));
      Add(new PlayerCollider((p)=>p.Die(Vector2.Zero)));
    }
    public override void Render() {
      base.Render();
      if(imag == null) Visible=false;
      else imag.DrawCentered(Position, col, scale, rot);
    }
  }
}

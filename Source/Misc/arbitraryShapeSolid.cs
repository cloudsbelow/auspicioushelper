


using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using CKey = (string, float, byte, bool, bool);
namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/ArbitrarySolid")]
public class ArbitraryShapeSolid:Solid{
  
  static Dictionary<CKey, (Vector2, MipGrid)> cache = new();
  static ArbitraryShapeSolid(){
    auspicioushelperModule.OnEnterMap.enroll(new PersistantAction(cache.Clear));
  }
  MTexture imag;
  float rot;
  Vector2 scale;
  Color col;
  public ArbitraryShapeSolid(EntityData d, Vector2 o):base(d.Position+o,0,0,d.Bool("safe")){
    string path = d.Attr("CustomColliderPath","");
    if(string.IsNullOrWhiteSpace(path)) path = d.Attr("image");
    CKey k = new(
      path, d.Float("rotation",0), 
      (byte)Math.Clamp((int)(d.Float("alphaCutoff",0.5f)*255),0,255), 
      d.Bool("flipH",false), d.Bool("flipV",false)
    );
    scale = new Vector2(d.Bool("flipH",false)?-1:1,d.Bool("flipV",false)?-1:1);
    rot = Calc.DegToRad*d.Float("rotation",0);
    MakeCollider(k);
    Depth  = (int)d.Float("depth",-100);
    if(d.tryGetStr("image", out var str)){
      imag = GetTextureAtPath(str);
    } else Visible =false;
    col = Util.hexToColor(d.Attr("color","fff"));
  }
  MTexture GetTextureAtPath(string path){
    if(GFX.Game.textures.ContainsKey(path)) return GFX.Game[path];
    else return GFX.Game.GetAtlasSubtextureFromAtlasAt(path, 0);
  }
  void MakeCollider(CKey path){
    if(cache.TryGetValue(path,out var cmg)){
      Collider = new MiptileCollider(cmg.Item2,Vector2.One);
      Collider.Position = cmg.Item1;
    } else {
      MTexture tex = GetTextureAtPath(path.Item1);
      Vector2 tlc = (tex.DrawOffset-tex.Center)/tex.ScaleFix;
      var f = new FloatRect(tlc.X,tlc.Y,tex.ClipRect.Width,tex.ClipRect.Height)._flip(path.Item4,path.Item5);
      bool[] dat = Util.TexData(tex, out int w, out int h).Map(x=>x.A>=path.Item3).Flip(w,path.Item4,path.Item5);
      if(path.Item2!=0){
        int amt = (int)Math.Round(path.Item2/90);
        dat = dat.Rotate(4-amt, ref w, ref h);
        f = f._rot90By(amt);
        // string str = "";
        // for(int y=0; y<h; y++){
        //   for(int x=0; x<w; x++){
        //     str+=dat[y*w+x]?'X':' ';
        //   }
        //   str+="\n";
        // }
        // DebugConsole.Write(str,f);
      }
      int gw = (w+8-1)/8;
      int gh = (h+8-1)/8;
      MipGrid.Layer l = new(gw, gh);
      for(int bx=0; bx<gw; bx++) for(int by=0; by<gh; by++){
        ulong block = 0;
        ulong num = 1;
        for(int ty=0; ty<8; ty++) for(int tx=0; tx<8; tx++){
          int x = bx*8+tx; int y = by*8+ty;
          if(x<w && y<h && dat[x+w*y]) block|=num;
          num<<=1;
        }
        l.SetBlock(block,bx,by);
      }
      MipGrid m = new(l);
      Collider = new MiptileCollider(m,Vector2.One);
      cache.Add(path, new(Collider.Position = f.tlc, m));
    }
    
  }
  public override void Render() {
    base.Render();
    //imag.DrawOffset=Vector2.Zero;
    if(imag == null) Visible=false;
    else imag.DrawCentered(Position, col, scale, rot);
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
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
}

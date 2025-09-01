




using System;
using System.Runtime.CompilerServices;
using Celeste.Editor;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

public sealed class MiptileCollider:Grid{
  public MipGrid mg;
  Vector2 cellsize;
  // Left/right/top/bottom all use width and height
  public override float Width { get => cellsize.X*cx; }
  public override float Height { get => cellsize.Y*cy; }
  public Vector2 tlc=>(lastentity?.Position??Vector2.Zero+Position).Round();
  public Int2 itlc=>Int2.Round(lastentity?.Position??Vector2.Zero+Position);
  int cx;
  int cy;
  public void SetMg(MipGrid m){
    mg=m;
    cx=m.width;
    cy=m.height;
  }
  public MiptileCollider(MipGrid m, Vector2 cs):base(0,0,cs.X,cs.Y){
    SetMg(m);
    cellsize = cs;
  }
  public MiptileCollider(Grid g):base(g.CellWidth,g.CellHeight,g.Data){
    SetMg(new(g));
    cellsize = new Vector2(g.CellWidth,g.CellHeight);
  }
  public override Collider Clone() {
    return new MiptileCollider(mg,cellsize);
  }
  public override bool Collide(Circle circle)=>false; //so does vanilla...
  public override bool Collide(Grid grid) {
    if(grid is MiptileCollider o){
      if(o.cellsize!=cellsize) throw new NotImplementedException("Miptile grids must have same cellsize to collide");
      return mg.collideGridSameCs(o.mg,(o.tlc-tlc)/cellsize);
    } else {
      throw new NotImplementedException();
    }
  }
  public bool CollideMipTileOffset(MiptileCollider o, Vector2 offset){
    if(o.cellsize!=cellsize) throw new NotImplementedException("Miptile grids must have same cellsize to collide");
    return mg.collideGridSameCs(o.mg,(o.tlc+offset-tlc)/cellsize);
  }
  public override bool Collide(ColliderList list)=>list.Collide(this);
  public override bool Collide(Hitbox hitbox){
    Vector2 pos = hitbox.AbsolutePosition;
    return collideFr(new(pos.X,pos.Y,hitbox.width,hitbox.height));
  }
  public override bool Collide(Rectangle rect)=>collideFr(new FloatRect(rect));
  public bool collideFr(FloatRect f){
    Vector2 ltlc = tlc;
    Int2 rtlc = Int2.Floor((f.tlc.Round()-ltlc)/cellsize);
    Int2 rbrc = Int2.Ceil((f.brc.Round()-ltlc)/cellsize);
    //DebugConsole.Write("collis", rtlc, rbrc, rtlc/8, rbrc/8);
    return mg.collideInFrame(IntRect.fromCorners(rtlc,rbrc));
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool collideFrOffset(FloatRect f, Vector2 offset)=>collideFr(new FloatRect(f.x+offset.X,f.y+offset.Y,f.w,f.h));
  public override bool Collide(Vector2 from, Vector2 to) {
    throw new NotImplementedException("");
  }
  public override bool Collide(Vector2 point) {
    return mg.collidePoint(Int2.Floor((point-tlc)/cellsize));
  }
  Entity lastentity;
  public override void Added(Entity entity) {
    lastentity=entity;
    base.Added(entity);
  }
  public static MiptileCollider fromGrid(Grid g){
    if(g is MiptileCollider gr) return gr;
    DynamicData d = new(g);
    if(!d.TryGet<MiptileCollider>("ausp_mipgrid", out var grid)){
      if(PartialTiles.usingPartialtiles){
        throw new Exception($"Partial tiles are enabled: Tile collider should be a MiptileCollider but is a {grid.GetType()}");
      }
      d.Set("ausp_mipgrid", grid = new MiptileCollider(g));
      grid.lastentity = g.Entity;
    }
    return grid;
  }
}
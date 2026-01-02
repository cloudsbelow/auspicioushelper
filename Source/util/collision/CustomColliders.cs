




using System;
using System.Runtime.CompilerServices;
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
  public Vector2 tlc=>((lastentity?.Position??Vector2.Zero)+Position).Round();
  public Int2 itlc=>Int2.Round((lastentity?.Position??Vector2.Zero)+Position);
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
  public MiptileCollider(MipGrid.Layer l, Vector2 cs, Vector2 tlc, bool skipMips=false):base(0,0,cs.X,cs.Y){
    SetMg(new MipGrid(l,skipMips));
    cellsize = cs;
    Position = tlc;
  }
  public MiptileCollider(Grid g):base(g.CellWidth,g.CellHeight,g.Data){
    SetMg(new(g));
    cellsize = new Vector2(g.CellWidth,g.CellHeight);
  }
  public override Collider Clone() {
    return new MiptileCollider(mg,cellsize);
  }
  //cellsize better be square...
  public override bool Collide(Circle circle){
    return mg.CollideCircle((circle.AbsolutePosition-tlc)/cellsize,circle.Radius/cellsize.X);
  }
  public override bool Collide(Grid grid) {
    if(grid is MiptileCollider o){
      return CollideMipTileOffset(o,Vector2.Zero);
    } else {
      throw new NotImplementedException();
    }
  }
  public bool CollideMipTileOffset(MiptileCollider o, Vector2 offset){
    if(o.cellsize!=cellsize){
      if(o.cellsize.X>cellsize.X) return o.CollideMipTileOffset(this, -offset);
      if(o.cellsize*8 == cellsize){
        return mg.collideGridLowCs(o.mg,(o.tlc+offset-tlc)/cellsize);
      } else throw new Exception("Miptile collider must be either same cell size or one apart");
    }
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
    // DebugConsole.Write("collis", ltlc, rtlc, rbrc, rtlc/8, rbrc/8);
    return mg.collideInFrame(IntRect.fromCorners(rtlc,rbrc));
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool collideFrOffset(FloatRect f, Vector2 offset)=>collideFr(new FloatRect(f.x+offset.X,f.y+offset.Y,f.w,f.h));
  public override bool Collide(Vector2 from, Vector2 to) {
    //TODO: use mips
    return base.Collide(from,to);
  }
  public override bool Collide(Vector2 point) {
    return mg.collidePoint(Int2.Floor((point-tlc)/cellsize));
  }
  internal Entity lastentity;
  public override void Added(Entity entity) {
    lastentity=entity;
    base.Added(entity);
  }
  public static MiptileCollider fromGrid(Grid g){
    if(g is MiptileCollider gr) return gr;
    if(g is DumbGridWrapper gri) return gri.mtc;
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
  public override void Render(Camera camera, Color color) {
    Draw.HollowRect(AbsoluteX, AbsoluteY, Width, Height, color);
  }
}


class DumbGridWrapper:Grid{
  public MiptileCollider mtc;
  MipGrid mg;
  Vector2 cellsize;
  public Vector2 tlc=>((Entity?.Position??Vector2.Zero)+Position).Round();
  public DumbGridWrapper(Grid g, MipGrid mg):base(g.CellWidth,g.CellHeight,g.Data){
    this.mg=mg;
    this.mtc=new(mg,cellsize=new(CellWidth,CellHeight));
  }
  public override void Added(Entity entity) {
    base.Added(entity);
    mtc.lastentity = entity;
  }
  public override bool Collide(Vector2 point){
    Vector2 vector = point - base.AbsolutePosition;
    if(vector.X<0 || vector.Y<0) return false;
    return Data[(int)(vector.X / CellWidth), (int)(vector.Y / CellHeight)];
  }
  public override bool Collide(Circle c){
    return mg.CollideCircle((c.AbsolutePosition-tlc)/cellsize,c.Radius/cellsize.X);
  }
}

public class DelegatingPointcollider:ColliderList{
  public interface CustomPointCollision{
    bool Collide(ColliderList l);
  }
  public override float Top=>0;
  public override float Left=>0;
  public override float Width=>0;
  public override float Height=>0;
  public override float Right=>0;
  public override float Bottom=>0;
  public override bool Collide(Circle circle)=>false;
  public override bool Collide(Grid grid)=>false;
  public override bool Collide(Hitbox hitbox)=>false;
  public override bool Collide(Rectangle rect)=>false;
  public override bool Collide(Vector2 from, Vector2 to)=>false;
  public override bool Collide(Vector2 point)=>false;
  public override bool Collide(ColliderList list)=>list is CustomPointCollision pch? pch.Collide(this):false;
}
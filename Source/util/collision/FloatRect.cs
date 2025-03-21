


using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public struct FloatRect{
  public float x;
  public float y;
  public float w;
  public float h;
  public Vector2 tlc{
    get=>new Vector2(x,y);
  }
  public Vector2 brc{
    get=>new Vector2(x+w,y+h);
  }
  public Vector2 center {
    get=>new Vector2(x+w/2,y+h/2);
  }
  public Vector2 radius{
    get=>new Vector2(w/2,h/2);
  }
  public FloatRect(float x,float y, float w, float h){
    this.x=x; this.y=y; this.w=w; this.h=h;
  }
  public FloatRect(Entity e){
    x=e.Left; y=e.Top; w=e.Width; h=e.Height;
  }
  public bool CollidePoint(Vector2 p){
    return p.X>=x && p.Y>=y && p.X<x+w && p.Y<y+h;
  }
  public bool CollideLine(Vector2 a, Vector2 b){
    Vector2 dir = b-a;
    Vector2 t1 = tlc/dir;
    Vector2 t2 = brc/dir;
    Vector2 maxs = Vector2.Max(t1,t2);
    Vector2 mins = Vector2.Min(t1,t2);
    float min = Math.Max(mins.X,mins.Y);
    float max = Math.Min(maxs.X,maxs.Y);
    return min<max && (min<1 || max>0);
  }
  public bool CollideCircle(Vector2 p, float r){
    p=p-center;
    Vector2 d=Vector2.Max(Vector2.Zero,new Vector2(Math.Abs(p.X)-w,Math.Abs(p.Y)-h));
    return d.X*d.X+d.Y*d.Y<r*r;
  }
  public bool CollideExRect(float ox, float oy, float ow, float oh){
    return x+w>ox && y+h>oy && x<ox+ow && y<oy+oh;
  }
  public Rectangle munane(){
    return new Rectangle((int) x, (int) y, (int) w, (int) h);
  }
  public override string ToString(){
      return "FloatRect:{"+string.Format("x:{0}, y:{1}, w:{2}, h:{3}",x,y,w,h)+"} ";
  }
}
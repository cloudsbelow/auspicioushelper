


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


public struct FloatRect{
  public struct FRCollision{
    public float enter = float.PositiveInfinity;
    public float exit = float.NegativeInfinity;
    public FRCollision(){}
    public FRCollision(float t1, float t2){
      enter = t1; exit=t2;
    }
    public bool collides=>enter<exit && exit>0;
    public bool collidesOne=>enter<exit && exit>0 && enter<=1;
    public void union(FRCollision o){
      enter = MathF.Min(enter,o.enter);
      exit = MathF.Max(exit,o.exit);
    }
    public FRCollision union(params FRCollision[] a){
      FRCollision f=a[0];
      for(int i=1; i<a.Length; i++){
        f.union(a[i]);
      }
      return f;
    }
    public override string ToString(){
      return $"FRCol: {{{enter} {exit} {collides}}}";
    }
  }

  public float x;
  public float y;
  public float w;
  public float h;
  public float left=>x;
  public float right=>x+w;
  public float top=>y;
  public float bottom=>y+h;
  public Vector2 tlc=>new Vector2(x,y);
  public Vector2 brc=>new Vector2(x+w,y+h);
  public Vector2 trc=>new Vector2(x+w,y);
  public Vector2 blc=>new Vector2(x,y+h);
  public Vector2 size=>new Vector2(w,h);
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
  public FloatRect(Rectangle r){
    x=r.X; y=r.Y; w=r.Width; h=r.Height;
  }
  public static FloatRect fromCorners(Vector2 tlc, Vector2 brc){
    return new FloatRect(tlc.X,tlc.Y,brc.X-tlc.X,brc.Y-tlc.Y);
  }
  public static FloatRect fromCornersUnordered(Vector2 one, Vector2 two){
    FloatRect r = new FloatRect(Math.Min(one.X,two.X),Math.Min(one.Y,two.Y),Math.Abs(one.X-two.X),Math.Abs(one.Y-two.Y));
    return r;
  }
  public static FloatRect fromRadius(Vector2 center, Vector2 radius){
    return new FloatRect(center.X-radius.X,center.Y-radius.Y,radius.X*2,radius.Y*2);
  }
  public void shift(Vector2 shift){
    x+=shift.X; y+=shift.Y;
  }
  public void expandXto(float loc){
    if(loc<x){
      w+=x-loc;
      x=loc;
    }
    if(loc>x+w){
      w+=loc-(x+w);
    }
  }
  public void expandYto(float loc){
    if(loc<y){
      h+=y-loc;
      y=loc;
    }
    if(loc>y+h){
      h+=loc-(y+h);
    }
  }
  public void invertY(){
    y=y+h;
    h=-h;
  }
  public bool CollidePoint(Vector2 p){
    return p.X>x && p.Y>y && p.X<x+w && p.Y<y+h;
  }
  public bool CollidePointCompact(Vector2 p){
    return p.X>=x && p.Y>=y && p.X<=x+w && p.Y<=y+h;
  }
  public bool CollideRay(Vector2 p, Vector2 dir){
    Vector2 t1 = (tlc-p)/dir;
    Vector2 t2 = (brc-p)/dir;
    Vector2 maxs = Vector2.Max(t1,t2);
    Vector2 mins = Vector2.Min(t1,t2);
    float min = Math.Max(mins.X,mins.Y);
    float max = Math.Min(maxs.X,maxs.Y);
    return min<max && min<1 && max>0;
  }
  public FRCollision ICollideRay(Vector2 p, Vector2 dir){
    Vector2 t1 = (tlc-p)/dir;
    Vector2 t2 = (brc-p)/dir;
    Vector2 maxs = Vector2.Max(t1,t2);
    Vector2 mins = Vector2.Min(t1,t2);
    float min = Math.Max(mins.X,mins.Y);
    float max = Math.Min(maxs.X,maxs.Y);
    return new FRCollision(min,max);
  }
  public bool CollideLine(Vector2 a, Vector2 b){
    Vector2 dir = b-a;
    return CollideRay(a,dir);
  }
  public bool CollideCircle(Vector2 p, float r){
    p=p-center;
    Vector2 d=Vector2.Max(Vector2.Zero,new Vector2(Math.Abs(p.X)-w,Math.Abs(p.Y)-h));
    return d.X*d.X+d.Y*d.Y<r*r;
  }
  public bool CollideExRect(float ox, float oy, float ow, float oh){
    return x+w>ox && y+h>oy && x<ox+ow && y<oy+oh;
  }
  public bool CollideFr(FloatRect other){
    return CollideExRect(other.x,other.y,other.w,other.h);
  }
  public bool CollideRectSweep(FloatRect o, Vector2 sweep){
    FloatRect expanded = new FloatRect(x-o.w,y-o.h,w+o.w,h+o.h);
    return expanded.CollideRay(o.tlc,sweep);
  }
  public FRCollision ICollideRectSweep(FloatRect o, Vector2 sweep){
    FloatRect expanded = new FloatRect(x-o.w,y-o.h,w+o.w,h+o.h);
    return expanded.ICollideRay(o.tlc,sweep);
  }
  FRCollision ccircle(float r, Vector2 p, Vector2 dir){
    FRCollision f=new FRCollision();
    var c=Vector2.Dot(p,dir);
    var d=p.X*p.X+p.Y*p.Y-r*r;
    var a=dir.X*dir.X+dir.Y*dir.Y;
    var det = c*c-d*a;
    if(det<0) return f;
    var l = MathF.Sqrt(det);
    f.enter = (-c-l)/a;
    f.exit = (-c+l)/a;
    return f;
  }
  public FRCollision ICollideCircleSweep(float r, Vector2 p, Vector2 sweep, float m=float.PositiveInfinity){
    FloatRect a = _expand(r,r);
    FRCollision f = a.ICollideRay(p,sweep);
    if(!f.collides || f.enter>m) return f;
    f=_expandX(r).ICollideRay(p,sweep);
    f.union(_expandY(r).ICollideRay(p,sweep));
    f.union(ccircle(r,p-tlc,sweep));
    f.union(ccircle(r,p-trc,sweep));
    f.union(ccircle(r,p-blc,sweep));
    f.union(ccircle(r,p-brc,sweep));
    return f;
  }
  public bool CollideCircleSweep(float r, Vector2 p, Vector2 sweep){
    return ICollideCircleSweep(r,p,sweep,1).collidesOne;
  }

  //this function is NOT TIGHT for grids; The returned collision will always be a superset of actual collision
  public FRCollision ISweep(Collider c, Vector2 s){
    if(c is null) return new FRCollision();
    if(c is Hitbox h || c is Grid g){
      return ICollideRectSweep(new FloatRect(c.AbsoluteLeft,c.AbsoluteTop,c.Width,c.Height),s);
    }
    if(c is Circle r){
      return ICollideCircleSweep(r.Radius, r.AbsolutePosition, s);
    }
    if(c is ColliderList l){
      var f = new FRCollision();
      foreach(var cl in l.colliders){
        f.union(ISweep(cl,s));
      }
      return f;
    }
    DebugConsole.Write($"Unimplemented collider type {c} on {c?.Entity}");
    return new FRCollision();
  }
  public bool CollideCollider(Collider c){
    if(c is Hitbox h){
      h.Collide(munane());
    } else if(c is Circle cir){
      return CollideCircle(cir.AbsolutePosition,cir.Radius);
    } else if(c is Grid r){
      r.Collide(munane());
    } else if(c is ColliderList l){
      foreach(Collider i in l.colliders){
        if(CollideCollider(i)) return true;
      }  
      return false;
    }else {
      DebugConsole.Write("Forgor to implement floatrect colliding for "+c.ToString());
      throw new NotImplementedException();
    }
    return false;
  }
  public Entity CollideFirst(List<Entity> li){
    foreach(Entity e in li){
      if(e.Collidable && CollideCollider(e.Collider)) return e;
    }
    return null;
  }
  public bool CollideEntitylist(List<Entity> e){
    return CollideFirst(e)!=null;
  }
  public Rectangle munane(){
    return new Rectangle((int) Math.Round(x), (int) Math.Round(y), (int) w, (int) h);
  }
  public override string ToString(){
      return "FloatRect:{"+string.Format("x:{0}, y:{1}, w:{2}, h:{3}",x,y,w,h)+"} ";
  }
  FloatRect copy(){
    return new FloatRect(x,y,w,h);
  }
  public void expandAll(float a){
    x-=a; y-=a; w+=a*2; h+=a*2;
  }
  public void expandLeft(float a){
    x-=a; w+=a;
  }
  public FloatRect expandLeft_(float a)=>new FloatRect(x-a,y,w+a,h);
  public void expandRight(float a)=>w+=a;
  public FloatRect expandRight_(float a)=>new FloatRect(x,y,w+a,h);
  public void expandH(float a){
    if(a<0) expandLeft(-a);
    else expandRight(a);
  }
  public FloatRect expandH_(float a){
    var f = copy();
    f.expandH(a);
    return f;
  }
  public void expandUp(float a){
    y-=a; h+=a;
  }
  public FloatRect expandUp_(float a)=>new FloatRect(x,y-a,w,h+a);
  public void expandDown(float a)=>h+=a;
  public FloatRect expandDown_(float a)=>new FloatRect(x,y,w,h+a);
  public void expandV(float a){
    if(a<0) expandUp(-a);
    else expandDown(a);
  }
  public FloatRect expandV_(float a){
    var f = copy();
    f.expandV(a);
    return f;
  }
  public bool IsInside(FloatRect o){
    return x>=o.x && x+w<=o.x+o.w && y>=o.y && y+h<=o.y+o.h;
  }
  public FloatRect _intersect(FloatRect o){
    var c1 = Vector2.Max(tlc,o.tlc);
    var c2 = Vector2.Min(brc, o.brc);
    return fromCorners(c1,c2);
  }
  public FloatRect _expandX(float amount){
    return new FloatRect(x-amount, y, w+amount*2, h);
  }
  public FloatRect _expandY(float amount){
    return new FloatRect(x, y-amount, w, h+2*amount);
  }
  public FloatRect _expand(float xe, float ye){
    return new FloatRect(x-xe,y-ye,w+xe*2,h+ye*2);
  }
  public static FloatRect full=> new FloatRect(-1000000000,-1000000000,2000000000,2000000000);
  public static FloatRect empty=>new FloatRect(1000000000,1000000000,-2000000000,-2000000000);
  public FloatRect _union(FloatRect f){
    var c1 = Vector2.Min(tlc,f.tlc);
    var c2 = Vector2.Max(brc,f.brc);
    return fromCorners(c1,c2);
  }
  public FloatRect _rot90By(int amt){
    amt=Util.SafeMod(amt,4);
    FloatRect f = amt>=2?fromCorners(-brc, -tlc):copy();
    if(amt%2!=0){
      return fromCorners(new(-f.blc.Y,f.blc.X),new(-f.trc.Y,f.trc.X));
    }
    return f;
  }
  public FloatRect _flip(bool h, bool v){
    Vector2 flip = new(h?-1:1,v?-1:1);
    return fromCornersUnordered(tlc*flip,brc*flip);
  }
}
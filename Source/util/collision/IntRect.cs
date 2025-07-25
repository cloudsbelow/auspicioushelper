






using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


public struct IntRect{
  public int x;
  public int y;
  public int w;
  public int h;
  public int left=>x;
  public int right=>x+w;
  public int top=>y;
  public int bottom=>y+h;
  public Int2 tlc=>new Int2(x,y);
  public Int2 brc=>new Int2(x+w,y+h);
  public Int2 trc=>new Int2(x+w,y);
  public Int2 blc=>new Int2(x,y+h);
  public Vector2 center {
    get=>new Vector2(x+w/2,y+h/2);
  }
  public Vector2 radius{
    get=>new Vector2(w/2,h/2);
  }
  public IntRect(int x,int y, int w, int h){
    this.x=x; this.y=y; this.w=w; this.h=h;
  }
  public IntRect(Entity e){
    x=(int)Math.Round(e.Left); y=(int)Math.Round(e.Top); 
    w=(int)Math.Round(e.Width); h=(int)Math.Round(e.Height);
  }
  public IntRect(Rectangle r){
    x=r.X; y=r.Y; w=r.Width; h=r.Height;
  }
  public static IntRect fromCorners(Int2 tlc, Int2 brc){
    return new IntRect(tlc.x,tlc.y,brc.x-tlc.x,brc.y-tlc.y);
  }
  public bool CollidePoint(Vector2 p){
    return p.X>x && p.Y>y && p.X<x+w && p.Y<y+h;
  }
  public bool CollidePoint(Int2 p){
    return p.x>x && p.y>y && p.x<x+w && p.y<y+h;
  }
  public bool CollidePointExpand(Int2 p, int e){
    return p.x>x-e && p.y>y-e && p.x<x+w+e && p.y<y+h+e;
  }
  public bool CollideExRect(float ox, float oy, float ow, float oh){
    return x+w>ox && y+h>oy && x<ox+ow && y<oy+oh;
  }
  public bool CollideExRect(int ox, int oy, int ow, int oh){
    return x+w>ox && y+h>oy && x<ox+ow && y<oy+oh;
  }
  public bool CollideFr(FloatRect other){
    return CollideExRect(other.x,other.y,other.w,other.h);
  }
  public bool CollideIr(IntRect other){
    return CollideExRect(other.x,other.y,other.w,other.h);
  }
  public static IntRect fromCenter(Int2 center, Int2 radius){
    return new IntRect(center.x-radius.x, center.y+radius.y, radius.x*2, radius.y*2);
  }
  public static IntRect fromCenter(Int2 center, int radius){
    return new IntRect(center.x-radius, center.y+radius, radius*2, radius*2);
  }
  public static implicit operator IntRect(Rectangle r){
    return new(r.X,r.Y,r.Width,r.Height);
  }
  public bool CollideCenter(Int2 center, Int2 radius){
    if(x<center.x+radius.x && y<center.y+radius.y){
      if(x+w>center.x-radius.x && y+h>center.y-radius.y) return true;
    }
    return false;
  }

  public override string ToString(){
      return "IntRect:{"+string.Format("x:{0}, y:{1}, w:{2}, h:{3}",x,y,w,h)+"} ";
  }
  public IntRect copy(){
    return new(x,y,w,h);
  }
  public void expandAll(int a){
    x-=a; y-=a; w+=a*2; h+=a*2;
  }
  public IntRect expandAll_(int a)=>new(x-a,y-a,w+a*2,h+a*2);
  public void expandLeft(int a){
    x-=a; w+=a;
  }
  public IntRect expandLeft_(int a)=>new IntRect(x-a,y,w+a,h);
  public void expandRight(int a)=>w+=a;
  public IntRect expandRight_(int a)=>new IntRect(x,y,w+a,h);
  public void expandH(int a){
    if(a<0) expandLeft(-a);
    else expandRight(a);
  }
  public IntRect expandH_(int a){
    var f = copy();
    f.expandH(a);
    return f;
  }
  public void expandUp(int a){
    y-=a; h+=a;
  }
  public IntRect expandUp_(int a)=>new IntRect(x,y-a,w,h+a);
  public void expandDown(int a)=>h+=a;
  public IntRect expandDown_(int a)=>new IntRect(x,y,w,h+a);
  public void expandV(int a){
    if(a<0) expandUp(-a);
    else expandDown(a);
  }
  public IntRect expandV_(int a){
    var f = copy();
    f.expandV(a);
    return f;
  }
}
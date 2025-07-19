


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[Flags]
public enum CollisionDirection{
  none=0, up=1, right=2, down=4, left=8, yes = 16, solid=31,
}
public static class SolidMiptree{
  const float bsize = 16;
  const float margin = 4;
  const int mipStep = 1; // each size is 1<<mipStep times larger
  struct RectCollider{
    public IntRect f;
    public CollisionDirection dir;
    public Entity e;
  }
  static Vector2 offset;
  class CollisionLevel{
    int stride=0;
    int height=0;
    float cellsize;
    List<RectCollider>[] buckets;
    bool hasContents = false;
    public void Clear(){
      if(!hasContents) return;
      hasContents = false;
      for(int i=0; i<stride*height; i++) buckets[i] = null;
    }
    public CollisionLevel(int w, int h, int level){
      cellsize = bsize*(1<<(level*mipStep));
      stride = w;
      height = h;
      buckets = new List<RectCollider>[w*h];
    }
    public void Add(RectCollider r){
      var low = Int2.Max(Int2.Floor((r.f.tlc-offset-new Vector2(margin,margin))/cellsize),0);
      var high = Int2.Ceil((r.f.brc-offset+new Vector2(margin,margin))/cellsize);
      high = Int2.Min(high, new Int2(stride-1, height-1));
      for(int y = low.y; y<=low.y; y++){
        for(int x = low.x; x<=high.x; x++){
          if(buckets[stride*y+x]==null)buckets[stride*y+x]=new();
          buckets[stride*y+x].Add(r);
        }
      }
      hasContents = true;
    }
    //Note that this function is only correct if radius<=margin. Ensure this!
    public Entity GetFirstRadius(Int2 center, Int2 radius, CollisionDirection dir){
      var l = buckets[stride*center.y+center.x];
      if(l == null) return null;
      foreach(var r in l) if((r.dir&dir)!=0){
        if(r.f.x<center.x+radius.x && r.f.x+r.f.w>center.x-radius.x){
          if(r.f.y<center.y+radius.y && r.f.y+r.f.h>center.y-radius.y){
            switch(r.e.Collider){
              case Hitbox: return r.e;
              case Circle c:
                Vector2 cent = c.AbsolutePosition-center;
                Vector2 d=Vector2.Max(Vector2.Zero,new Vector2(Math.Abs(cent.X)-radius.x,Math.Abs(cent.Y)-radius.y));
                if(d.X*d.X+d.Y*d.Y<c.Radius*c.Radius) return r.e;
                continue;
              case Grid g:
                var offset = center-radius-r.f.tlc;
                var offsetbrc = center+radius-r.f.tlc;
                Int2 cs = new Int2((int)g.CellWidth,(int)g.CellHeight);
                Int2 start = offset/cs;
                for(int x=Math.Max(start.x,0); x*cs.x<offsetbrc.x; x++){
                  for(int y=Math.Max(start.y,0); y*cs.y<offsetbrc.y; y++){
                    if(g.Data[x,y]) return r.e;
                  }
                }
                continue;
              case ColliderList li:
                if(FloatRect.fromCorners(center-radius,center+radius).CollideCollider(li)) return r.e;
                continue;
            }
          }
        }
      }
      return null;
    }
  }
  static List<CollisionLevel> levels;
  static IntRect lbounds;
  static int maxlevel;
  const int levelLimit = 10;
  public static Dictionary<Type, Func<Entity, CollisionDirection>> cdirdict = new();
  static public void Construct(Scene scene, IntRect bounds){
    offset = new Vector2(bounds.x,bounds.y);
    if(bounds.w!=lbounds.w || bounds.h!=lbounds.h){
      levels.Clear();
      int w=Math.Max(1,(int)Math.Ceiling(bounds.w/bsize));
      int h=Math.Max(1,(int)Math.Ceiling(bounds.h/bsize));
      for(int maxlevel=0;; maxlevel++){
        levels.Add(new (w,h,maxlevel));
        w=(w+1)>>mipStep;
        h=(h+1)>>mipStep;
        if(w*h<=1 || maxlevel>=levelLimit) break;
      }
    } else for(int i=0; i<levels.Count; i++)levels[i].Clear();
    lbounds = bounds;
    foreach(Solid s in scene)Add(s,CollisionDirection.solid);
    foreach(JumpThru j in scene)Add(j,CollisionDirection.up);
  }
  static public void Clear(){
    for(int i=0; i<levels.Count; i++)levels[i].Clear();
  }

  static public Entity Test(Int2 center, Int2 rad, CollisionDirection dir){
    if(lbounds.CollideCenter(center,rad)) for(int i=0; i<=maxlevel; i++){
      var res = levels[i].GetFirstRadius(center,rad, dir);
      if(res!=null) return res;
    }
    return null;
  }
  public static void Add(Entity e, CollisionDirection dir){
    RectCollider r;
    r.e=e;
    r.dir=dir;
    if(r.dir!=CollisionDirection.none){
      r.f = new IntRect(
        (int)Math.Round(e.Left),(int)Math.Round(e.Top),
        (int)Math.Round(e.Width),(int)Math.Round(e.Height)
      );
      int i=0;
      int s = (int)Math.Ceiling(Math.Max(r.f.w,r.f.h)/bsize);
      for(; i<maxlevel && s>(1<<(mipStep*i)); i++);
      levels[i].Add(r);
    }
  }
}
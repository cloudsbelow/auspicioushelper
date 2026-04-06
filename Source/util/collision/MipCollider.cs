



using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

interface IMipCollider{
  const ulong FULL = 0xffff_ffff_ffff_ffffUL;
  const ulong BYTEMARKER = 0x0101_0101_0101_0101UL;
  const int BIG = 100000000;
  IntRect getBounds();
  ulong fromOffsetAbsolute(int level, Int2 offset);
  
  struct EvalLevel{
    public int level,x,y;
    public ulong mask = FULL;
    public EvalLevel(int level, int x, int y){
      this.level=level; this.x=x; this.y=y; 
    }
    public bool Empty=>mask==0;
    public Int2 Loc=>new(x,y);
    public EvalLevel Next(){
      int index = System.Numerics.BitOperations.TrailingZeroCount(mask);
      mask &= mask-1;
      return new(level-1,x*8+index%8, y*8+index/8);
    }
  }
  public static bool Collide(params ReadOnlySpan<IMipCollider> list){
    IntRect bounds = list[0].getBounds();
    Int2[] offsets = new Int2[list.Length];
    offsets[0] = bounds.tlc;
    for(int i=1; i<list.Length; i++){
      IntRect o = list[i].getBounds();
      bounds = bounds._intersect(o);
      offsets[i] = o.tlc;
    }
    if(bounds.w<=0 || bounds.h<=0) return false;
    Util.FiniteStack<EvalLevel> s = new(stackalloc EvalLevel[12]);
    for(int i=0; i<offsets.Length; i++) offsets[i]-=bounds.tlc;

    int ilevel = 0;
    int maxdim = Math.Max(bounds.w,bounds.h);
    while((1<<(3*(ilevel+1)))<maxdim) ilevel++;
    s.Push(new(ilevel,0,0));
    for(int i=0; i<list.Length; i++) s.Top.mask |= list[i].fromOffsetAbsolute(ilevel,-offsets[i]);
    while(!s.Empty){
      if(s.Top.Empty){
        s.Pop();
        continue;
      }
      if(s.Top.level == 0) return true;
      s.Push(s.Top.Next());
      int level = s.Top.level;
      Int2 loc = s.Top.Loc<<(3*level);
      for(int i=0; i<list.Length; i++) s.Top.mask |= list[i].fromOffsetAbsolute(level,loc-offsets[i]);
    }
    return false;
  }
  

  public class HorizontalHalfplane:IMipCollider{
    int mx;
    public HorizontalHalfplane(int x, bool positive)=>mx=positive?x:x-BIG;
    IntRect IMipCollider.getBounds()=>new IntRect(mx,-BIG,BIG,2*BIG);
    ulong IMipCollider.fromOffsetAbsolute(int level, Int2 offset) {
      int lshift = level+level+level;
      int row = 0xff;
      int x = offset.x;
      if(x<0) row &= 0xff>>((-x)>>lshift);
      int xinv = BIG-(x+8<<lshift);
      if(xinv<0) row &= 0xff<<((-xinv)>>lshift);
      // int x1 = Math.Clamp(-offset.x/ldenom,0,7);
      // int x2 = Math.Clamp((BIG-offset.x)/ldenom,1,8);
      // byte row = (byte)(((1<<(x2-x1))-1)<<x1);
      return BYTEMARKER*(ulong)row;
    }
  }
  public class CircleWrapper:IMipCollider{
    Circle c;
    public CircleWrapper(Circle c)=>this.c=c;
    IntRect IMipCollider.getBounds()=>IntRect.fromCenter(Int2.Round(c.AbsolutePosition),Int2.One*(int)Math.Ceiling(c.Radius));
    ulong IMipCollider.fromOffsetAbsolute(int level, Int2 offset){
      int lshift = level+level+level;
      return MipGrid.MakeCircleMask(offset.x>>lshift, offset.y>>lshift, Int2.One*(int)Math.Ceiling(c.Radius), c.Radius,level);
    }
  }
  public class HitboxWrapper:IMipCollider{
    Hitbox h;
    public HitboxWrapper(Hitbox h)=>this.h=h;
    IntRect IMipCollider.getBounds()=>IntRect.fromTlc(Int2.Round(h.AbsolutePosition),new((int)h.width,(int)h.height));
    ulong IMipCollider.fromOffsetAbsolute(int level, Int2 offset) {
      int lshift = level+level+level;
      int x1 = Math.Max(0,(-offset.x)>>lshift);
      int x2 = Math.Max(0,(offset.x+(8<<lshift)-(int)h.width)>>lshift);
      int y1 = Math.Max(0,(-offset.y)>>lshift);
      int y2 = Math.Max(0,(offset.y+(8<<lshift)-(int)h.height)>>lshift);
      if(((x1|x2|y1|y2)&~7)!=0) return 0;
      byte row = (byte)((0xff<<x1) & (0xff>>x2));
      int rows = y2-y1;
      var mask = rows==8? FULL: ((1UL << (rows*8))-1) << (y1*8);
      return mask & (0x0101_0101_0101_0101UL*row);
    }
  }
}
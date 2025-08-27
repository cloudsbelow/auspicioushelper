using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
class MipGridCollisionCacher{
  Dictionary<CacheKey, bool> results = new();
  struct CacheKey: IEquatable<CacheKey>{
    MipGrid a;
    MipGrid b;
    Vector2 l;
    Vector2 h;
    public CacheKey(MipGrid a, MipGrid b, Vector2 boffset){
      Vector2 dif = (a.tlc-(b.tlc+boffset))/a.cellshape;
      this.a=a; this.b=b; l=dif.Floor(); h=dif.Ceiling();
    }
    public bool Equals(CacheKey o){
      return ReferenceEquals(a, o.a) && ReferenceEquals(b, o.b) && l==o.l && h==o.h;
    }
    public override bool Equals(object obj)=>obj is CacheKey k && Equals(k);
    public override int GetHashCode() {
      return HashCode.Combine(RuntimeHelpers.GetHashCode(a), RuntimeHelpers.GetHashCode(b), l, h);
    }
  }
  public bool CollideCheck(MipGrid a, MipGrid b, Vector2 boffset){
    CacheKey col = new CacheKey(a,b,boffset);
    if(!results.TryGetValue(col,out var res)){
      results.Add(col,res = a.collideMipGridOffset(b,boffset));
    }
    return res;
  }
}
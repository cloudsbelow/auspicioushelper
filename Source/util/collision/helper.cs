


using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static class CollisionExtensions{
  public struct CachedCollision{
    Entity item = null;
    public CachedCollision(){}
    public void setNone()=>item=null;
    public void setItem(Entity i)=>item=i;
    public bool Check(Entity e, Vector2 at){
      if(item == null) return false;
      bool flag = e.CollideCheck(item,at);
      if(!flag) item=null;
      return flag;
    }
    public bool CheckOutside(Entity e, Vector2 at){
      if(item == null) return false;
      bool flag = e.CollideCheckOutside(e,at);
      if(!flag) item=null;
      return flag;
    }
    public bool CheckGrounded(Entity e, Vector2 groundvec){
      return (item is JumpThru && CheckOutside(e,groundvec)) || Check(e,groundvec);
    }
  }
  public static bool OnGroundCached(this Actor a, ref CachedCollision cache, int downCheck=1){
    Vector2 cpos = a.Position + new Vector2(0,downCheck);
    if(cache.CheckGrounded(a,cpos)) return true;
    Platform p = a.CollideFirst<Solid>(cpos);
    if(p == null && !a.IgnoreJumpThrus) p = a.CollideFirstOutside<JumpThru>(cpos);
    if(p!=null) cache.setItem(p);
    return p!=null;
  }
  public static bool OnGroundCached(this Actor a, ref CachedCollision cache, Vector2 at, int downCheck=1){
    using(new Util.AutoRestore<Vector2>(ref a.Position, at)) return OnGroundCached(a,ref cache, downCheck);
  }
}
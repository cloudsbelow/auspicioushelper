


using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper.Wrappers;
public class StrawbW:Strawberry, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  bool sd=false;
  public bool detatched {get{
    return sd=sd||Follower.HasLeader;
  }}
  public StrawbW(EntityData d, Vector2 o, EntityID id):base(d,o,id){
  }
}
public class FeatherW:FlyFeather, ISimpleEnt{
  TemplateDisappearer.vcaTracker vca = new();
  bool ownCollidable=true;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public FeatherW(EntityData d, Vector2 o):base(d,o){
    ResetEvents.LazyEnable(typeof(FeatherW));
  } 
  [ResetEvents.OnHook(typeof(FlyFeather),nameof(FlyFeather.Respawn))]
  static void Hook(On.Celeste.FlyFeather.orig_Respawn orig, FlyFeather s){
    orig(s);
    if(s is FeatherW f){
      f.ownCollidable = true;
      f.Collidable = f.ownCollidable && f.vca.Collidable;
    }
  }
  [ResetEvents.OnHook(typeof(FlyFeather),nameof(FlyFeather.OnPlayer))]
  static void Hook(On.Celeste.FlyFeather.orig_OnPlayer orig, FlyFeather s,Player p){
    orig(s,p);
    if(s is FeatherW f && !f.Collidable) f.ownCollidable = false;
  }
  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    vca.Align(vis,col,act);
    vca.Apply(this, ocol:ownCollidable);
  }
}
public class BoosterW:Booster, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public BoosterW(EntityData d, Vector2 o):base(d,o){}
  void ITemplateChild.relposTo(Vector2 loc, Vector2 ls){
    Position = loc+toffset;
    if(outline!=null)outline.Position = Position;
  }
}
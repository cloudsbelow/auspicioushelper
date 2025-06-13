


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
    DebugConsole.Write("here");
  }
}
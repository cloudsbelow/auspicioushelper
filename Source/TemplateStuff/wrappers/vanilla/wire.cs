


using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class CWire:Wire, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  Vector2 fromOffset;
  Vector2 toOffset;
  public CWire(EntityData d, Vector2 o):base(d,o){
  }
  public void setOffset(Vector2 ppos){
    fromOffset = Curve.Begin-ppos;
    toOffset = Curve.End-ppos;
    Depth+=parent.depthoffset;
  }
  public void relposTo(Vector2 loc, Vector2 ls){
    Curve.Begin = fromOffset+loc;
    Curve.End = toOffset+loc;
  }
}

public class CFlagline:CliffFlags, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  Vector2 fromOffset;
  Vector2 toOffset;
  Flagline flagline;
  public CFlagline(EntityData d, Vector2 o):base(d,o){
    flagline = Get<Flagline>();
  }
  public void setOffset(Vector2 ppos){
    fromOffset = Position-ppos;
    toOffset = flagline.To-ppos;
    //Depth+=parent.depthoffset;
  }
  public void relposTo(Vector2 loc, Vector2 ls){
    Position = fromOffset+loc;
    flagline.To = toOffset+loc;
  }
}

public class CClothsline:Clothesline, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  Vector2 fromOffset;
  Vector2 toOffset;
  Flagline flagline;
  public CClothsline(EntityData d, Vector2 o):base(d,o){
    flagline = Get<Flagline>();
  }
  public void setOffset(Vector2 ppos){
    fromOffset = Position-ppos;
    toOffset = flagline.To-ppos;
  }
  public void relposTo(Vector2 loc, Vector2 ls){
    Position = fromOffset+loc;
    flagline.To = toOffset+loc;
  }
}

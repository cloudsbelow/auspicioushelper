


using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;
public class StrawbW:Strawberry, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  bool sd=false;
  Template.Propagation ITemplateChild.prop=> Template.Propagation.None;
  public bool detatched {get{
    return sd=sd||Follower.HasLeader;
  }}
  public StrawbW(EntityData d, Vector2 o, EntityID id):base(d,o,id){
  }
}

public class BoosterW(Booster b):ISimpleWrapper{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  Monocle.Entity ISimpleWrapper.wrapped => b;
  void ITemplateChild.relposTo(Vector2 loc, Vector2 ls){
    b.Position = loc+toffset;
    if(b.outline!=null) b.outline.Position = b.Position;
  }
}

public class BoostW(BadelineBoost boost): ISimpleWrapper{
  Template.Propagation ITemplateChild.prop=> Template.Propagation.Riding;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public Entity wrapped=>boost;
  bool ITemplateChild.hasPlayerRider()=> boost.holding!=null;
  void ITemplateChild.parentChangeStat(int v, int c, int a)=> ISimpleWrapper.changeStatSafe(boost, v,c,a);
}


public class ChaserW:BadelineOldsite, ISimpleEnt{
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Template.Propagation ITemplateChild.prop=> Template.Propagation.None;
  void ITemplateChild.relposTo(Vector2 _, Vector2 __){}
  static int TemplateCount(Template t){
    while(t.parent!=null) t=t.parent;
    return t.GetChildren<ChaserW>().Count;
  }
  public ChaserW(EntityData d,Vector2 o):base(d.Position+o,TemplateCount(EntityParser.currentParent)){}
}
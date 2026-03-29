


using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/TemplateDisplacer")]
[Tracked]
public sealed class TemplateDisplacer:TemplateInstanceable{
  public Vector2[] nodes;
  public Vector2 origpos;
  public IntRect bounds = IntRect.empty;
  public TemplateDisplacer(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public TemplateDisplacer(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    nodes = d.Nodes??[];
    origpos=d.Position;
    foreach(var n in nodes) bounds.union_(Int2.Round(n-origpos));
    bounds.expandAll(1);
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    foreach(Vector2 n in nodes) addInstance(n-origpos);
  }
  const string TempIdent = "auspicioushelper/hehehehe";
  public static Template ConstructAt(DisplacerData dd, Vector2 o){
    using(new ChainLock()) return new Template(o+dd.Position){t=dd.disp};
  }
  static TemplateDisplacer()=>EntityParser.clarify(TempIdent, EntityParser.Types.template, (l,ld,o,e)=>{
    if(e is not DisplacerData cd) DebugConsole.WriteFailure("COMPAT ISSUE! CapturingData getting downcast to EntityData somewhere",true);
    else return ConstructAt(cd,o);
    return null;
  }, true);
  public class DisplacerData:EntityData{
    public templateFiller disp;
    public DisplacerData()=>Name=TempIdent;
  }
}
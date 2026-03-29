


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
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReset)]
  public static List<TemplateDisplacer> available = new();
  public Vector2[] nodes;
  public Vector2 origpos;
  bool[] used;
  public IntRect bounds = IntRect.empty;
  int id;
  public TemplateDisplacer(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public TemplateDisplacer(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    nodes = d.Nodes??[];
    origpos=d.Position;
    used = new bool[nodes.Length];
    foreach(var n in nodes) bounds.union_(Int2.Round(n-origpos));
    bounds.expandAll(1);
    id=d.ID;
  }
  protected override void EmptySetup(EntityData data) {
    TemplateBehaviorChain.AddEmptyTemplate(new TDataIn(){disp=new(this),Position=data.Position});
  }
  public override void Added(Scene scene) {
    if(parent!=null || hasDeclaredTemplate) base.Added(scene);
    else{
      origAdded(scene);
      UpdateHook.AddAfterUpdate(()=>{
        MarkExpanded();
        if(used.All(x=>x)) RemoveSelf();
        else for(int i=0; i<nodes.Length; i++) if(!used[i]){
          DebugConsole.Write($"Adding initial instance {i} at offset {nodes[i]-origpos}");
          addInstance(nodes[i]-origpos);
        }
      },true,false);
    }
  }
  public void setFiller(templateFiller tf){

  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    foreach(Vector2 n in nodes) addInstance(n-origpos);
  }
  const string TempIdent = "auspicioushelper/hehehehe";
  static TemplateDisplacer()=>EntityParser.clarify(TempIdent, EntityParser.Types.template, (l,ld,o,e)=>{
    if(e is not TDataOut cd) DebugConsole.WriteFailure("COMPAT ISSUE! CapturingData getting downcast to EntityData somewhere",true);
    else if(!cd.disp.TryGetTarget(out var targ)) DebugConsole.WriteFailure("Weak reference issue",true);
    else if(targ.t is not {} t) DebugConsole.Write("Template of displacer is empty");
    else using(new ChainLock()){
      DebugConsole.Write("Makign the hehehehe at ",o,e.Position);
      if(t.chain == null) return new Template(o+e.Position){t=t};
      else{
        EntityData dat = t.chain.NextEnt();
        if(!Level.EntityLoaders.TryGetValue(dat.Name, out var loader)) throw new Exception($"{dat.Name} has no loader");
        return loader(l,ld,o+e.Position-dat.Position,dat);
      } 
    }
    return null;
  }, true);
  class TDataOut:EntityData{
    public WeakReference<TemplateDisplacer> disp;
    public TDataOut()=>Name=TempIdent;
  }
  public class TDataIn:EntityData{
    public WeakReference<TemplateDisplacer> disp;
  }
  public EntityData makeCapturing(int i){
    used[i]=true;
    return new TDataOut(){
      ID = 800000+id*100+i, disp = new(this), Position=nodes[i]
    };
  }
}
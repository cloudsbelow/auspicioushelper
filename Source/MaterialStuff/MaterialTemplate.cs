
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/MaterialTemplate")]
public class MaterialTemplate:TemplateDisappearer, IOverrideVisualsEasy{
  public HashSet<OverrideVisualComponent> comps {get;} = new();
  bool invis;
  bool collidable = true;
  string channel;
  bool active=true;
  public MaterialTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public MaterialTemplate(EntityData d, Vector2 offset, int depthoffset):base(d,offset+d.Position,depthoffset){
    invis = d.Bool("dontNormalRender",true);
    lident = d.Attr("identifier","");
    if(string.IsNullOrWhiteSpace(lident)) DebugConsole.Write("No layer specified for material template");
    collidable = d.Bool("collidable",true);
    channel = d.Attr("channel","");
  }
  string lident;
  IOverrideVisuals layer;
  public override void addTo(Scene scene) {
    base.addTo(scene);
    List<Entity> l = new();
    AddAllChildren(l);
    var mlayer = MaterialController.getLayer(lident);
    if(mlayer is IOverrideVisuals qlay){
      layer = qlay;
      int tdepth = -TemplateDepth(); 
      foreach(var e in l) if(!(e is Template)){
        OverrideVisualComponent.Get(e).AddToOverride(new(layer,tdepth,invis));
      }
    } else {
      DebugConsole.Write($"Layer {lident} not found");
    }
    if(!collidable)setCollidability(false);
  }
  public override void OnNewEnts(List<Entity> l) {
    int tdepth = -TemplateDepth(); 
    if(layer!=null)foreach(var e in l)OverrideVisualComponent.Get(e).AddToOverride(new(layer,tdepth,invis));
    base.OnNewEnts(l);
  }
}
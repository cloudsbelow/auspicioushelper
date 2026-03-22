


using System;
using System.Linq;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
public class TemplateInstanceable:Template{
  public TemplateInstanceable(EntityData data, Vector2 pos, int depthoffset):base(data,pos,depthoffset){}
  public int numInstances {get; private set;} = 0;
  public Template addInstance(Vector2 offset, templateFiller template=null, Func<Vector2,Template> templateCtor=null){
    template = template??t;
    Template res;
    using(new Template.ChainLock()) res = templateCtor==null? new Template(offset+virtLoc):templateCtor(offset+virtLoc);
    res.t=template;
    res.ownidpath=numInstances++.ToString();
    addEnt(res);
    if(addingScene==null){
      AddNewEnts(res.GetChildren<Entity>());
    }
    return res;
  }
  public virtual void makeInitialInstances(){
    MarkExpanded();
  }
  public override void addTo(Scene scene) {
    addingScene = scene;
    setTemplate(scene:scene);
    makeInitialInstances();
    scene.Add(this);
    addingScene = null;
  }
}


public interface IRemovableContainer{
  public void RemoveChild(ITemplateChild c){

  }
  public bool RestoreChild(ITemplateChild c){
    return false;
  }
}
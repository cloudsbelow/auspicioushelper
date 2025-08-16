


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
  public bool useDisappearer = false;
  public Template addInstance(Vector2 offset, templateFiller template=null){
    template = template??t;
    Template res;
    if(template.chain is {}){
      if(EntityParser.create(template.ChildEntities.First(),(Scene??addingScene) as Level,template.roomdat,virtLoc+offset-t.origin,this,fullpath) is Template temp){
        temp.ownidpath = numInstances++.ToString();
        addEnt(res=temp);
      } else throw new System.Exception("oops");
    } else {
      addEnt(res=useDisappearer?new TemplateDisappearer(offset+virtLoc){
        t=template, ownidpath=numInstances++.ToString()
      }:new Template(offset+virtLoc){
        t=template, ownidpath=numInstances++.ToString()
      });
    }
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

[CustomEntity("auspicioushelper/TemplateInstancer")]
public class TemplateInstancer:TemplateInstanceable{
  Vector2[] nodes;
  public TemplateInstancer(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public TemplateInstancer(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    nodes = (d.Nodes??[]).Select(x=>x-d.Position).ToArray();
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    foreach(Vector2 n in nodes) addInstance(n);
  }
}
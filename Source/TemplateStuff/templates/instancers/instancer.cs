


using System.Linq;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

public class InstanceTemplate:Template{
  public InstanceTemplate(EntityData data, Vector2 pos, int depthoffset):base(data,pos,depthoffset){}
  int numInstances = 0;
  public bool useDisappearer = false;
  public Template addInstance(Vector2 offset){
    Template res;
    if(t.chain is {}){
      if(EntityParser.create(t.ChildEntities.First(),(Scene??addingScene) as Level,t.roomdat,Position+offset-t.origin,this,fullpath) is Template temp){
        temp.ownidpath = numInstances++.ToString();
        addEnt(res=temp);
      } else throw new System.Exception("oops");
    } else {
      addEnt(res=useDisappearer?new TemplateDisappearer(offset+Position){
        t=t, ownidpath=numInstances++.ToString()
      }:new Template(offset+Position){
        t=t, ownidpath=numInstances++.ToString()
      });
    }
    if(addingScene==null){
      AddNewEnts(res.GetChildren<Entity>());
    }
    return res;
  }
  public virtual void makeInitialInstances(){}
  public override void addTo(Scene scene) {
    addingScene = scene;
    setTemplate(scene:scene);
    scene.Add(this);
    addingScene = null;
  }
}

[CustomEntity("auspicioushelper/InstancedTemplate")]
public class InstancedTemplate:InstanceTemplate{
  Vector2[] nodes;
  public InstancedTemplate(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public InstancedTemplate(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    nodes = (d.Nodes??[]).Select(x=>x-d.Position).ToArray();
  }
  public override void makeInitialInstances() {
    base.makeInitialInstances();
    foreach(Vector2 n in nodes) addInstance(n);
  }
}
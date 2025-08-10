



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateBehaviorChain")]
public class TemplateBehaviorChain:Entity{
  public static Dictionary<Vector2,EntityData> mainRoom=new();
  public static void setup(){
    auspicioushelperModule.OnNewScreen.enroll(new PersistantAction(()=>{mainRoom.Clear();}));
    EntityParser.clarify("auspicioushelper/TemplateBehaviorChain",EntityParser.Types.unwrapped,(l,d,o,e)=>{
      string templateStr = e.Attr("template","");
      if(!MarkedRoomParser.getTemplate(templateStr, EntityParser.currentParent, l, out var t)){
        DebugConsole.Write($"No template found with identifier \"{templateStr}\" in {d.Name} at {d.Position}");
        return null;
      } else return startChain(t,e,e.Position+o,l,EntityParser.currentParent);
    },true);
  }
  public static void AddEmptyTemplate(EntityData d){
    mainRoom[d.Position] = d;
  }
  public static IEnumerator GetChainEnumerator(Vector2[] nodes, Template source){
    var contents = source?.t.room.emptyTemplates??mainRoom;
    if(nodes!=null)foreach(var n in nodes){
      if(contents.TryGetValue(n, out var e)){
        if(e.Name == "auspicioushelper/TemplateBehaviorChain") yield return GetChainEnumerator(e.Nodes,source);
        else yield return e;
      } else {
        if(source!=null)DebugConsole.WriteFailure($"Did not find empty template at {n} in {source} with path {source.fullpath}");
        else DebugConsole.WriteFailure($"Did not find empty template at {n}");
      }
    }
  }
  public class Chain{
    Util.EnumeratorStack<EntityData> stack;
    templateFiller final;
    public Chain(templateFiller finalFiller, Vector2[] nodes, Template source = null){
      stack = new(GetChainEnumerator(nodes,source));
      final = finalFiller;
    }
    public templateFiller NextFiller(){
      EntityData e = stack.Next();
      if(e==null) return final;
      return templateFiller.MkNestingFiller(e,this).setRoomdat(final.roomdat);
    }
    public EntityData NextEnt(){
      return stack.Next();
    }
  }
  Vector2[] nodes;
  string templateStr;
  EntityData dat;
  public TemplateBehaviorChain(EntityData d, Vector2 o):base(d.Position+o){
    nodes = d.Nodes;
    dat=d;
    templateStr = d.Attr("template","");
    if(string.IsNullOrWhiteSpace(templateStr)){
      AddEmptyTemplate(d);
    }
  }
  static Template startChain(templateFiller final, EntityData dat, Vector2 pos, Level l, Template scope=null){
    var chain = new Chain(final,dat.Nodes,scope);
    var first = chain.NextEnt();
    if(first == null){
      return new Template(dat, pos, 0);
    } else {
      if(!Level.EntityLoaders.TryGetValue(first.Name, out var loader)){
        DebugConsole.WriteFailure("Unknown template type");
        return null;
      } else {
        Entity e = loader(l,l.Session.LevelData,pos-first.Position,first);
        if(e is Template te){
          te.t = chain.NextFiller();
          return te;
        }
        throw new Exception($"your chained entity is not a template? how did u do this? {e}");
      }
    }
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    if(string.IsNullOrWhiteSpace(templateStr)){
      RemoveSelf(); return;
    }
    if(!MarkedRoomParser.getTemplate(templateStr, null, scene, out var t)){
      DebugConsole.Write($"No template found with identifier \"{templateStr}\" in {this} at {Position}");
    } else startChain(t, dat, Position, scene as Level, null)?.addTo(scene);
    RemoveSelf();
  }
}
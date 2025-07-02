



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateBehaviorChain")]
public class TemplateBehaviorChain:Entity{
  public static Dictionary<ScopedPosition,EntityData> things=new();
  public readonly struct ScopedPosition:IEquatable<ScopedPosition>{
    readonly MarkedRoomParser.TemplateRoom s;  
    readonly Vector2 p;
    public override bool Equals(object obj){
      return obj is ScopedPosition o && Equals(o);
    }
    public bool Equals(ScopedPosition other)=>p==other.p && s==other.s; 
    public override int GetHashCode(){
      return HashCode.Combine(s,p);
    }
    ScopedPosition(Vector2 p, Template s){
      this.p=p; this.s=s?.t.room;
    }
    public static IEnumerator Get(Vector2[] nodes, Template source){
      foreach(var n in nodes){
        if(things.TryGetValue(new ScopedPosition(n,source), out var e)){
          if(e.Name == "auspicioushelper/TemplateBehaviorChain") yield return Get(e.Nodes,source);
          else yield return e;
        } else {
          DebugConsole.WriteFailure($"Did not find empty template at {n} in {source} with path {source.fullpath}");
        }
      }
    }
    public static void Add(EntityData d, Template parent = null){
      things[new ScopedPosition(d.Position,parent)] = d;
    }
  }
  public class Chain{
    Util.EnumeratorStack<EntityData> stack;
    templateFiller final;
    public Chain(templateFiller finalFiller, Vector2[] nodes, Template source = null){
      stack = new(ScopedPosition.Get(nodes,source));
      final = finalFiller;
    }
    public templateFiller NextFiller(){
      EntityData e = stack.Next();
      if(e==null) return final;
      return templateFiller.MkNestingFiller(e,this);
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
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    if(!MarkedRoomParser.getTemplate(templateStr, null, scene, out var t)){
      DebugConsole.Write($"No template found with identifier \"{templateStr}\" in {this} at {Position}");
    }
    var chain = new Chain(t,nodes);
    var first = chain.NextEnt();
    if(first == null){
      DebugConsole.Write("Empty behavior chain (please don't)");
      scene.Add(new Template(dat, Position, 0));
    } else {
      if(!Level.EntityLoaders.TryGetValue(first.Name, out var loader)){
        DebugConsole.WriteFailure("Unknown template type");
      } else {
        Level l = Scene as Level;
        Entity e = loader(l,l.Session.LevelData,Position-first.Position,first);
        DebugConsole.Write($"first in chain: {e} {e.Position} {Position}");
        if(e is Template te){
          te.t = chain.NextFiller();
          te.addTo(scene);
        } else {
          DebugConsole.Write($"your chained entity is not a template? how did u do this? {e}");
        }
      }
    }
    RemoveSelf();
  }
}
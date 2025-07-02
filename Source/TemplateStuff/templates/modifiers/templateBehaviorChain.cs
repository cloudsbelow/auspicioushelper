



using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateBehaviorChain")]
public class TemplateBehaviorChain:Entity{
  public static Dictionary<ScopedPosition,EntityData> things=new();
  public readonly struct ScopedPosition:IEquatable<ScopedPosition>{
    readonly Template s;  
    readonly Vector2 p;
    public override bool Equals(object obj){
      return obj is ScopedPosition o && Equals(o);
    }
    public bool Equals(ScopedPosition other)=>p==other.p && s==other.s; 
    public override int GetHashCode(){
      return HashCode.Combine(s,p);
    }
    ScopedPosition(Vector2 p, Template s){
      this.p=p; this.s=s;
    }
    public static List<EntityData> Get(Vector2[] nodes, Template source){
      List<EntityData> ret = new();
      if(nodes==null) return ret;
      foreach(var n in nodes){
        if(things.TryGetValue(new ScopedPosition(n,source), out var e)){
          ret.Add(e);
        } else {
          DebugConsole.WriteFailure($"Did not find empty template at {n} in {source} with path {source.fullpath}");
        }
      }
      return ret;
    }
  }
  Vector2[] nodes;
  public TemplateBehaviorChain(EntityData d, Vector2 o):base(d.Position+o){
    nodes = d.Nodes;
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    RemoveSelf();
    foreach(EntityData e in ScopedPosition.Get(nodes, null)){
      
    }
  }
}
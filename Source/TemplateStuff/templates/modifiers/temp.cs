



using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;
public class TemplateBehaviorChain:Template{
  public static Dictionary<ScopedPosition,EntityData> things=new();
  public struct ScopedPosition:IEquatable<ScopedPosition>{
    Template s; 
    Vector2 p;
    public override bool Equals(object obj){
      return obj is ScopedPosition o && Equals(o);
    }
    public bool Equals(ScopedPosition other)=>p==other.p && s==other.s; 
    public override int GetHashCode(){
      return HashCode.Combine(s,p);
    }
    public List<EntityData> Get(Vector2[] nodes, Template source){
      List<EntityData> ret = new();
      if(nodes==null) return ret;
      foreach(var n in nodes){
        if(things.TryGetValue(new ScopedPosition{p=n,s=source}, out var e)){
          ret.Add(e);
        } else {
          DebugConsole.WriteFailure($"Did not find empty template at {n} in {source} with path {source.fullpath}");
        }
      }
      return ret;
    }
  }
  public TemplateBehaviorChain():base(null,Vector2.Zero,0){}
}
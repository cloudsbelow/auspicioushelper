


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
public class MapenterEv:Attribute{
  static Dictionary<string, (Action<EntityData,LevelData>, Action)> found = new();
  string fnName;
  public MapenterEv(string fnName){
    this.fnName=fnName;
  }
  const BindingFlags bf = BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  static MapenterEv(){
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      if(t.IsDefined(typeof(MapenterEv))){
        var attr = (MapenterEv)Attribute.GetCustomAttribute(t, typeof(MapenterEv));
        var ce = (CustomEntityAttribute)Attribute.GetCustomAttribute(t,typeof(CustomEntityAttribute));
        Action<EntityData, LevelData> fn = null;
        
        if(t.GetMethod(attr.fnName, bf, [typeof(EntityData), typeof(LevelData)]) is {} l0){
          if(l0.ReturnType == typeof(void)) fn = l0.CreateDelegate<Action<EntityData, LevelData>>();
          else fn = (d,ld)=>l0.Invoke(null,[d,ld]);
        }
        if(t.GetMethod(attr.fnName, bf, [typeof(EntityData)]) is {} l1){
          if(fn!=null) DebugConsole.WriteFailure("dummy idit.",true);
          if(l1.ReturnType == typeof(void)){
            var fn_ = l1.CreateDelegate<Action<EntityData>>();
            fn = (d,ld)=>fn_(d);
          } else fn = (d,ld)=>l1.Invoke(null,[d]);
        }
        var sfn = t.GetMethod(attr.fnName, bf, [])?.CreateDelegate<Action>();

        if(ce == null || (fn==null && sfn==null)) DebugConsole.WriteFailure("bad attribute. Either not custom entity or load method unfound.",true);
        foreach(var s in ce.IDs) found.Add(s,(fn,sfn));
      }
    }
  }
  public static void Run(MapData mapdata){
    HashSet<Action> run = new();
    foreach(LevelData ld in mapdata.Levels){
      foreach(EntityData d in ld.Entities){ 
        if(found.TryGetValue(d.Name,out var pair)){
          pair.Item1(d,ld);
          if(pair.Item2 is {} fn) run.Add(fn);
        }
      }
    }
    foreach(var m in run) m();
  }
}
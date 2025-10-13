


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
public class MapenterEv:Attribute{
  static Dictionary<string, (MethodInfo, MethodInfo)> found = new();
  string fnName;
  public MapenterEv(string fnName){
    this.fnName=fnName;
  }
  static MapenterEv(){
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      if(t.IsDefined(typeof(MapenterEv))){
        var attr = (MapenterEv)Attribute.GetCustomAttribute(t, typeof(MapenterEv));
        var ce = (CustomEntityAttribute)Attribute.GetCustomAttribute(t,typeof(CustomEntityAttribute));
        var load1 = t.GetMethod(attr.fnName,BindingFlags.Static,[typeof(EntityData)]);
        var load2 = t.GetMethod(attr.fnName,BindingFlags.Static,[]);
        if(ce == null || (load1??load2) == null) DebugConsole.WriteFailure("bad attribute. Either not custom entity or load method unfound.",true);
        foreach(var s in ce.IDs) found.Add(s,(load1,load2));
      }
    }
  }
  public static void Run(MapData mapdata){
    HashSet<MethodInfo> run = new();
    foreach(LevelData ld in mapdata.Levels){
      foreach(EntityData d in ld.Entities){ 
        if(found.TryGetValue(d.Name,out var pair)){
          pair.Item1?.Invoke(null,[d]);
          if(pair.Item2 is {} fn && !run.Contains(fn)){
            run.Add(fn);
            fn.Invoke(null,[]);
          }
        }
      }
    }
  }
}



using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
public class CustomloadEntity:Attribute{
  static Dictionary<string, Action<Level,LevelData,Vector2,EntityData>> found = new();
  string fnName;
  public CustomloadEntity(string fnName){
    this.fnName=fnName;
  }
  public CustomloadEntity(){
    fnName=null;
  }
  static CustomloadEntity(){
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      if(t.IsDefined(typeof(CustomloadEntity))){
        var attr = (CustomloadEntity)Attribute.GetCustomAttribute(t, typeof(CustomloadEntity));
        var ce = (CustomEntityAttribute)Attribute.GetCustomAttribute(t,typeof(CustomEntityAttribute));
        Action<Level,LevelData,Vector2,EntityData> l = null;
        if(attr.fnName is {} fn){
          BindingFlags flags = BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
          if(t.GetMethod(fn,flags,[typeof(EntityData)]) is {} f1)l=(l,d,o,e)=>f1.Invoke(null,[e]);
          else if(t.GetMethod(fn,flags,[typeof(Level),typeof(LevelData),typeof(Vector2),typeof(EntityData)]) is {} f2)l=(l,d,o,e)=>f2.Invoke(null,[l,d,o,e]);
          else if(t.GetMethod(fn,flags,[]) is {} f3)l=(l,d,o,e)=>f3.Invoke(null,[]);
          if(l==null) DebugConsole.WriteFailure($"Could not find valid function {fn} on {t}",true);
        }
        if(ce!=null) foreach(var s in ce.IDs) found.Add(s,l);
      }
    }
  }
  static bool Load(Level l, LevelData d, Vector2 o, EntityData e){
    if(!found.TryGetValue(e.Name,out var fn))return false;
    if(fn!=null) fn(l,d,o,e);
    return true;
  }
  [OnLoad]
  public static HookManager hooks = new(()=>{
    Everest.Events.Level.OnLoadEntity+=Load;
  },()=>{
    Everest.Events.Level.OnLoadEntity-=Load;
  });
}
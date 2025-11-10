



using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Celeste.Mod.Entities;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class FoundEntity:OnAnyRemoveComp{
  public string ident;
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  static Dictionary<string, FoundEntity> found = new();
  public FoundEntity(string identifier, Entity e):base(false,false){
    ident = identifier;
    found[ident] = this;
    e.Add(this);
  }
  public override void OnRemove() {
    found.Remove(ident);
  }
  public static object reflectGet(Entity e, List<string> path, List<int> args, int startidx = 2){
    object o = e;
    int j=0;
    for(int i=startidx; i<path.Count; i++){
      if(o == null) return 0;
      Type type = o.GetType();
      if(path[i] == "__index__"){
        if(o is IList list) o = list[args[j]];
        else if(o is IEnumerable enumer){
          int k=0;
          o=null;
          foreach(object c in enumer){
            if(k++ == args[j]){
              o=c; break;
            }
          }
        }
        j++;continue;
      }
      FieldInfo field = type.GetField(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if(field!=null){
        o=field.GetValue(o); continue;
      }
      PropertyInfo prop = type.GetProperty(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if(prop != null){
        //DebugConsole.Write($"Getting prop {path[i]} {o.ToString()}")
        o = prop.GetValue(o); continue;
      }
      MethodInfo method = type.GetMethod(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
      if (method != null) {
          return method;
      }

      DebugConsole.WriteFailure($"The reflection process on entity {e?.ToString()} failed at index {i} looking for {path[i]} on {o?.ToString()}");
      return 0;
    }
    return o;
  }
  
  public object reflectGet(List<string> path, List<int> args){
    return reflectGet(Entity,path,args);
  }
  public static void clear(Scene refill = null){
    found.Clear();
    if(refill!=null){
      foreach(FoundEntity f in refill.Tracker.GetComponents<FoundEntity>()){
        found[f.ident] = f;
      }
    }
  }
  public static object sreflectGet(List<string> path, List<int> args){
    if(!found.TryGetValue(path[1], out var f)){
      DebugConsole.Write($"Entity with attached identifier {path[1]} not found");
      return 0;
    }
    return f.reflectGet(path,args);
  }
  public static object sreflectCall(List<string> path, List<int> args){
    if(!found.TryGetValue(path[1], out var f)){
      DebugConsole.Write($"Entity with attached identifier {path[1]} not found");
      return 0;
    }
    object o = f.reflectGet(path,args);
    if(o is MethodInfo m){
      try{
        return m.Invoke(f.Entity,null);
      } catch(Exception ex){
        DebugConsole.Write($"Method invocation on {path[1]} failed:\n {ex}");
      }
    }
    return null;
  }
  public static FoundEntity find(string ident){
    if(ident=="player"){
      if(Engine.Instance.scene is Level l && l.Tracker.GetEntity<Player>() is {} p){
        if(!(p.Get<FoundEntity>() is {} fent)) fent = new FoundEntity("player",p); 
        return fent;
      }
      return null;
    }
    return found.TryGetValue(ident, out var f)?f:null;
  }


  [CustomEntity("auspicioushelper/EntityMarkingFlag")]
  [MapenterEv(nameof(Search))]
  [CustomloadEntity]
  public class MarkingFlag:Entity{
    static void Search(EntityData d){
      //Finder.watch(d.Attr("path"),d.Attr("identifier"));
      Finder.watch(d.Attr("path"),(e)=>new FoundEntity(d.Attr("identifier"),e));
    }
  }
}
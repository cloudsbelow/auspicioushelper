



using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class FoundEntity:Component{
  int id;
  EntityData d;
  public string ident;
  static Dictionary<string, FoundEntity> found = new();
  public FoundEntity(EntityData d, string identifier):base(false,false){
    id=d.ID; this.d=d; ident = identifier;
  }
  public void finalize(Entity e){
    if(e==null){
      DebugConsole.Write($"Failed to find the entity {d.Name} with id {id} - (maybe this entity adds itself non-standardly?)");
      return;
    }
    DebugConsole.Write($"Found the entity {d.Name} with id {id}");
    found[ident] = this;
    e.Add(this);
  }
  public override void EntityRemoved(Scene scene){
    base.EntityRemoved(scene);
    found.Remove(ident);
  }
  public override void Removed(Entity entity){
    base.Removed(entity);
    found.Remove(ident);
  }
  public static int reflectGet(Entity e, List<string> path, List<int> args, int startidx = 2){
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
            if(k == args[j]){
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
      DebugConsole.Write($"The reflection process on entity {e?.ToString()} failed at index {i} looking for {path[i]} on {o?.ToString()}");
      return 0;
    }
    try {
      return Convert.ToInt32(o);
    } catch(Exception){
      return o==null?0:1;
    }
  }
  public int reflectGet(List<string> path, List<int> args){
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
  public static int sreflectGet(List<string> path, List<int> args){
    if(!found.TryGetValue(path[1], out var f)){
      DebugConsole.Write($"Entity with attached identifier {path[1]} not found");
      return 0;
    }
    return reflectGet(f.Entity, path, args);
  }
  public static FoundEntity find(string ident){
    return found.TryGetValue(ident, out var f)?f:null;
  }
}
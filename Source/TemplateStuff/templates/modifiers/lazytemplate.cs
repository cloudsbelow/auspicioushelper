




using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class LazyTemplate:Template{
  public static HashSet<LazyTemplate> toRemove;
  public LazyTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public LazyTemplate(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){}

  List<Entity> AddedEnts = new();
  List<Entity> AddingEnts = new();
  public static void Unload(){
    HashSet<Entity> removing = new();
    HashSet<Component> removingComp = new();
    HashSet<Type> toSortEntity = new();
    HashSet<Type> toSortComponet = new();
    HashSet<Type> processed = new();
    Tracker tr = (Engine.Instance.scene as Level).Tracker;
    foreach(var lt in toRemove){
      if(lt.AddedEnts.Count>0 || lt.AddingEnts.Count>0) throw new Exception();
      foreach(var e in lt.GetChildren<Entity>()){
        Type et = e.GetType();
        if(!processed.Contains(et)){
          if(Tracker.TrackedEntityTypes.TryGetValue(t, out var l))foreach(var ty in l){
            toSortComponet.Add(ty);
          }
          processed.Add(t);
        }
        foreach(var c in e.Components){
          Type ct=c.GetType();
          if(!processed.Contains(ct)){
            if(Tracker.TrackedComponentTypes.TryGetValue(ct, out var l))foreach(var ty in l){
              toSortComponet.Add(ty);
            }
            processed.Add(ct);
          }
          removingComp.Add(c);
        }
        removing.Add(e);
      }
    }
    toRemove.Clear();
    foreach(Type ty in toSortComponet){
      if(tr.Components.TryGetValue(ty, out var li)){
        List<Component> nlist = new();
        foreach(var c in li) if(!removingComp.Contains(c)) nlist.Add(c);
        tr.Components[ty]=nlist;
      }
    }
    foreach(Type ty in toSortEntity){
      if(tr.Entities.TryGetValue(ty, out var li)){
        List<Entity> nlist = new();
        foreach(var e in li) if(!removing.Contains(e)) nlist.Add(e);
        tr.Entities[ty]=nlist;
      }
    }
    TagLists tl = (Engine.Instance.scene as Level).TagLists;
    for(int i=0; i<tl.lists.Length; i++){
      List<Entity> nlist = new();
      foreach(var e in tl.lists[i]) if(!removing.Contains(e)) nlist.Add(e);
      tl.lists[i]=nlist;
    }
  }
}




// using System;
// using System.Collections.Generic;
// using System.Collections.Specialized;
// using Monocle;

// namespace Celeste.Mod.auspicioushelper;

// public class TrackedComponent:Component{
//   Util.HybridSet<TTracker> inTrackers;
//   public TrackedComponent():base(false,false){}
//   public void TrackerRemoved(TTracker t)=>inTrackers.Remove(t);
//   public override void EntityRemoved(Scene scene) {
//     base.EntityRemoved(scene);
//     foreach(var p in parents)p.o.RemoveC(this);
//   }
//   public override void SceneEnd(Scene scene) {
//     base.SceneEnd(scene);
//     foreach(var p in parents)p.o.RemoveC(this);
//   }
//   public override void Removed(Entity entity) {
//     base.Removed(entity);
//     foreach(var p in parents)p.o.RemoveC(this);
//   }
// }
// public class TTrackerContext{
//   static 
//   Util.HybridSet<Type> tracked;
//   public List<Entity> GetTrackedAs(Type t){
//     return null;
//   }
// }
// public class TTracker:Component{
//   Dictionary<Type,Util.HybridSet<Entity>> tracked;
//   public TTracker(TTrackerContext ctx){

//   }
//   public void Track(Entity e){
//     if
//   }
//   public void Untrack(Entity e){

//   }
// }
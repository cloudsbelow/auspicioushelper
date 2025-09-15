



using System.Collections;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public abstract class OnAnyRemoveComp:Component{
  public OnAnyRemoveComp(bool a, bool b):base(a,b){}
  public abstract void OnRemove();
  public override void EntityRemoved(Scene scene) {
    base.EntityRemoved(scene);
    OnRemove();
  }
  public override void SceneEnd(Scene scene) {
    base.SceneEnd(scene);
    OnRemove();
  }
  public override void Removed(Entity entity) {
    base.Removed(entity);
    OnRemove();
  }
}
public class GroupTracker{
  class TrackedComponent:OnAnyRemoveComp{
    Util.HybridSet<GroupTracker> inside = new();
    public TrackedComponent():base(false,false){}
    public override void OnRemove(){
      foreach(var t in inside) t.QuietRemove(this);
      inside.Clear();
    }
    public void RemoveFrom(GroupTracker tr)=>inside.Remove(tr);
    public void AddTo(GroupTracker tr)=>inside.Add(tr);
  }
  static TrackedComponent Get(Entity e){
    if(e.Get<TrackedComponent>() is {} o) return o;
    e.Add(o = new TrackedComponent());
    o.Entity=e;
    return o;
  }
  void Add(TrackedComponent c){
    tracked.Add(c);
    c.AddTo(this);
  }
  void Remove(TrackedComponent c){
    tracked.Remove(c);
    c.RemoveFrom(this);
  }
  public void Clear(){
    foreach(var c in tracked) c.RemoveFrom(this);
    tracked.Clear();
  }
  void QuietRemove(TrackedComponent c)=>tracked.Remove(c);
  Util.HybridSet<TrackedComponent> tracked = new();
  public class TrackedGroupComp:OnAnyRemoveComp, IEnumerable<Entity>{
    GroupTracker tracked = new();
    public TrackedGroupComp():base(false,false){}
    public override void OnRemove() {
      tracked.Clear();
    }
    public void Add(Entity e)=>tracked.Add(Get(e));
    public void Remove(Entity e)=>tracked.Remove(Get(e));
    public IEnumerator<Entity> GetEnumerator(){
      foreach(var c in tracked.tracked) yield return c.Entity;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}



using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public partial class Anti0fZone{
  class HoldableRaster:LinearRaster<Holdable>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(1,1);
      Fill(p.Scene.Tracker.GetComponents<Holdable>().Map(
        h=>{
          Holdable o = (Holdable) h;
          Collider origc = h.Entity.Collider;
          if(o.PickupCollider!=null){
            h.Entity.Collider = o.PickupCollider;
          }
          var res =  new ACol<Holdable>(f.ISweep(h.Entity.Collider,-step),(Holdable)h);
          h.Entity.Collider = origc;
          return res;
        }
      ),maxt);
    }
    public bool prog(Player p, float step){
      if(!Input.GrabCheck || p.IsTired || p.Holding!=null) return false;
      switch(p.StateMachine.state){
        case 0: case 7:
          if(p.Ducking) return false; break;
        case 2:
          if(p.DashDir == Vector2.Zero || !p.CanUnDuck) return false; break;
        default: return false;
      }
      prog(step);
      foreach(var h in active){
        if(h.o.Check(p) && p.Pickup(h.o)){
          DebugConsole.Write("Picked up");
          p.StateMachine.State = 8; 
          return true;
        }
      }
      return false;
    }
  }
  class ColliderRaster:LinearRaster<PlayerCollider>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(1,1);
      Fill(p.Scene.Tracker.GetComponents<PlayerCollider>().Where(h=>h.Entity.Collidable).Select(
        h=>new ACol<PlayerCollider>(f.ISweep(h.Entity.Collider,-step),(PlayerCollider)h)
      ),maxt);
    }
    public bool prog(Player p, float step){
      if(p.StateMachine.state == 21) return false;
      prog(step);
      Collider old = p.Collider;
      p.Collider = p.hurtbox;
      active.RemoveAll(cn=>{
        if(p.Dead || cn.o.Check(p)) return true;
        return false;
      });
      p.Collider = old;
      return p.Dead;
    }
  }
  class TriggerRaster:LinearRaster<Trigger>{
    public void Fill(Player p, Vector2 step, float maxt){
      FloatRect f = new FloatRect(p)._expand(4,4);
      Fill(p.Scene.Tracker.GetEntities<Trigger>().Map(
        h=>new ACol<Trigger>(f.ISweep(h.Collider,-step),(Trigger)h)
      ),maxt);
    }
    public bool prog(Player p, float step){
      if (p.StateMachine.State == 18) return false;
      prog(step);
      active.RemoveAll(cn=>{
        var t = cn.o;
        if (p.CollideCheck(t)){
          if (!t.Triggered){
            t.Triggered = true;
            p.triggersInside.Add(t);
            t.OnEnter(p);
          }
          t.OnStay(p);
          return true;
        } 
        return false;
      });
      return false;
    }
  }

  class TrackerOverride:IDisposable{
    Dictionary<Type, List<Entity>> oldEnts = new();
    Dictionary<Type, List<Component>> oldComps = new();
    static TrackerOverride top=null;
    TrackerOverride last;
    Tracker t;
    public TrackerOverride(Tracker tracker){
      last=top;
      if(last!=null && last.t!=tracker) throw new Exception("don't mix trackers.");
      if(last!=null) throw new NotImplementedException("paranoia throw (i actually implemented it)");
      top=this;
      t=tracker;
    }
    public void restore(){
      foreach(var (k,e) in oldEnts){
        e.RemoveAll(x=>x.Scene==null);
        t.Entities[k]=e;
      }
      foreach(var (k,c) in oldComps){
        c.RemoveAll(x=>x.Scene==null);
        t.Components[k]=c;
      } 
      oldEnts.Clear();
      oldComps.Clear();
    }
    void IDisposable.Dispose(){
      restore();
      top = last;
    }
    public static void SetEnt(Type t, List<Entity> nlist){
      if(!top.oldEnts.ContainsKey(t)) top.oldEnts.Add(t,top.t.Entities[t]);
      top.t.Entities[t]=nlist;
    }
    public static void SetComp(Type t, List<Component> nlist){
      if(!top.oldComps.ContainsKey(t)) top.oldComps.Add(t,top.t.Components[t]);
      top.t.Components[t]=nlist;
    }
    public static void Restore()=>top.restore();
  }

  public static void CullToPathEnt(Scene s, Type e, FloatRect f, Vector2 path){
    TrackerOverride.SetEnt(e, s.Tracker.Entities[e].MapAndFilter(ent=>{
      return (ent,f.ISweep(ent.Collider,-path).collidesOne);
    }));
  }
  public static void CullToPathComp(Scene s, Type e, FloatRect f, Vector2 path, Func<Component, Collider> getCollider){
    TrackerOverride.SetComp(e, s.Tracker.Components[e].MapAndFilter(c=>{
      var o = c.Entity.Collider;
      var n = getCollider==null? o:(getCollider(c)??o);
      if(o!=n) c.Entity.Collider = n; 
      var res = f.ISweep(n,-path).collidesOne;
      if(o!=n) c.Entity.Collider = o; 
      return (c,res);
    }));
  }
}
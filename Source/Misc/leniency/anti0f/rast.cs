


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public partial class Anti0fZone{
  public struct ACol<T>{
    public FloatRect.FRCollision f;
    public T o;
    public int order;
    public ACol(FloatRect.FRCollision info, T col){
      f=info; o=col;
    }
  }
  public abstract class LinearRaster<T>{
    public List<ACol<T>> active = new();
    public List<ACol<T>> mayHit = new();
    public int addIdx = 0;
    bool noProg = false;
    public void Fill(IEnumerable<ACol<T>> l, float maxt, bool noprog = false){
      var idx = 0;
      var enu = l.GetEnumerator();
      while(enu.MoveNext()){
        var col = enu.Current;
        if(col.f.collides && col.f.enter<maxt){
          col.order=idx++;
          mayHit.Add(col);
        }
      }
      if(this.noProg = noprog) active=mayHit;
      else mayHit.Sort((a,b)=>{
        var c1 = MathF.Max(a.f.enter,0)- MathF.Max(b.f.enter,0);
        if(c1 != 0) return MathF.Sign(c1);
        return a.order-b.order;
      });
    }
    public void Clear(){
      active.Clear();
      mayHit.Clear();
      addIdx=0;
    }
    public bool prog(float step){
      if(noProg) return false;
      bool changeFlag = false;
      active.RemoveAll(x=>{
        bool f = x.f.exit<step;
        changeFlag |= f;
        return f;
      });
      if(addIdx>=mayHit.Count || mayHit[addIdx].f.enter>step) return changeFlag;
      while(addIdx<mayHit.Count && mayHit[addIdx].f.enter<=step){
        if(mayHit[addIdx].f.exit>=step) active.Add(mayHit[addIdx]);
        addIdx++;
      }
      return true;
    }
  }

  class ZoneRaster:LinearRaster<Anti0fZone>{
    int zpc; public bool doplayercolliders=>zpc>0; public bool hpc;
    int zt; public bool dotriggers=>zt>0; public bool ht;
    int zh; public bool doholdables=>zh>0; public bool hh;
    int zs; public bool dosolids=>zs>0; public bool hs;
    int zwj; public bool dowalljumps=>zwj>0;
    public int minSteps=0;
    bool wholeroom;

    void inire(Anti0fZone z, int adj){
      zpc+=z.cplayercolliders?adj:0;
      zt+=z.ctriggers?adj:0;
      zh+=z.cthrowables?adj:0;
      zs+=z.csolids?adj:0;
      zwj+=z.alwayswjc?adj:0;
      minSteps = z.mindist;
    }
    Anti0fZone iniw(Anti0fZone z){
      if(z.wholeroom){
        wholeroom = true;
        hpc|=z.cplayercolliders;
        ht|=z.ctriggers;
        hh|=z.cthrowables;
        hs|=z.csolids;
        inire(z,1);
      }
      return z;
    }
    public bool Fill(Player p, Vector2 step, float maxt, bool first){
      FloatRect f = new FloatRect(p)._expand(1,1);
      wholeroom = false; zpc=0; zt=0; zh=0; zs=0; zwj=0;
      hpc = false; ht = false; hh = false; hs = false;
      Fill(p.Scene.Tracker.GetEntities<Anti0fZone>().Map(
        h=>new ACol<Anti0fZone>(f.ISweep(h.Collider,-step),iniw((Anti0fZone)h))
      ),maxt);
      if(SolidAnti0fComp.Use(p)){
        wholeroom=true;
        hs=true;
        zs+=1;
      }
      if(first && !useSteps) return false;
      foreach(var z_ in mayHit){
        var z = z_.o;
        hpc|=z.cplayercolliders;
        ht|=z.ctriggers;
        hh|=z.cthrowables;
        hs|=z.csolids;
      }
      if(hpc) crast.Fill(p,step,maxt);
      if(ht) trast.Fill(p,step,maxt);
      if(hh) hrast.Fill(p,step,maxt);
      if(hs){
        srast.Fill(p,step,maxt);
        CullToPathEnt(p.Scene, typeof(JumpThru), f._expand(8,8), step*maxt);
      }
      return true;
    }
    public bool prog(Player p, float step){
      active.RemoveAll(x=>{
        if(x.f.exit<step){
          inire(x.o,-1);
          return false;
        }
        return false;
      });
      while(addIdx<mayHit.Count && mayHit[addIdx].f.enter<=step){
        if(mayHit[addIdx].f.exit>=step){
          inire(mayHit[addIdx].o,1);
          active.Add(mayHit[addIdx]);
        }
        addIdx++;
      }

      bool flag = false;
      if(hs) flag |= srast.prog(p,step,dosolids,dowalljumps);
      if(flag||exitNormal.OrShortcircuit(p)) return true;
      if(doholdables) flag |= hrast.prog(p,step);
      if(doplayercolliders) flag |= crast.prog(p,step);
      if(dotriggers) flag |= trast.prog(p,step);
      return flag;
    }
    //This function is what someone deeply scared of floating point misrepresentation writes.
    public float stepMagn(ref float current, float max){
      float dif = max-current;
      if(max-current<=1){
        current = max;
        return dif;
      }
      if(wholeroom || active.Count != 0){
        current=current+1;
        return 1;
      }
      if(addIdx>=mayHit.Count){
        current = max;
        return dif;
      }
      max = MathF.Max(1,MathF.Min(mayHit[addIdx].f.enter,max));
      dif = max-current;
      current = max;
      return dif;
    }
    public bool useSteps=>wholeroom||mayHit.Count>0;
  }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public struct ACol<T>{
  public FloatRect.FRCollision f;
  public T o;
  public int order;
  public ACol(FloatRect.FRCollision info, T col){
    f=info; o=col;
  }
}
public abstract class LinearRaster<T>{
  public LinkedList<ACol<T>> active = new();
  public List<ACol<T>> mayHit = new();
  public int addIdx = 0;
  public void Fill(IEnumerable<ACol<T>> l, float maxt){
    var idx = 0;
    var enu = l.GetEnumerator();
    while(enu.MoveNext()){
      var col = enu.Current;
      if(col.f.collides && col.f.enter<maxt){
        col.order=idx++;
        mayHit.Add(col);
      }
    }
    mayHit.Sort((a,b)=>{
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
    var cn = active.First;
    if((cn==null) && (addIdx>=mayHit.Count || mayHit[addIdx].f.enter>step)) return false;
    while(cn!=null){
      if(cn.Value.f.exit<step) active.Remove(cn);
      cn=cn.Next;
    }
    while(addIdx<mayHit.Count && mayHit[addIdx].f.enter<=step){
      if(mayHit[addIdx].f.exit>=step) active.AddLast(mayHit[addIdx]);
      addIdx++;
    }
    //DebugConsole.Write($"{this.GetType().ToString()} {active.Count} {step}");
    return true;
  }
}

public class QuickCollider<T>{
  List<(FloatRect, T)> items = new();
  FloatRect bounds = FloatRect.empty;
  public int Count=>items.Count;
  public void Add(T item, FloatRect itemBounds){
    items.Add(new(itemBounds,item));
    bounds = bounds._union(itemBounds);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IEnumerable<T> Test(FloatRect test){
    if(!bounds.CollideFr(test)) yield break;
    foreach(var i in items) if(i.Item1.CollideFr(test)) yield return i.Item2;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public IEnumerable<T> TestPoint(Vector2 test){
    if(!bounds.CollidePoint(test)) yield break;
    foreach(var i in items) if(i.Item1.CollidePoint(test)) yield return i.Item2;
  }
}

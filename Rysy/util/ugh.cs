

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Rysy.Graphics;
using Rysy.Shared;

namespace auspicioushelper.Rysy;
public static partial class Util{
  public class QuickCollider<T>{
    List<(IntRect, T)> items = new();
    IntRect bounds = IntRect.empty;
    public int Count=>items.Count;
    public void Add(T item, IntRect itemBounds){
      items.Add(new(itemBounds,item));
      bounds = bounds.union_(itemBounds);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> Test(IntRect test){
      if(!bounds.CollideIr(test)) yield break;
      foreach(var i in items) if(i.Item1.CollideIr(test)) yield return i.Item2;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> TestPoint(Vector2 test){
      if(!bounds.CollidePoint(test)) yield break;
      foreach(var i in items) if(i.Item1.CollidePoint(test)) yield return i.Item2;
    }
  }
  public static List<T2> Map<T1,T2>(this List<T1> li, Func<T1,T2> pred){
    List<T2> l = new();
    foreach(var x in li) l.Add(pred(x));
    return l;
  }
  public static T2 ReduceMapI<T1,T2>(this List<T1> list, Func<T1,T2> map, Func<T2,T2,T2> reduce){
    T2 initial = map(list[0]);
    for(int i=1; i<list.Count; i++) initial = reduce(initial,map(list[i]));
    return initial;
  }
  //Assumes that rightmost dimension is 
  public static void FillRect<T>(this T[,] li, Int2 tlc, Int2 brc, T item){
    brc = Int2.Min(brc,new Int2(li.GetLength(0),li.GetLength(1)));
    for(int x=Math.Max(0,tlc.x); x<brc.x; x++) for(int y=Math.Max(0,tlc.y); y<brc.y; y++) li[x,y]=item;
  }
  public static T GetOrDefault<T>(this IList<T> li, int idx, T def=default){
    if(idx>=li.Count || idx<0) return def;
    return li[idx];
  }
  public const BindingFlags GoodBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
}



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
public static partial class Util{
  public class EnumeratorStack<T>{
    Stack<IEnumerator> stack = new();
    public EnumeratorStack(IEnumerator e){
      stack.Push(e);
    }
    public T Next(){
      if(stack.Count==0) return default(T);
      if(stack.Peek().MoveNext()){
        var val = stack.Peek().Current;
        if(val is IEnumerator en){
          stack.Push(en);
          return Next();
        } else if(val is T res){
          return res;
        }
        return Next();
      } else {
        stack.Pop();
        return Next();
      }
    }
    public List<T> toList(){
      List<T> l=new(); 
      while(Next() is {} e) l.Add(e);
      return l;
    }
  }

  public class FunctionList<T1>{
    List<Func<T1,bool>> funcs = new();
    public int Count=>funcs.Count;
    public enum InvocationMode{
      And, AndShortcircut, Or, OrShortcircuit
    }
    InvocationMode mode;
    public FunctionList(InvocationMode mode = InvocationMode.OrShortcircuit, params Func<T1,bool>[] initial){
      this.mode = mode;
      foreach(var fn in initial) Add(fn);
    }
    public void Add(Func<T1,bool> n){
      if(!funcs.Contains(n))funcs.Add(n);
    }
    public void Remove(Func<T1,bool> n)=>funcs.Remove(n);
    public bool AndShortcircut(T1 p){
      foreach(var f in funcs) if(!f(p)) return false;
      return true;
    }
    public bool And(T1 p){
      bool res = true;
      foreach(var f in funcs) res=res&&f(p);
      return res;
    }
    public bool OrShortcircuit(T1 p){
      foreach(var f in funcs) if(f(p)) return true;
      return false;
    }
    public bool Or(T1 p){
      bool res = false;
      foreach(var f in funcs) res = res||f(p);
      return res; 
    }
    public bool Invoke(T1 p){
      switch(mode){
        case InvocationMode.And: return And(p);
        case InvocationMode.AndShortcircut: return AndShortcircut(p);
        case InvocationMode.Or: return Or(p);
        default: return OrShortcircuit(p);
      }
    }
    public static implicit operator Func<T1, bool>(FunctionList<T1> fl) => fl.Invoke;
  }

  public class FastTrie<T>(bool alwaysClean=false){
    struct TrieLine{
      public T end, starend;
      public int edgeIndex;
      public short numStarEdge, numEdge;
      public bool hasEnd, hasStarend;
    }
    TrieLine[] items;
    (char,ushort)[] edges;
    Dictionary<string, T> record = new();
    Dictionary<(ushort, char), ushort> overflow = new();
    bool dirty=false;

    //Gets best match
    public bool TryGet(string s, out T res){
      if(alwaysClean) s=s.AsClean();
      if(dirty) Build();
      List<ushort> activeA = new(0);
      List<ushort> intoA = new();
      List<(char,ushort)> stars = new();
      HashSet<ushort> includedStars = new();
      res = default;
      bool foundAny = false;
      foreach(var c in s){
        foreach(var (o, idx) in stars){
          if(c==o) intoA.Add(idx);
        }
        foreach(var idx in activeA){
          var a = items[idx];
          if(a.hasStarend){
            res = a.starend;
            foundAny = true;
          }
          int tindex = a.edgeIndex;
          for(int i=0; i<a.numStarEdge; i++){
            var j = edges[tindex++];
            if(includedStars.Contains(j.Item2)) continue;
            stars.Add(j);
            includedStars.Add(j.Item2);
          }

          if(a.numEdge<0){
            if(overflow.TryGetValue(new(idx, c), out var nidx)) intoA.Add(nidx);
          } else for(int i=0; i<a.numEdge; i++){
            var j = edges[tindex++];
            if(c==j.Item1){
              intoA.Add(j.Item2);
              break;
            }
          }
        }
        var temp = activeA;
        temp.Clear();
        activeA = intoA;
        intoA = temp;
      }

      foreach(var idx in activeA){
        var a = items[idx];
        if(a.hasStarend){
          res = a.starend;
          foundAny = true;
        }
        if(a.hasEnd){
          res = a.end;
          foundAny = true;
        }
      }
      return foundAny;
    }
    public IEnumerator<T> getAll(string s){
      if(alwaysClean) s=s.AsClean();
      if(dirty) Build();
      List<ushort> activeA = new(0);
      List<ushort> intoA = new();
      List<(char,ushort)> stars = new();
      HashSet<ushort> includedStars = new();
      foreach(var c in s){
        foreach(var (o, idx) in stars){
          if(c==o) intoA.Add(idx);
        }
        foreach(var idx in activeA){
          var a = items[idx];
          if(a.hasStarend) yield return a.starend;
          int tindex = a.edgeIndex;
          for(int i=0; i<a.numStarEdge; i++){
            var j = edges[tindex++];
            if(includedStars.Contains(j.Item2)) continue;
            stars.Add(j);
            includedStars.Add(j.Item2);
          }
          
          if(a.numEdge<0){
            if(overflow.TryGetValue(new(idx, c), out var nidx)) intoA.Add(nidx);
          } else for(int i=0; i<a.numEdge; i++){
            var j = edges[tindex++];
            if(c==j.Item1){
              intoA.Add(j.Item2);
              break;
            }
          }
        }
        var temp = activeA;
        activeA.Clear();
        activeA = intoA;
        intoA = temp;
      }

      foreach(var idx in activeA){
        var a = items[idx];
        if(a.hasStarend) yield return a.starend;
        if(a.hasEnd) yield return a.end;
      }
    }
    public void Add(string s, T val){
      if(alwaysClean) s=s.AsClean();
      record.Add(s,val);
      dirty=true;
    }
    public void SetAll(IEnumerable<string> strings, T value){
      foreach(var s in strings) record[alwaysClean?s.AsClean():s] = value;
      dirty=true;
    }
    public T GetOrDefault(string s)=>TryGet(alwaysClean? s.AsClean():s, out var r)? r:default;
    public void Clear(){
      record.Clear();
      dirty=true;
    }
    public bool hasStuff=>record.Count>0;

    class BuildNode{
      public ushort idx;
      public short numedge, numstaredge;
      public bool hasEnd, hasStarend;
      public T end, starEnd;

    }
    struct BuildEdge{
      public char c;
      public BuildNode from,to;
      public bool star;
    }
    const int MaxListEdge=7;
    void Build(){
      dirty=false;
      ushort count=1;
      Dictionary<string,BuildNode> map = new(){{"",new()}};
      if(record.TryGetValue("", out var emptyVal)) map[""]=new(){hasEnd=true, end=emptyVal};

      foreach(var (s,val) in record){
        for(int i=1; i<=s.Length; i++){
          bool last = i==s.Length;
          var sub = s.Substring(0,i);
          if(sub[^1]=='*'){
            if(sub.Length>=2 && sub[^2]=='*' && auspicioushelperModule.InFolderMod){
              throw new Exception($"consecutive asterisks illegal: {s}");
            }
            if(last){
              var fidx = map.GetValueOrDefault(s.Substring(0,i-1));
              fidx.hasStarend = true;
              fidx.starEnd = val;
            }
            continue;
          }
          if(!map.ContainsKey(sub)){
            map.Add(sub, new(){idx=count++, hasEnd=last, end=last?val:default});
            if(count==0) throw new Exception("too many");
          }
        }
      }
      List<BuildEdge> build = new();
      foreach(var (s,idx) in map){
        if(s.Length==0) continue;
        bool staredge = s.Length>-2 && s[^2]=='*';
        var fidx = map.GetValueOrDefault(s.Substring(0, s.Length-(staredge?2:1)));
        if(staredge) fidx.numstaredge++;
        else fidx.numedge++;
        build.Add(new(){c=s[^1], from=fidx, to=idx, star=staredge});
      }

      int edgesLen=0;
      items = new TrieLine[count];
      foreach(var (s,idx) in map){
        var shouldoverflow = idx.numedge>MaxListEdge;
        items[idx.idx] = new(){
          hasEnd=idx.hasEnd, end=idx.end,
          hasStarend=idx.hasStarend, starend=idx.starEnd,
          edgeIndex=edgesLen, numStarEdge=idx.numstaredge,
          numEdge=shouldoverflow? (short)-1:idx.numedge
        };
        edgesLen+=idx.numstaredge;
        if(!shouldoverflow) edgesLen+=idx.numedge;
      }

      edges = new (char,ushort)[edgesLen];
      overflow.Clear();
      foreach(var edge in build){
        var fidx = edge.from;
        var from = items[fidx.idx];
        if(from.numEdge<0 && !edge.star) overflow.Add(new(fidx.idx, edge.c), edge.to.idx);
        else{
          var idx=from.edgeIndex;
          if(edge.star) idx += from.numStarEdge - (fidx.numstaredge--);
          else idx += from.numStarEdge + from.numEdge - (fidx.numedge--);
          edges[idx] = new(edge.c, edge.to.idx);
        }
      }
    }
  }


  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static CollisionDirection getCollisionDir(Vector2 move){
    CollisionDirection dir = CollisionDirection.yes;
    if(move.X<0) dir |= CollisionDirection.right;
    else if(move.X>0) dir |= CollisionDirection.left;
    if(move.Y<0) dir |= CollisionDirection.down;
    else if(move.Y>0) dir |= CollisionDirection.up;
    return dir;
  }
  public static Vector2 randomizeQuadrent(Vector2 inp){
    inp.X*=Calc.Random.Chance(0.5f)?1:-1;
    inp.Y*=Calc.Random.Chance(0.5f)?1:-1;
    return inp;
  }
  public static float randomizeAngleQuad(float angle){
    if(Calc.Random.Chance(0.5f)) angle=-angle;
    if(Calc.Random.Chance(0.5f)) angle=MathF.PI-angle;
    return angle;
  }
  public static float Clamp(float v, float min, float max){
    if(v<min) return min;
    if(v>max) return max;
    return v;
  }
  public static float Saturate(float v)=>Math.Clamp(v,0,1);
  public static double Saturate(double v)=>Math.Clamp(v,0,1);

  public class HybridSet<T>:ISet<T>{
    const int threshold = 8;
    HashSet<T> set = null;
    List<T> list = null;
    public HybridSet(){
      list = new(threshold);
    }
    public HybridSet(IEnumerable<T> o){
      if (o is ICollection<T> coll && coll.Count > threshold){
        set = new(o);
        return;
      }
      list = new(threshold);
      foreach(T e in o) Add(e);
    }
    bool usingSet=>set!=null;
    public bool IsReadOnly=>false;
    public bool Add(T elem){
      if(usingSet) return set.Add(elem);
      else {
        if(!list.Contains(elem)){
          if(list.Count>=threshold){
            set = new(list);
            list = null;
            return set.Add(elem);
          } else {
            list.Add(elem);
            return true;
          }
        } else return false;
      }
    }
    void ICollection<T>.Add(T elem)=>Add(elem);
    public bool Remove(T elem){
      if(usingSet){
        bool res = set.Remove(elem);
        if(set.Count<threshold/2-1){
          list = new(set);
          set = null;
        }
        return res;
      } else return list.Remove(elem);
    }
    public int Count=>usingSet?set.Count:list.Count;
    public void Clear(){
      if(usingSet){
        set = null;
        list = new(threshold);
      } else list.Clear();
    }
    public bool Contains(T elem)=>usingSet?set.Contains(elem):list.Contains(elem);
    public void CopyTo(T[] arr, int index){
      if(usingSet) set.CopyTo(arr, index);
      else list.CopyTo(arr,index);
    }
    public void ExceptWith(IEnumerable<T> o){
      if(usingSet) set!.ExceptWith(o);
      else{
        var os = o as ISet<T> ?? new HashSet<T>(o);
        list.RemoveAll(os.Contains);
      }
    }
    public bool IsProperSubsetOf(IEnumerable<T> other)=>usingSet?set.IsProperSubsetOf(other) : new HashSet<T>(list).IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<T> other)=>usingSet?set.IsProperSupersetOf(other) : new HashSet<T>(list).IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<T> other)=>usingSet?set.IsSubsetOf(other) : new HashSet<T>(list).IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<T> other)=>usingSet?set.IsSupersetOf(other) : new HashSet<T>(list).IsSupersetOf(other);
    public bool Overlaps(IEnumerable<T> o){
      if(usingSet) return set.Overlaps(o);
      else {
        var os = o as ISet<T> ?? new HashSet<T>(o);
        return list.Any(os.Contains);
      }
    }
    public bool SetEquals(IEnumerable<T> other)=>usingSet?set.SetEquals(other) : new HashSet<T>(list).SetEquals(other);
    public void IntersectWith(IEnumerable<T> o){
      if(usingSet) set.IntersectWith(o);
      else {
        List<T> nlist = new();
        foreach(T x in o){
          if(list.Contains(x) && !nlist.Contains(x)) nlist.Add(x);
        }
        list = nlist;
      }
    }
    void promoteSet(){
      if(!usingSet){
        set = new(list);
        list = null;
      }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void tryDemote(){
      if(usingSet && set.Count<threshold/2-1){
        list = new(set);
        set=null;
      }
    }
    public void SymmetricExceptWith(IEnumerable<T> o){
      promoteSet();
      set.SymmetricExceptWith(o);
      tryDemote();
    }
    public void UnionWith(IEnumerable<T> o){
      promoteSet();
      set.UnionWith(o);
      tryDemote();
    }
    public IEnumerator<T> GetEnumerator()=>usingSet?set.GetEnumerator():list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IReadOnlyList<T> GetList(){
      if(usingSet) return set.ToArray();
      return list;
    }
  }
  public class OrderedSet<T>:IEnumerable<T>{
    HashSet<T> set = new();
    List<T> list = new();
    public void Add(T n){
      if(!set.Contains(n)){
        set.Add(n); list.Add(n);
      }
    }
    public bool Contains(T n)=>set.Contains(n);
    IEnumerator<T> IEnumerable<T>.GetEnumerator()=>list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()=>list.GetEnumerator();
    public int Count=>list.Count;
  }
  public static int HigherPow2(int v){
    v|=v>>1;
    v|=v>>2;
    v|=v>>4;
    v|=v>>8;
    v|=v>>16;
    return v+1;
  }
  public ref struct RingDeque<T>{
    int head=0;
    int tail;
    int mask;
    Span<T> data;
    public RingDeque(Span<T> span){
      data = span;
      tail = mask = span.Length-1;
      if((mask & span.Length)!=0) throw new Exception("Ring deque buffer size must be power of 2");
    }
    public T Head()=>data[head];
    public T Tail()=>data[tail];
    public int Count=>(tail+1-head)&mask;
    public T Dequeue(){
      T ret = data[head];
      head=(head+1)&mask;
      return ret;
    }
    public void Push(T val){
      tail = (tail+1)&mask;
      data[tail]=val;
    }
    public T Pop(){
      T ret = data[tail];
      tail = (tail-1)&mask;
      return ret;
    }
  }
  public class SetStack<T>{
    public struct Handle{
      public readonly LinkedListNode<T> val;
      public readonly SetStack<T> origin;
      public Handle(LinkedListNode<T> v, SetStack<T> o){
        val=v; origin=o;
      }
      public void Remove()=>origin.Remove(this);
    }
    LinkedList<T> things = new();
    public int Count=>things.Count;
    public Handle Push(T item){
      return new(things.AddLast(item),this);
    }
    public T Pop(){
      if(things.Count==0) throw new Exception("Popping from empty stack");
      var n = things.Last();
      things.RemoveLast();
      return n;
    }
    public T Peek(){
      if(things.Count==0) throw new Exception("Peaking into empty stack");
      return things.Last();
    }
    public T PeekOrDefault()=>things.Count()==0?default:things.Last();
    public void Remove(Handle h){
      things.Remove(h.val);
    }
  }
  public static void AddMultiple<T1,T2>(this Dictionary<T1,T2> dict, IEnumerable<T1> k, T2 v){
    foreach(var key in k) dict.Add(key,v);
  }

  public ref struct AutoRestore<T>:IDisposable{
    ref T reference;
    T oldValue;
    public AutoRestore(ref T toRestore){
      reference = ref toRestore;
      oldValue = reference;
    }
    public AutoRestore(ref T toRestore, T nval):this(ref toRestore){
      reference = nval;
    }
    void IDisposable.Dispose() {
      reference=oldValue;
    }
  }
  public static AutoRestore<T> WithRestore<T>(ref T toRestore){
    return new AutoRestore<T>(ref toRestore);
  }
  public static AutoRestore<T> WithRestore<T>(ref T toRestore, T nval){
    return new AutoRestore<T>(ref toRestore, nval);
  }
  public struct RingBuffer<T>{
    T[] items;
    int loopidx;
    public ref T this[int idx]{
      get=>ref items[(idx+loopidx)%items.Length];
    }
    public void Backward()=>loopidx = (items.Length+loopidx-1)%items.Length;
    public void Forward()=>loopidx = (loopidx+1)%items.Length;
    public RingBuffer(T[] dat)=>items=dat;
    public RingBuffer(int capacity):this(new T[capacity]){}
  }
  public ref struct FiniteStack<T>{
    Span<T> items;
    int top=-1;
    public ref T Top=>ref items[top];
    public bool Empty=>top==-1;
    public void Push(T item)=>items[++top]=item;
    public T Pop()=>items[top--];
    public void Emplace()=>top++;
    public FiniteStack(Span<T> dat)=>items=dat;
    public FiniteStack(int capacity):this(new T[capacity]){}
  }
  public class MultiDisposable:IDisposable{
    List<IDisposable> toDispose=null;
    public IDisposable Add(IDisposable d){
      if(toDispose==null) toDispose=new();
      toDispose.Add(d);
      return this;
    }
    void IDisposable.Dispose() {
      if(toDispose!=null) foreach(var d in toDispose) d.Dispose();
    }
  }
  // public class SomeList<T>{
  //   List<T> items = new();
  //   HashSet<T> toRemove = new();
  //   List<T> deferredAdd = new(0);
  //   List<T> deferredRemove = new(0);
  //   bool locked;
  //   void Catchup(){
  //     if(deferredAdd.Count==0 && deferredRemove.Count==0) return;
  //     foreach(var v in deferredAdd) Add(v);
  //     deferredAdd.Clear();
  //     foreach(var v in deferredRemove) Remove(v);
  //     deferredRemove.Clear();
  //   }
  // }
  public ref struct LazyList<T>{
    public List<T> l = null;
    public LazyList(){}
    public void Add(T item)=>(l??=new()).Add(item);
    public int Count=>l==null?0:l.Count;
    public Enumerator GetEnumerator() => new Enumerator(l);
    public ref struct Enumerator{
      List<T>.Enumerator li;
      bool nonempty;
      public Enumerator(List<T> list){
        nonempty = list != null;
        li = nonempty ? list!.GetEnumerator() : default;
      }
      public T Current => li.Current;
      public bool MoveNext() => nonempty && li.MoveNext();
    }
  }
}
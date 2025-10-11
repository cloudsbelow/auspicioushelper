


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
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
    public enum InvocationMode{
      And, AndShortcircut, Or, OrShortcircuit
    }
    InvocationMode mode;
    public FunctionList(InvocationMode mode = InvocationMode.OrShortcircuit){
      this.mode = mode;
    }
    public void Add(Func<T1,bool> n)=>funcs.Add(n);
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

  public class Trie{
    class TrieNode{
      Dictionary<char, TrieNode> n;
      public bool isEnding {get; private set;}=false;
      public bool isSuperEnding=false;
      public bool Test(char c, ref TrieNode next)=>(!isSuperEnding)&&(n?.TryGetValue(c,out next)??false);
      public void Add(string s, int idx){
        if(idx<s.Length){
          if(n==null) n=new();
          if(s[idx]=='*')isSuperEnding=true;
          if(!n.TryGetValue(s[idx], out var next)){
            n.Add(s[idx],next = new TrieNode());
          }
          next.Add(s,idx+1);
        } else isEnding = true;
      }
    }
    TrieNode root = new TrieNode();
    public void Add(string s)=>root.Add(s,0);
    public bool Test(string s){
      if(s == null) return false;
      TrieNode cur = root;
      int idx = 0;
      do{
        if(idx==s.Length) return cur.isEnding;
        if(s[idx]=='*') return true;
      } while(cur.Test(s[idx++],ref cur));
      return cur?.isSuperEnding??false;
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
}
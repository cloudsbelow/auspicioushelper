


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
    public FunctionList(InvocationMode mode = InvocationMode.OrShortcircuit){
      this.mode = mode;
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
  public class Trie<T>{
    class TrieNode{
      Dictionary<char, TrieNode> n;
      List<(Regex, T, string)> res;
      public T containedItem;
      public bool hasItem = false;
      public bool Get(string s, int idx, out T item){
        if(idx==s.Length){
          item=hasItem?containedItem:default;
          if(res!=null)foreach(var (r,i,orig) in res) if(r.Match("").Success){
            item=i;
            return true;
          }
          return hasItem;
        }
        if(n?.TryGetValue(s[idx], out var a)??false) if(a.Get(s,idx+1,out item)) return true;
        if(res!=null){
          string trunc = s.Substring(idx);
          foreach(var (r,i,orig) in res) if(r.Match(trunc).Success){
            item = i;
            return true;
          }
        } 
        item = default;
        return false;
      }
      public void Add(string s, int idx, T item, bool alwaysOverride){
        if(idx<s.Length){
          if(s[idx] == '*'){
            if(res==null) res=new();
            for(int i=0; i<res.Count; i++) if(res[i].Item3==s){
              if(alwaysOverride){
                res[i] = new(res[i].Item1,item,s);
              } else throw new Exception("Key already present in trie");
            }
            string re = "^";
            for(int i=idx; i<s.Length; i++){
              char c=s[i];
              if(char.IsAsciiLetterOrDigit(c)||c=='-'||c=='_'||c=='\\'||c==' '||c=='\n')re+=c;
              else if(c=='/')re+=@"\/";
              else if(c=='*')re+=".*";
              else throw new Exception("unhandled character in sequence");
            }
            re+="$";
            res.Add(new(new(re,RegexOptions.Compiled),item,s));
            return;
          }
          if(n==null) n=new();
          if(!n.TryGetValue(s[idx], out var next)){
            n.Add(s[idx],next = new TrieNode());
          }
          next.Add(s,idx+1,item, alwaysOverride);
        } else if(hasItem==false || alwaysOverride){
          hasItem=true;
          containedItem=item;
        } else throw new Exception("Key already present in trie");
      }
      public bool HasStuff=>n!=null || res!=null;
    }
    TrieNode root = new TrieNode();
    bool alwaysClean;
    public Trie(bool asClean=false){
      alwaysClean=asClean;
    }
    public void Add(string s, T value)=>   root.Add(alwaysClean?s.AsClean():s, 0, value,false);
    public void Set(string s, T value)=>   root.Add(alwaysClean?s.AsClean():s, 0, value,true);
    public void SetAll(IEnumerable<string> strings, T value){
      foreach(var s in strings) root.Add(alwaysClean?s.AsClean():s, 0, value, true);
    }
    public T GetOrDefault(string s) =>     root.Get(alwaysClean?s.AsClean():s, 0, out var item)? item:default;
    public bool TryGet(string s, out T o)=>root.Get(alwaysClean?s.AsClean():s, 0, out o);
    public void Clear()=>root=new();
    public bool hasStuff=>root.HasStuff;
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
}
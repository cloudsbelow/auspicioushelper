


using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;
public partial class Util{
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
  public static CollisionDirection getCollisionDir(Vector2 move){
    CollisionDirection dir = CollisionDirection.yes;
    if(move.X<0) dir |= CollisionDirection.right;
    else if(move.X>0) dir |= CollisionDirection.left;
    if(move.Y<0) dir |= CollisionDirection.down;
    else if(move.Y>0) dir |= CollisionDirection.up;
    return dir;
  }
}
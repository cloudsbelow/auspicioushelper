


using System;
using System.Collections;
using System.Collections.Generic;

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
}
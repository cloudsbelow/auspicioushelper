


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

public static class ResetEvents{
  public enum RunTimes{
    OnEnter, OnExit, OnReload, OnReset, OnNewScreen
  }
  public static ActionList getList(this RunTimes r){
    return r switch{
      RunTimes.OnEnter=>auspicioushelperModule.OnEnterMap,
      RunTimes.OnExit=>auspicioushelperModule.OnExitMap,
      RunTimes.OnReload=>auspicioushelperModule.OnReloadMap,
      RunTimes.OnNewScreen=>auspicioushelperModule.OnNewScreen,
      RunTimes.OnReset=>auspicioushelperModule.OnReset,
      _=>null
    };
  }
  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
  public class ClearOn:Attribute{
    public RunTimes[] m;
    public bool toNull;
    public ClearOn(params RunTimes[] moments){
      m=moments;
      toNull=false;
    }
    public ClearOn(bool toNull, params RunTimes[] moments){
      m=moments;
      this.toNull=toNull;
    }
  }
  public class NullOn:ClearOn{
    public NullOn(params RunTimes[] moments):base(true,moments){}
  }
  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
  public class RunOn:Attribute{
    public RunTimes[] m;
    public RunOn(params RunTimes[] moments){
      m=moments;
    }
  }
  
  public static void Load(){
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)){
        if(f.IsDefined(typeof(ClearOn))){
          if(!f.IsStatic) DebugConsole.WriteFailure($"{f} can not be autocleared as it is not static",true);
          ClearOn c = (ClearOn)f.GetCustomAttribute(typeof(ClearOn));
          PersistantAction p;
          if(c.toNull){
            Type ty = f.FieldType;
            object setTo = ty.IsValueType? Activator.CreateInstance(ty) : null;
            p = new(()=>f.SetValue(null, setTo),$"ResetEvents.NullOn: {t.Name}.{f.Name}");
          } else{ 
            MethodInfo clearInfo = f.FieldType.GetMethod("Clear");
            if(clearInfo==null) DebugConsole.WriteFailure("Autoclear field is not clearable",true);
            p = new(()=>{
              clearInfo.Invoke(f.GetValue(null),[]);
            },$"ResetEvents.ClearOn: {t.Name}.{f.Name}");
          }
          
          foreach(var r in c.m)r.getList().enroll(p);
        }
      }
      foreach(var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)){
        if(m.IsDefined(typeof(RunOn))){
          if(m.ReturnType!=null){}
          if(!m.IsStatic) DebugConsole.WriteFailure($"{m} cannot be a ResetEvent as it is not static", true);
          RunOn c = (RunOn)m.GetCustomAttribute(typeof(RunOn));
          PersistantAction p = new(()=>{
            m.Invoke(null,[]);
          },$"ResetEvents.RunOn: {t.Name}.{m.Name}()");
          foreach(var r in c.m)r.getList().enroll(p);
        }
      }
    }
  }

  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class LazyLoadDuration:Attribute{
    RunTimes t;
    public LazyLoadDuration(RunTimes unloadTime=RunTimes.OnEnter){
      t=unloadTime;
    }
    public static RunTimes Get(Type t)=>t.GetCustomAttribute(typeof(LazyLoadDuration)) is LazyLoadDuration l? l.t:RunTimes.OnEnter;
  }
  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
  public class LazyThing:Attribute{
    public virtual Action apply(MethodInfo m)=>null;
  }
  public abstract class LazyHook:LazyThing{
    protected string methodStr;
    protected Type ty;
    protected Util.HookTarget mode;
    public LazyHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal){
      methodStr=method;
      this.ty=ty;
      this.mode=mode;
    }
  }
  public class ILHook:LazyHook{
    public ILHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal):base(ty,method,mode){}
    public override Action apply(MethodInfo m){
      if(OnLoad.ILHook.apply(mode, m, ty, methodStr) is not {} h) return null;
      return h.Dispose;
    }
  }
  public class OnHook:LazyHook{
    public OnHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal):base(ty,method,mode){}
    public override Action apply(MethodInfo m){
      if(OnLoad.OnHook.apply(mode, m, ty, methodStr) is not {} h) return null;
      return h.Dispose;
    }
  }
  public class EverestEvent:LazyThing{
    Type ty;
    string evstr;
    public EverestEvent(Type ty, string evstr){
      this.ty=ty; this.evstr = evstr;
    }
    public override Action apply(MethodInfo m) {
      return OnLoad.EverestEvent.apply(m,ty,evstr);
    }
  }
  public class OnEnable:LazyThing{
    public override Action apply(MethodInfo m){
      m.Invoke(null,[]);
      return null;
    }
  }
  public class OnDisable:LazyThing{
    public override Action apply(MethodInfo m){
      return ()=>m.Invoke(null,[]);
    }
  }

  static Dictionary<Type,HookManager> LLstuff = new();
  public static void LazyEnable(Type t){
    if(!LLstuff.TryGetValue(t, out var h)){
      List<Action> onDispose = new();
      LLstuff[t] = h = new(()=>{
        foreach(MethodInfo m in t.GetMethods(Util.GoodBindingFlags)) if(m.IsStatic){
          foreach(var attr in m.GetCustomAttributes<LazyThing>()) if(attr.apply(m) is {} g) onDispose.Add(g);
        }
      },()=>{
        foreach(var a in onDispose) a();
        onDispose.Clear();
      } , LazyLoadDuration.Get(t), $"auto-lazyload {t}");
    } h.enable();
  }
  public static void LazyEnable(params Type[] ts){
    foreach(var t in ts) LazyEnable(t);
  }
  public interface IReloadHooks{
    void Apply();
  }
  public interface IReloadHooks<T>:IReloadHooks{
    void IReloadHooks.Apply(){
      LazyEnable(typeof(T));
    }
  }
  public interface IReloadHooks<T1,T2>:IReloadHooks{
    void IReloadHooks.Apply(){
      LazyEnable(typeof(T1));
      LazyEnable(typeof(T2));
    }
  }
  public interface IReloadHooks<T1,T2,T3>:IReloadHooks{
    void IReloadHooks.Apply(){
      LazyEnable(typeof(T1));
      LazyEnable(typeof(T2));
      LazyEnable(typeof(T3));
    }
  }
}



using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

public static class ResetEvents{
  public static ActionList OnLvlCleanup = new("Final Level Cleanup");
  public static ActionList OnNewAssets = new("Asset Changes");
  public static ActionList OnNewScreen = new("New screen");
  public static ActionList OnLvlReset = new("Level Reset");

  [Flags] //IMPORTANT: these must be listed in descending order of frequency!
  public enum Times{
    LvlReset=8, ChangeScreen=4, NewAssets=2, LvlCleanup=1, None=0
  }
  public static IEnumerable<ActionList> getLists(this Times r){
    if(r.HasFlag(Times.LvlReset)) yield return OnLvlReset;
    if(r.HasFlag(Times.ChangeScreen)) yield return OnNewScreen;
    if(r.HasFlag(Times.NewAssets)) yield return OnNewAssets;
    if(r.HasFlag(Times.LvlCleanup)) yield return OnLvlCleanup; 
  }
  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
  public class ClearOn(Times invokeAt):Attribute{
    public Times times=>invokeAt;
  }
  public class NullOn(Times invokeAt):ClearOn(invokeAt){}


  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
  public class RunOn(Times invokeAt):Attribute{
    public Times times=>invokeAt;
  }
  
  public static void Load(){
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)){
        if(f.IsDefined(typeof(ClearOn))){
          if(!f.IsStatic) DebugConsole.WriteFailure($"{f} can not be autocleared as it is not static",true);
          ClearOn c = (ClearOn)f.GetCustomAttribute(typeof(ClearOn));
          PersistantAction p;
          if(c is NullOn){
            Type ty = f.FieldType;
            object setTo = ty.IsValueType? Activator.CreateInstance(ty) : null;
            p = new(()=>f.SetValue(null, setTo),$"ResetEvents.NullOn: {t.Name}.{f.Name}");
          } else { 
            MethodInfo clearInfo = f.FieldType.GetMethod("Clear");
            if(clearInfo==null) DebugConsole.WriteFailure("Autoclear field is not clearable",true);
            p = new(()=>{
              clearInfo.Invoke(f.GetValue(null),[]);
            },$"ResetEvents.ClearOn: {t.Name}.{f.Name}");
          }
          foreach(var r in getLists(c.times)) r.enroll(p);
        }
      }

      List<(MethodInfo, LazyThing)> items = new();
      foreach(var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)){
        if(m.IsDefined(typeof(RunOn))){
          if(m.ReturnType!=null){}
          if(!m.IsStatic) DebugConsole.WriteFailure($"{m} cannot be a ResetEvent as it is not static", true);
          RunOn c = (RunOn)m.GetCustomAttribute(typeof(RunOn));
          string label = $"ResetEvents.RunOn: {t.Name}.{m.Name}()";
          PersistantAction p = new(m.CreateDelegate<Action>(), label);
          foreach(var r in getLists(c.times)) r.enroll(p);
        }

        foreach(var attr in m.GetCustomAttributes<LazyThing>()){
          items.Add((m,attr));
        }
      }
      if(items.Count>0) lazyClassStuff.Add(t,items);
    }
  }
  static Dictionary<Type, List<(MethodInfo, LazyThing)>> lazyClassStuff = new();
  static Dictionary<Type, Func<Times>> cachedBTypes = new();
  public static class Hooks<T>{
    static bool enabled = false;
    static List<Action> onDispose = null;
    static Times times = Times.None;
    public static Times enable(){
      if(enabled) return times;
      enabled = true;
      // DebugConsole.Write($"Enabling lazy hooks in", typeof(T), lazyClassStuff.GetValueOrDefault(typeof(T))?.Count);

      Times childTimes = Times.None;
      var cur = typeof(T);
      foreach(var i in cur.GetInterfaces()) childTimes |= ensureDep(i);
      while((cur=cur.BaseType)!=null) childTimes |= ensureDep(cur);

      if(lazyClassStuff.TryGetValue(typeof(T), out var list)){
        onDispose = new();
        foreach(var (m,a) in list){
          if(a.apply(m) is {} thing) onDispose.Add(thing);
        }
      } else times = childTimes;

      if(childTimes>times){
        DebugConsole.WriteFailure($"Inheriting class {typeof(T)} resets less frequently than a parent. this aint good.");
        times |= childTimes;
      }
      ScheduledAction s = new(disable, $"disable lazy hooks on {typeof(T)}");
      foreach(var r in getLists(times=LazyLoadDuration.Get(typeof(T)))) r.enroll(s);

      return times;
    }
    static Times ensureDep(Type t){
      if(lazyClassStuff.ContainsKey(t)){
        if(!cachedBTypes.TryGetValue(t, out var fn)){
          Type gt = typeof(Hooks<>).MakeGenericType([t]);
          cachedBTypes.Add(t, fn=gt.GetMethod(nameof(enable)).CreateDelegate<Func<Times>>());
        }
        return fn();
      } else return Times.None;
    }
    static void disable(){
      enabled = false;
      if(onDispose!=null){
        foreach(var a in onDispose) a();
        onDispose.Clear();
      }
    } 
    public static void enableAll(){
      foreach(var (t,m) in lazyClassStuff) ensureDep(t);
    }
  }

  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class LazyLoadDuration(Times newResetTime):Attribute{
    Times times=>newResetTime;
    public static Times Get(Type t)=>t.GetCustomAttribute(typeof(LazyLoadDuration)) is LazyLoadDuration l? l.times:Times.LvlCleanup;
  }
  [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
  public class LazyThing:Attribute{
    public virtual Action apply(MethodInfo m)=>null;
  }
  public abstract class LazyHook:LazyThing{
    protected string methodStr;
    protected Type ty;
    protected Util.HookTarget mode;
    protected Type[] types=null;
    public LazyHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal){
      methodStr=method;
      this.ty=ty;
      this.mode=mode;
    }
    public LazyHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal, params Type[] spec):this(ty,method,mode)=>types=spec;
  }
  public class ILHook:LazyHook{
    public ILHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal):base(ty,method,mode){}
    public override Action apply(MethodInfo m){
      if(Util.applyILHook(mode, m, ty, methodStr, types) is not {} h) return null;
      return h.Dispose;
    }
  }
  public class OnHook:LazyHook{
    public OnHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal):base(ty,method,mode){}
    public override Action apply(MethodInfo m){
      if(Util.ApplyOnhook(mode, m, ty, methodStr, types) is not {} h) return null;
      return h.Dispose;
    }
  }
  public class EverestEvent(Type ty, string evstr):LazyThing{
    public override Action apply(MethodInfo m) {
      return Util.applyEverestEvent(m,ty,evstr);
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

  public interface IReloadHooks{
    void Apply();
  }
  public interface IReloadHooks<T>:IReloadHooks{
    void IReloadHooks.Apply(){
      Hooks<T>.enable();
    }
  }
}
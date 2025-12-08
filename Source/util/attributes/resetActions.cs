


using System;
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
          if(!f.IsStatic) DebugConsole.WriteFailure("Bad",true);
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
          RunOn c = (RunOn)m.GetCustomAttribute(typeof(RunOn));
          PersistantAction p = new(()=>{
            m.Invoke(null,[]);
          },$"ResetEvents.RunOn: {t.Name}.{m.Name}()");
          foreach(var r in c.m)r.getList().enroll(p);
        }
      }
    }
  }
}
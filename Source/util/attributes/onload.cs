


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Field|AttributeTargets.Method, AllowMultiple=true, Inherited=false)]
public class OnLoad:Attribute{
  public OnLoad(){
  }
  public class CustomOnload:OnLoad{
    public virtual void Apply(MethodInfo m){}
    public virtual void Apply(FieldInfo f){}
  }
  public class ILHook:CustomOnload{
    string methodStr;
    Type ty;
    Util.HookTarget mode;
    public ILHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal){
      methodStr=method;
      this.ty=ty;
      this.mode=mode;
    }
    internal static MonoMod.RuntimeDetour.ILHook apply(Util.HookTarget mode, MethodInfo m, Type ty, string methodStr){
      if(mode == Util.HookTarget.Placeholder) return null;
      var p = m.GetParameters();
      if(p.Length!=1 || p[0].ParameterType!=typeof(ILContext) || !m.IsStatic){
        DebugConsole.WriteFailure($"ILHook attr {m} on {ty}.{methodStr} illegal",true);
      }
      MethodInfo methodbase = mode switch {
        Util.HookTarget.Normal=>ty.GetMethod(methodStr, Util.GoodBindingFlags),
        Util.HookTarget.Coroutine=>ty.GetMethod(methodStr, Util.GoodBindingFlags)?.GetStateMachineTarget(),
        Util.HookTarget.PropGet=>ty.GetProperty(methodStr, Util.GoodBindingFlags)?.GetGetMethod(),
        Util.HookTarget.PropSet=>ty.GetProperty(methodStr, Util.GoodBindingFlags)?.GetSetMethod(),
        _=>null
      };
      if(methodbase == null){
        DebugConsole.WriteFailure($"Could not add hook to nonexistent method {ty}.{methodStr} ({mode})",true);
        return null;
      }
      return new MonoMod.RuntimeDetour.ILHook(methodbase, (ILContext ctx)=>m.Invoke(null,[ctx]));
    }
    public override void Apply(MethodInfo m){
      var hook = apply(mode,m,ty,methodStr);
      if(hook == null) return;
      HookManager.cleanupActions.enroll(new ScheduledAction(hook.Dispose, $"dispose ILHOOK<{ty}> {methodStr}"));
    }
  }
  public class OnHook:CustomOnload{
    string methodStr;
    Type ty;
    Util.HookTarget mode;
    public OnHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal){
      methodStr=method;
      this.ty=ty;
      this.mode=mode;
    }
    internal static Hook apply(Util.HookTarget mode, MethodInfo m, Type ty, string methodStr){
      if(mode == Util.HookTarget.Placeholder) return null;
      //DebugConsole.Write($"doing on hook on {ty}.{methodStr} ({mode}) via",m);
      var p = m.GetParameters();
      if(!m.IsStatic){
        DebugConsole.WriteFailure($"On hook attr {m} on {ty}.{methodStr} illegal",true);
      }
      MethodInfo methodbase = mode switch {
        Util.HookTarget.Normal=>ty.GetMethod(methodStr, Util.GoodBindingFlags),
        Util.HookTarget.Coroutine=>ty.GetMethod(methodStr, Util.GoodBindingFlags)?.GetStateMachineTarget(),
        Util.HookTarget.PropGet=>ty.GetProperty(methodStr, Util.GoodBindingFlags)?.GetGetMethod(true),
        Util.HookTarget.PropSet=>ty.GetProperty(methodStr, Util.GoodBindingFlags)?.GetSetMethod(true),
        _=>null
      };
      if(methodbase == null){
        DebugConsole.WriteFailure($"Could not add hook to nonexistent method {ty}.{methodStr} ({mode})",true);
        return null;
      }
      try{
        return new Hook(methodbase, m);
      }catch(Exception ex){
        throw new Exception($"Failed on hook on {ty}.{methodStr} using {m}: ${ex.Message}");
      }
    }
    public override void Apply(MethodInfo m){
      var hook = apply(mode,m,ty,methodStr);
      if(hook == null) return;
      HookManager.cleanupActions.enroll(new ScheduledAction(hook.Dispose, $"dispose ONHOOK<{ty}> {methodStr}"));
    }
  }
  public static void Run(){
    MethodInfo hookm = typeof(HookManager).GetMethod(nameof(HookManager.enable));
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      foreach(FieldInfo f in t.GetFields(Util.GoodBindingFlags)){
        foreach(var c in f.GetCustomAttributes<OnLoad>()){
          if(c is CustomOnload custom) custom.Apply(f);
          else if(!f.IsStatic || f.FieldType!=typeof(HookManager)){
            DebugConsole.WriteFailure($"OnLoad applied to non-static or non-hookmanager: {t} {f}",true);
          } else {
            hookm.Invoke(f.GetValue(null),null);
          }
        }
      }
      foreach(MethodInfo m in t.GetMethods(Util.GoodBindingFlags)){
        foreach(var attr in m.GetCustomAttributes<OnLoad>()){
          if(attr is CustomOnload c) c.Apply(m);
          else if(!m.IsStatic || m.GetParameters().Length!=0){ 
            DebugConsole.WriteFailure($"OnLoad applied to non-static or non-parameterless method {t} {m}",true);
          } else m.Invoke(null,null);
        }
      }
    }
  }
}

public partial class Util{
  public enum HookTarget{
    Normal, Coroutine, PropGet, PropSet, Placeholder
  }
}
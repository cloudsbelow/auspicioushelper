

using System;
using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

public partial class Util{
  public enum HookTarget{
    Normal, Coroutine, PropGet, PropSet, Placeholder
  }
  public static MethodBase GetMethod(Type ty, string methodStr, HookTarget mode, Type[] types){
    if(methodStr == "") return types==null?ty.GetConstructors()[0]:ty.GetConstructor(Util.GoodBindingFlags,types);
    if(types!=null) return mode switch{
      HookTarget.Normal=>ty.GetMethod(methodStr, Util.GoodBindingFlags, types),
      HookTarget.Coroutine=>ty.GetMethod(methodStr, Util.GoodBindingFlags, types)?.GetStateMachineTarget(),
      _=>null
    }; 
    else return mode switch {
      HookTarget.Normal=>ty.GetMethod(methodStr, Util.GoodBindingFlags),
      HookTarget.Coroutine=>ty.GetMethod(methodStr, Util.GoodBindingFlags)?.GetStateMachineTarget(),
      HookTarget.PropGet=>ty.GetProperty(methodStr, Util.GoodBindingFlags)?.GetGetMethod(true),
      HookTarget.PropSet=>ty.GetProperty(methodStr, Util.GoodBindingFlags)?.GetSetMethod(true),
      _=>null
    };
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
  public class WithDetourCtx(int prio=0, string[] before=null, string[] after=null):Attribute{
    public DetourConfig Get()=>new DetourConfig(nameof(auspicioushelper),prio,before,after){};
  }

  internal static Hook ApplyOnhook(HookTarget mode, MethodInfo m, Type ty, string methodStr, Type[] spec=null){
    if(mode == HookTarget.Placeholder) return null;
    var cfg = m.GetCustomAttribute<WithDetourCtx>()?.Get();
    if(!m.IsStatic){
      DebugConsole.WriteFailure($"On hook attr {m} on {ty}.{methodStr} illegal",true);
    }
    MethodBase methodbase = GetMethod(ty,methodStr,mode,spec);
    if(methodbase == null){
      DebugConsole.WriteFailure($"Could not add hook to nonexistent method {ty}.{methodStr} ({mode})",true);
      return null;
    }
    try{
      return new Hook(methodbase, m, cfg);
    }catch(Exception ex){
      throw new Exception($"Failed on hook on {ty}.{methodStr} using {m}: ${ex.Message}");
    }
  }

  internal static ILHook applyILHook(Util.HookTarget mode, MethodInfo m, Type ty, string methodStr, Type[] spec = null){
    if(mode == HookTarget.Placeholder) return null;
    var p = m.GetParameters();
    var cfg = m.GetCustomAttribute<Util.WithDetourCtx>()?.Get();
    if(p.Length!=1 || p[0].ParameterType!=typeof(ILContext) || !m.IsStatic){
      DebugConsole.WriteFailure($"ILHook attr {m} on {ty}.{methodStr} illegal",true);
    }
    MethodBase methodbase = GetMethod(ty,methodStr,mode,spec);
    if(methodbase == null){
      DebugConsole.WriteFailure($"Could not add hook to nonexistent method {ty}.{methodStr} ({mode})",true);
      return null;
    }
    return new ILHook(methodbase, (ILContext ctx)=>m.Invoke(null,[ctx]), cfg);
  }

  internal static Action applyEverestEvent(MethodInfo m, Type t, string ev){
    var evt = t.GetEvent(ev);
    var delType = evt.EventHandlerType;
    var del = Delegate.CreateDelegate(delType,null,m);
    evt.AddEventHandler(null,del);
    return ()=>evt.RemoveEventHandler(null,del);
  }
}
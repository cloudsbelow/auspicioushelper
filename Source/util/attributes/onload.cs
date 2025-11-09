


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Field|AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
public class OnLoad:Attribute{
  public OnLoad(){
  }
  public static void Run(){
    MethodInfo hookm = typeof(HookManager).GetMethod(nameof(HookManager.enable));
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      foreach(FieldInfo f in t.GetFields(Util.GoodBindingFlags)){
        if(!f.IsDefined(typeof(OnLoad))) continue;
        if(!f.IsStatic || f.FieldType!=typeof(HookManager)){
          DebugConsole.WriteFailure($"OnLoad applied to non-static or non-hookmanager: {t} {f}",true);
        }
        else hookm.Invoke(f.GetValue(null),null);
      }
      foreach(MethodInfo m in t.GetMethods(Util.GoodBindingFlags)){
        if(!m.IsDefined(typeof(OnLoad))) continue;
        if(!m.IsStatic || m.GetParameters().Length!=0) DebugConsole.WriteFailure($"OnLoad applied to non-static or non-parameterless method {t} {m}",true);
        else m.Invoke(null,null);
      }
    }
  }
}
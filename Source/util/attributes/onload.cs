


using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Field|AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
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
    public ILHook(Type ty, string method){
      methodStr=method;
      this.ty=ty;
    }
    public override void Apply(MethodInfo m){
      var p = m.GetParameters();
      if(p.Length!=1 || p[0].ParameterType!=typeof(ILContext) || !m.IsStatic){
        DebugConsole.WriteFailure($"ILHook attr {m} on {ty}.{methodStr} illegal",true);
      }
      var hook = new MonoMod.RuntimeDetour.ILHook(ty.GetMethod(methodStr), (ILContext ctx)=>m.Invoke(null,[ctx]));
      HookManager.cleanupActions.enroll(new ScheduledAction(hook.Dispose, $"ILHOOK<{ty}> {methodStr}"));
    }
  }
  public static void Run(){
    MethodInfo hookm = typeof(HookManager).GetMethod(nameof(HookManager.enable));
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      foreach(FieldInfo f in t.GetFields(Util.GoodBindingFlags)){
        if(!f.IsDefined(typeof(OnLoad))) continue;
        if(f.GetCustomAttribute(typeof(OnLoad)) is CustomOnload c){
          c.Apply(f);
        } else {
          if(!f.IsStatic || f.FieldType!=typeof(HookManager)){
            DebugConsole.WriteFailure($"OnLoad applied to non-static or non-hookmanager: {t} {f}",true);
          }
          else hookm.Invoke(f.GetValue(null),null);
        }
      }
      foreach(MethodInfo m in t.GetMethods(Util.GoodBindingFlags)){
        if(!m.IsDefined(typeof(OnLoad))) continue;
        if(m.GetCustomAttribute(typeof(OnLoad)) is CustomOnload c){
          c.Apply(m);
        } else {
          if(!m.IsStatic || m.GetParameters().Length!=0) DebugConsole.WriteFailure($"OnLoad applied to non-static or non-parameterless method {t} {m}",true);
          else m.Invoke(null,null);
        }
      }
    }
  }
}
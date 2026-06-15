


using System;
using System.Reflection;

namespace Celeste.Mod.auspicioushelper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=false)]
public class OnLoad:Attribute{
  public OnLoad(){
  }
  public class CustomOnload:OnLoad{
    public virtual void Apply(MethodInfo m){}
    public virtual void Apply(FieldInfo f){}
  }
  public abstract class LoadHook:CustomOnload{
    protected string methodStr;
    protected Type ty;
    protected Util.HookTarget mode;
    protected Type[] types=null;
    public LoadHook(Type ty, string method, Util.HookTarget mode = Util.HookTarget.Normal){
      methodStr=method;
      this.ty=ty;
      this.mode=mode;
    }
    public LoadHook(Type ty, string method, Util.HookTarget mode=Util.HookTarget.Normal, params Type[] spec):
      this(ty,method,mode)=>types=spec;
  }
  public class ILHook:LoadHook{
    public ILHook(Type ty, string method, Util.HookTarget mode=Util.HookTarget.Normal, params Type[] spec):
      base(ty,method,mode,spec){}
    public ILHook(Type ty, string method, Util.HookTarget mode=Util.HookTarget.Normal):
      base(ty,method,mode){}
    public override void Apply(MethodInfo m){
      if(Util.applyILHook(mode,m,ty,methodStr,types) is not {} hook) return;
      HookManager.cleanupActions.enroll(hook.Dispose, $"{ty}.{methodStr}: dispose ilhook {m}");
    }
  }
  public class OnHook:LoadHook{
    public OnHook(Type ty, string method, Util.HookTarget mode=Util.HookTarget.Normal, params Type[] spec):
      base(ty,method,mode,spec){}
    public OnHook(Type ty, string method, Util.HookTarget mode=Util.HookTarget.Normal):
      base(ty,method,mode){}
    public override void Apply(MethodInfo m){
      if(Util.ApplyOnhook(mode,m,ty,methodStr,types) is not {} hook) return;
      HookManager.cleanupActions.enroll(hook.Dispose, $"{ty}.{methodStr}: dispose onhook {m}");
    }
  }
  public class EverestEvent(Type type, string evstr):CustomOnload{
    public override void Apply(MethodInfo m){
      if(Util.applyEverestEvent(m,type,evstr) is not {} ev) return;
      HookManager.cleanupActions.enroll(ev, $"{type}.{evstr} dispose everest event {m}");
    }
  }
  public static void Run(){
    MethodInfo hookm = typeof(HookManager).GetMethod(nameof(HookManager.enable));
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      foreach(MethodInfo m in t.GetMethods(Util.GoodBindingFlags | BindingFlags.DeclaredOnly)){
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
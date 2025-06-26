


using System;
using System.Linq;
using System.Linq.Expressions;

namespace Celeste.Mod.auspicioushelper;

public static class CursedIopHelper{
  public static Type getType(string modulename, string path){
    return Everest.Modules.FirstOrDefault(m=>m.Metadata.Name == modulename)?.GetType().Assembly.GetType(path);
  }
  public static bool isLoaded(string modulename)=>Everest.Modules.FirstOrDefault(m=>m.Metadata.Name==modulename)!=null;
  public static Action<T> instanceAction<T>(string methodname){
    var method = typeof(T).GetMethod(methodname,Type.EmptyTypes);
    if(method == null){
      DebugConsole.Write($"Could not construct {typeof(T)}.{methodname}()");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var call = Expression.Call(inst, method);
    return Expression.Lambda<Action<T>>(call,inst).Compile();
  } 
  public static Action<T,T1> instanceAction<T,T1>(string methodname){
    var method = typeof(T).GetMethod(methodname,[typeof(T1)]);
    if(method == null){
      DebugConsole.Write($"Could not construct {typeof(T)}.{methodname}({typeof(T1)})");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var call = Expression.Call(inst, method, arg1);
    return Expression.Lambda<Action<T,T1>>(call,inst,arg1).Compile();
  } 
  public static Action<T,T1,T2> instanceAction<T,T1,T2>(string methodname){
    var method = typeof(T).GetMethod(methodname,[typeof(T1),typeof(T2)]);
    if(method == null){
      DebugConsole.Write($"Could not construct {typeof(T)}.{methodname}({typeof(T1)},{typeof(T2)})");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var call = Expression.Call(inst, method, arg1,arg2);
    return Expression.Lambda<Action<T,T1,T2>>(call,inst,arg1,arg2).Compile();
  } 
  public static Action<T,T1,T2,T3> instanceAction<T,T1,T2,T3>(string methodname){
    var method = typeof(T).GetMethod(methodname,[typeof(T1),typeof(T2),typeof(T3)]);
    if(method == null){
      DebugConsole.Write($"Could not construct {typeof(T)}.{methodname}({typeof(T1)},{typeof(T2)},{typeof(T3)})");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var arg3 = Expression.Parameter(typeof(T3), "arg3");
    var call = Expression.Call(inst, method, arg1,arg2,arg3);
    return Expression.Lambda<Action<T,T1,T2,T3>>(call,inst,arg1,arg2,arg3).Compile();
  } 
  public static Func<T, TResult> instanceFunc<T, TResult>(string methodname){
    var method = typeof(T).GetMethod(methodname,[]);
    if(method == null){
      DebugConsole.Write($"Could not construct ({typeof(TResult)}){typeof(T)}.{methodname}()");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var call = Expression.Call(inst, method);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<T, TResult>>(conv,inst).Compile();
  }
  public static Func<T, T1, TResult> instanceFunc<T, T1, TResult>(string methodname){
    var method = typeof(T).GetMethod(methodname,[typeof(T1)]);
    if(method == null){
      DebugConsole.Write($"Could not construct ({typeof(TResult)}){typeof(T)}.{methodname}({typeof(T1)})");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var call = Expression.Call(inst, method, arg1);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<T, T1, TResult>>(conv,inst,arg1).Compile();
  }
  public static Func<T, T1, T2, TResult> instanceFunc<T, T1, T2, TResult>(string methodname){
    var method = typeof(T).GetMethod(methodname,[typeof(T1),typeof(T2)]);
    if(method == null){
      DebugConsole.Write($"Could not construct ({typeof(TResult)}){typeof(T)}.{methodname}({typeof(T1)},{typeof(T2)})");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var call = Expression.Call(inst, method, arg1);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<T, T1, T2, TResult>>(conv,inst,arg1,arg2).Compile();
  }
  public static Func<T, T1, T2, T3, TResult> instanceFunc<T, T1, T2, T3, TResult>(string methodname){
    var method = typeof(T).GetMethod(methodname,[typeof(T1),typeof(T2), typeof(T3)]);
    if(method == null){
      DebugConsole.Write($"Could not construct ({typeof(TResult)}){typeof(T)}.{methodname}({typeof(T1)},{typeof(T2)},{typeof(T3)})");
      return null;
    }
    var inst = Expression.Parameter(typeof(T), "inst");
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var arg3 = Expression.Parameter(typeof(T3), "arg3");
    var call = Expression.Call(inst, method, arg1);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<T, T1, T2, T3, TResult>>(conv,inst,arg1,arg2,arg3).Compile();
  }
}
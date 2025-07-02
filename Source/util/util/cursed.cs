


using System;
using System.Linq;
using System.Linq.Expressions;

namespace Celeste.Mod.auspicioushelper;

public static partial class Util{
  public static Type getModdedType(string modulename, string path){
    return Everest.Modules.FirstOrDefault(m=>m.Metadata.Name == modulename)?.GetType().Assembly.GetType(path);
  }
  public static bool isLoaded(string modulename)=>Everest.Modules.FirstOrDefault(m=>m.Metadata.Name==modulename)!=null;
  public static Action<object> instanceAction(Type T, string methodname){
    var method = T.GetMethod(methodname,[]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct {T}.{methodname}()");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var call = Expression.Call(inst, method);
    return Expression.Lambda<Action<object>>(call,obj).Compile();
  } 
  public static Action<object, T1> instanceAction<T1>(Type T, string methodname){
    var method = T.GetMethod(methodname,[typeof(T1)]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct {T}.{methodname}({typeof(T1)})");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var call = Expression.Call(inst, method, arg1);
    return Expression.Lambda<Action<object,T1>>(call,obj,arg1).Compile();
  } 
  public static Action<object, T1,T2> instanceAction<T1,T2>(Type T, string methodname){
    var method = T.GetMethod(methodname,[typeof(T1),typeof(T2)]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct {T}.{methodname}({typeof(T1)},{typeof(T2)})");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var call = Expression.Call(inst, method, arg1,arg2);
    return Expression.Lambda<Action<object,T1,T2>>(call,obj,arg1,arg2).Compile();
  } 
  public static Action<object, T1,T2,T3> instanceAction<T1,T2,T3>(Type T, string methodname){
    var method = T.GetMethod(methodname,[typeof(T1),typeof(T2),typeof(T3)]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct {T}.{methodname}({typeof(T1)},{typeof(T2)},{typeof(T3)})");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var arg3 = Expression.Parameter(typeof(T3), "arg3");
    var call = Expression.Call(inst, method, arg1,arg2,arg3);
    return Expression.Lambda<Action<object,T1,T2,T3>>(call,obj,arg1,arg2,arg3).Compile();
  } 
  public static Func<object, TResult> instanceFunc<TResult>(Type T, string methodname){
    var method = T.GetMethod(methodname,[]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct ({typeof(TResult)}){T}.{methodname}()");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var call = Expression.Call(inst, method);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<object, TResult>>(conv,obj).Compile();
  }
  public static Func<object, T1, TResult> instanceFunc<T1, TResult>(Type T, string methodname){
    var method = T.GetMethod(methodname,[typeof(T1)]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct ({typeof(TResult)}){T}.{methodname}({typeof(T1)})");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var call = Expression.Call(inst, method, arg1);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<object, T1, TResult>>(conv,obj,arg1).Compile();
  }
  public static Func<object, T1, T2, TResult> instanceFunc<T1, T2, TResult>(Type T, string methodname){
    var method = T.GetMethod(methodname,[typeof(T1),typeof(T2)]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct ({typeof(TResult)}){T}.{methodname}({typeof(T1)},{typeof(T2)})");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var call = Expression.Call(inst, method, arg1,arg2);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<object, T1, T2, TResult>>(conv,obj,arg1,arg2).Compile();
  }
  public static Func<object, T1, T2, T3, TResult> instanceFunc<T1, T2, T3, TResult>(Type T, string methodname){
    var method = T.GetMethod(methodname,[typeof(T1),typeof(T2), typeof(T3)]);
    if(method == null){
      DebugConsole.WriteFailure($"Could not construct ({typeof(TResult)}){T}.{methodname}({typeof(T1)},{typeof(T2)},{typeof(T3)})");
      return null;
    }
    var obj = Expression.Parameter(typeof(object),"instance");
    var inst = Expression.Convert(obj, T);
    var arg1 = Expression.Parameter(typeof(T1), "arg1");
    var arg2 = Expression.Parameter(typeof(T2), "arg2");
    var arg3 = Expression.Parameter(typeof(T3), "arg3");
    var call = Expression.Call(inst, method, arg1,arg2,arg3);
    var conv = Expression.Convert(call, typeof(TResult));
    return Expression.Lambda<Func<object, T1, T2, T3, TResult>>(conv,obj,arg1,arg2,arg3).Compile();
  }
  public static Func<object,Tfield> instanceFieldGetter<Tfield>(Type T, string fieldname){
    var obj = Expression.Parameter(typeof(object), "obj");
    var casted = Expression.Convert(obj, T);
    var field = Expression.Field(casted, fieldname);
    return Expression.Lambda<Func<object,Tfield>>(field,obj).Compile();
  }
  public static Action<object,Tfield> instanceFieldSetter<Tfield>(Type T, string fieldname){
    var obj = Expression.Parameter(typeof(object), "obj");
    var casted = Expression.Convert(obj, T);
    var value = Expression.Parameter(typeof(Tfield), "value");
    var field = Expression.Field(casted, fieldname);
    var assign = Expression.Assign(field,value);
    return Expression.Lambda<Action<object,Tfield>>(assign,obj,value).Compile();
  }

  public class FieldHelper<Tfield>{
    public Func<object,Tfield> getter = null;
    public Action<object,Tfield> setter = null;
    public FieldHelper(Type T, string fieldname){
      getter = instanceFieldGetter<Tfield>(T,fieldname);
      setter = instanceFieldSetter<Tfield>(T,fieldname);
    }
    public Tfield get(object o)=>getter(o);
    public void set(object o, Tfield v)=>setter(o,v);
  }
}

using Celeste.Mod;
using System.Linq;
using System;
using System.Reflection;
using Celeste.Mod.auspicioushelper;
using System.Collections.Generic;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper.Import;

internal static class SpeedrunToolIop{
  internal static List<object> toDeregister = new List<object>();
  static void loadState(Dictionary<Type, Dictionary<string, object>> values, Level level){
    try{
      MaterialPipe.fixFromSrt();
      PortalGateH.intersections.Clear();
      BackdropCapturer.CapturedBackdrops.FixFromSrt(level);
    }catch(Exception ex){
      DebugConsole.WriteFailure($"Auspicioushelper speedruntool failed: \n {ex}");
      throw new Exception();
    }
  }

  [AttributeUsage(AttributeTargets.Field)]
  public class Static : Attribute { }
  static void SetupStaticAttr(){
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      List<string> st = new();
      foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)){
        if(f.IsDefined(typeof(Static))){
          if(!f.IsStatic) DebugConsole.WriteFailure("SrtIOP.Static attribute applied to non-static class",true);
          else if(!st.Contains(f.Name)) st.Add(f.Name);
        }
      }
      if(st.Count==0) continue;
      //Logger.Verbose("auspicioushelper",$"(SRT) Type {t.FullName}: adding static fields [{string.Join(", ",st)}]");
      toDeregister.Add(SpeedrunToolImport.RegisterStaticTypes(t, st.ToArray()));
    }
  }

  #pragma warning disable CS0649
  [ModImportName("SpeedrunTool.SaveLoad")]
  internal static class SpeedrunToolImport {
    public static Func<
      Action<Dictionary<Type, Dictionary<string, object>>, Level>, //onSave
      Action<Dictionary<Type, Dictionary<string, object>>, Level>, //onLoad
      Action, //onClear
      Action<Level>, //beforeSave
      Action<Level>, //beforeLoad
      Action, //preClone
    object> RegisterSaveLoadAction;
    public static Func<Type, string[], object> RegisterStaticTypes;
    public static Action<object> Unregister;
    public static Func<object, object> DeepClone;
  }
  #pragma warning restore CS0649
  internal static void srtloaduseapi(){
    DebugConsole.Write("Doing srt setup");
    typeof(SpeedrunToolImport).ModInterop();
    if(SpeedrunToolImport.RegisterStaticTypes!=null){
      SetupStaticAttr();
    }
    if(SpeedrunToolImport.RegisterSaveLoadAction!=null){
      toDeregister.Add(SpeedrunToolImport.RegisterSaveLoadAction(null,loadState,null,null,null,null));
    }
  }
  internal static HookManager hooks = new HookManager(srtloaduseapi, void()=>{
    DebugConsole.Write("Unloading SRT interop");
    foreach(object o in toDeregister){
      SpeedrunToolImport.Unregister(o);
    }
    toDeregister.Clear();
  });
}









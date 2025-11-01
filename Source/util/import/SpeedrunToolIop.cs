
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
      foreach(Entity e in Engine.Instance.scene.Entities){
        if(e is PortalOthersider m) m.RemoveSelf();
        if(e is PortalGateH portalgateh){
          portalHooks.hooks.enable();
        }
        if(e is Actor a){
          PortalGateH.SurroundingInfoH s = PortalGateH.evalEnt(a);
          PortalIntersectInfoH info = null;
          if(a.Left<s.leftl) {
            PortalGateH.intersections[a]=(info = new PortalIntersectInfoH(s.leftn, s.left,a));
          } else if(a.Right>s.rightl){
            PortalGateH.intersections[a]=(info = new PortalIntersectInfoH(s.rightn, s.right, a));
          }
          if(info!=null) info.addOthersider();
        }
      }
      FoundEntity.clear(Engine.Instance.scene);
      BackdropCapturer.CapturedBackdrops.FixFromSrt(level);
    }catch(Exception ex){
      DebugConsole.WriteFailure($"Auspicioushelper speedruntool failed: \n {ex}");
      throw new Exception();
    }
  }

  static List<object[]> staticTypes = new List<object[]>{
    new object[] {
      typeof(RenderTargetPool), new string[] { "available"}
    }, new object[]{
      typeof(MaterialPipe), new string[] {"layers", "leaving", "entering", "toremove"}
    }, new object[]{
      typeof(CassetteMaterialLayer), new string[] {"layers"}
    }, new object[]{
      typeof(TemplateDreamblockModifier), new string[] {nameof(TemplateDreamblockModifier.normrenderer),nameof(TemplateDreamblockModifier.revrender)}
    }, new object[]{
      typeof(BackdropCapturer), new string[] {nameof(BackdropCapturer.groups)}
    }
  };

  [AttributeUsage(AttributeTargets.Field)]
  public class Static : Attribute { }
  static void SetupStaticAttr(){
    Dictionary<Type,string[]> d = new();
    foreach(var sr in staticTypes) d[(Type)sr[0]]=(string[])sr[1];
    foreach(var t in typeof(auspicioushelperModule).Assembly.GetTypesSafe()){
      List<string> st = d.TryGetValue(t,out var da)?da.ToList():new();
      foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)){
        if(f.IsDefined(typeof(Static))){
          if(!f.IsStatic) DebugConsole.WriteFailure("SrtIOP.Static attribute applied to non-static class",true);
          else if(!st.Contains(f.Name)) st.Add(f.Name);
        }
      }
      if(st.Count==0) continue;
      //DebugConsole.Write($"(SRT) Type {t.FullName}: adding static fields [{string.Join(", ",st)}]");
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









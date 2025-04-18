
using Celeste.Mod;
using System.Linq;
using System;
using System.Reflection;
using Celeste.Mods.auspicioushelper;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static class SpeedrunToolIop{
  static Type interoptype = null;
  public static List<object> toDeregister = new List<object>();
  public static void speedruntoolinteropload(){
    DebugConsole.Write("Found speedruntool, setting up");
    var stmodule = Everest.Modules.FirstOrDefault(m=>m.Metadata.Name == "SpeedrunTool");
    if(stmodule == null) return;
    interoptype = stmodule.GetType().Assembly.GetType("Celeste.Mod.SpeedrunTool.SpeedrunToolInterop+SaveLoadExports");
    if(interoptype == null) return;
    do{
      MethodInfo registerfn = interoptype.GetMethod("RegisterStaticTypes");
      if(registerfn == null) break;
      try {
        toDeregister.Add(registerfn.Invoke(null, new object[] {
          typeof(ChannelState),
          new string[] { "channelStates"}
        }));
        toDeregister.Add(registerfn.Invoke(null, new object[] {
          typeof(ChannelBooster),
          new string[] { "lastUsed"}
        }));
      } catch (Exception ex) {
        DebugConsole.Write($"Failed to register static types: {ex}");
      }
    }while(false);
    do{
      MethodInfo registerfn = interoptype.GetMethod("RegisterSaveLoadAction");
      if(registerfn == null)break;

      Action<Dictionary<Type, Dictionary<string, object>>, Level> loadState = (values, level)=>{
        DebugConsole.Write($"Loading auspicioushelper savestate stuff");
        int? lastUsed = ChannelBooster.lastUsed?.id;
        ChannelState.unwatchAll();
        PortalGateH.intersections.Clear();
        TemplateCassetteManager.unfrickMats(level);
        foreach(Entity e in Engine.Instance.scene.Entities){
          if(e is PortalGateH portalgateh){
            portalHooks.hooks.enable();
          }
          if(e is IChannelUser e_){
            if(e_ is ChannelBooster b && b.id == lastUsed){
              ChannelBooster.lastUsed = b;
              DebugConsole.Write("Found matching booster");
            }
            ChannelState.watch(e_);
          }
          if(e is PortalOthersider m){
            m.RemoveSelf();
          }
          if(e is Actor a){
            PortalGateH.SurroundingInfoH s = PortalGateH.evalEnt(a);
            PortalIntersectInfoH info = null;
            if(a.Left<s.leftl) {
              PortalGateH.intersections[a]=(info = new PortalIntersectInfoH(s.leftn, s.left,a));
              PortalOthersider mn = info.addOthersider();
            } else if(a.Right>s.rightl){
              PortalGateH.intersections[a]=(info = new PortalIntersectInfoH(s.rightn, s.right, a));
              PortalOthersider mn = info.addOthersider();
            }
          }
        }
        foreach(ChannelTracker t in Engine.Instance.scene.Tracker.GetComponents<ChannelTracker>()){
          ChannelState.watch(t);
        }
        FoundEntity.clear(Engine.Instance.scene);
        DebugConsole.Write($"Finished successfully");
      };

      try {
        toDeregister.Add(registerfn.Invoke(null, new object[]{
          null, loadState, null, null, null, null
        }));
      } catch(Exception ex){
        DebugConsole.Write($"Failed to register action: {ex}");
      }
    }while(false);
  }
  public static HookManager hooks = new HookManager(speedruntoolinteropload, void()=>{
    if(interoptype == null) return; 
    MethodInfo deregisterfn = interoptype.GetMethod("Unregister");
    Logger.Log("[auspicious]","We are unloading");
    try{
      foreach(object o in toDeregister){
        deregisterfn.Invoke(null,new object[]{o});
      }
    } catch(Exception ex){
      DebugConsole.Write($"Deregistration failed with {ex}");
    }
  });
}
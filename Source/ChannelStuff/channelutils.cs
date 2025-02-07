


using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Monocle;
using Celeste.Mod;
using System.Linq;
using System;
using System.Reflection;

namespace Celeste.Mods.auspicioushelper;
public static class ChannelState{
  public static Dictionary<string, int> channelStates = new Dictionary<string, int>();
  public static Dictionary<string, List<IChannelUser>> watching = new Dictionary<string, List<IChannelUser>>();
  public static int readChannel(string ch){
    int v=0;
    channelStates.TryGetValue(ch, out v);
    return v;
  }
  public static void SetChannel(string ch, int state){
    if(readChannel(ch) == state) return;
    channelStates[ch] = state;
    // foreach(ChannelBaseEntity b in Engine.Scene.Tracker.GetEntities<ChannelBaseEntity>()){
    //   if(b.channel == ch)b.setChVal(state);
    // }
    if (watching.TryGetValue(ch, out var list)) {
      foreach(IChannelUser b in list){
        b.setChVal(state);
      }
    }
  }
  public static void unwatchNow(IChannelUser b){
    if (watching.TryGetValue(b.channel, out var list)) {
      list.Remove(b);
    }
  }
  public static void watch(IChannelUser b){
    //DebugConsole.Write("watching new thing");
    if (!watching.TryGetValue(b.channel, out var list)) {
      list = new List<IChannelUser>();
      watching[b.channel] = list;
    }
    list.Add(b);
  }
  public static void unwatchAll(){
    watching.Clear();
  }
  public static void unwatchTemporary(){
    foreach(var pair in watching){
      var newlist = new List<IChannelUser>();
      foreach(IChannelUser e in pair.Value){
        if(e is Entity en && en.TagCheck(Tags.Persistent)){
          newlist.Add(e);
        }
      }
      watching[pair.Key] = newlist;
    }
  }

  public static void speedruntoolinteropload(){
    var stmodule = Everest.Modules.FirstOrDefault(m=>m.Metadata.Name == "SpeedrunTool");
    if(stmodule == null) return;
    DebugConsole.Write("Found Speedruntool");
    Type interoptype = stmodule.GetType().Assembly.GetType("Celeste.Mod.SpeedrunTool.SpeedrunToolInterop+SaveLoadExports");
    if(interoptype == null) return;
    do{
      DebugConsole.Write("Attempting to bind static class");
      MethodInfo registerfn = interoptype.GetMethod("RegisterStaticTypes");
      if(registerfn == null) break;
      try {
        registerfn.Invoke(null, new object[] {
          typeof(ChannelState),
          new string[] { "channelStates"}
        });
        registerfn.Invoke(null, new object[] {
          typeof(ChannelBooster),
          new string[] { "lastUsed"}
        });
        DebugConsole.Write("Static type registration succeeded");
      } catch (Exception ex) {
        DebugConsole.Write($"Failed to register static types: {ex}");
      }
    }while(false);
    do{
      DebugConsole.Write("Setting up loading hook");
      MethodInfo registerfn = interoptype.GetMethod("RegisterSaveLoadAction");
      if(registerfn == null)break;

      Action<Dictionary<Type, Dictionary<string, object>>, Level> loadState = (values, level)=>{
        DebugConsole.Write($"Loading state");
        EntityID? lastUsed = ChannelBooster.lastUsed?.id;
        unwatchAll();
        foreach(Entity e in Engine.Instance.scene.Entities){
          if(e is IChannelUser e_){
            if(e_ is ChannelBooster b && b.id.ToString() == lastUsed.ToString()){
              ChannelBooster.lastUsed = b;
              DebugConsole.Write("Found matching booster");
            }
            watch(e_);
          }
        }
        foreach(ChannelTracker t in Engine.Instance.scene.Tracker.GetComponents<ChannelTracker>()){
          watch(t);
        }
      };

      try {
        registerfn.Invoke(null, new object[]{
          null, loadState, null, null, null, null
        });
        DebugConsole.Write("Hook successfully added");
      } catch(Exception ex){
        DebugConsole.Write($"Failed to register action: {ex}");
      }
    }while(false);
    DebugConsole.Write("Setup for speedruntool ended");
  }
}
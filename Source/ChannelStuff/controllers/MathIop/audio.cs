

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;

namespace Celeste.Mod.auspicioushelper.channelmath;

public static class FmodIop{
  static Dictionary<int,EventInstance> evs=new();
  static HashSet<int> trusted = new();
  static ConcurrentQueue<nint> toremove = new();
  static Dictionary<nint, int> endMap = new();
  static int handleCtr = 0;
  static FMOD.RESULT endcb(EVENT_CALLBACK_TYPE type, nint evi, nint parameter){
    if(type == EVENT_CALLBACK_TYPE.STOPPED){
      toremove.Enqueue(evi);
    }
    return FMOD.RESULT.OK;
  }
  static int GetPlayHandle(string thing, Vector2 pos){
    int handle = ++handleCtr;
    EventInstance ev = Audio.Play(thing, pos);
    ev.getPlaybackState(out var state);
    if(state == PLAYBACK_STATE.STOPPED) return 0;
    evs.Add(handle, ev);
    endMap.Add(ev.getRaw(),handle);
    ev.setCallback(endcb,EVENT_CALLBACK_TYPE.STOPPED);
    return handle;
  }
  static void clean(){
    while(toremove.TryDequeue(out var tr)){
      if(endMap.TryGetValue(tr, out var idx)){
        evs.Remove(idx);
        trusted.Remove(idx);
        endMap.Remove(tr);
      }
    }
  }
  public static void Register(){
    ChannelMathController.registerInterop("fmodP", (List<string> strs, List<int> ints)=>{
      Vector2 cpos = ChannelMathController.callingController.Position;
      Vector2 pos = strs.Count>2?FoundEntity.find(strs[2])?.Entity.Position??cpos:cpos;
      return GetPlayHandle(strs[1], pos);
    });
    ChannelMathController.registerInterop("fmodC", (List<string> strs, List<int> ints)=>{
      clean();
      trusted.Add(ints[0]);
      return evs.ContainsKey(ints[0])?1:0;
    });
    ChannelMathController.registerInterop("fmodS", (List<string> strs, List<int> ints)=>{
      clean();
      bool has = evs.TryGetValue(ints[0], out var ev);
      if(has) Audio.Stop(ev);
      return has?1:0;
    });
    ChannelMathController.registerInterop("fmodPS",(List<string> strs, List<int> ints)=>{
      clean();
      bool has = evs.TryGetValue(ints[0], out var ev);
      if(has){
        ev.setParameterValue(strs[1], ints[1]);
      }
      return has?1:0;
    });
    ChannelMathController.registerInterop("fmodPG",(List<string> strs, List<int> ints)=>{
      clean();
      var ev = evs.GetValueOrDefault(ints[0]);
      ev.getParameterValue(strs[1], out var v, out var vf);
      return (int)Math.Round(v);
    });
    ChannelMathController.registerInterop("fmodPF",(List<string> strs, List<int> ints)=>{
      clean();
      var ev = evs.GetValueOrDefault(ints[0]);
      ev.getParameterValue(strs[1], out var v, out var vf);
      return (int)Math.Round(vf);
    });
    ChannelMathController.registerInterop("fmodA",(List<string> strs, List<int> ints)=>{
      clean();
      return evs.ContainsKey(ints[0])?1:0;
    });
    ChannelMathController.registerInterop("fmodV",(List<string> strs, List<int> ints)=>{
      clean();
      if(!evs.TryGetValue(ints[0], out var ev)) return 0;
      ev.setVolume((float)ints[1]/100);
      return 1;
    });
    ChannelMathController.registerInterop("fmodT",(List<string> strs, List<int> ints)=>{
      clean();
      if(!evs.TryGetValue(ints[0], out var ev)) return 0;
      ev.setTimelinePosition(ints[1]);
      return 1;
    });
  }
  public static HookManager cbs = new HookManager(Register,bool ()=>{
    clean();
    foreach(var pair in evs){
      if(!trusted.Contains(pair.Key))Audio.Stop(pair.Value);
    }
    evs.Clear();
    trusted.Clear();
    toremove.Clear();
    return false;
  },auspicioushelperModule.OnReset);
} 
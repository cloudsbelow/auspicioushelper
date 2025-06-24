

using System;
using System.Collections.Generic;
using System.ComponentModel;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;

namespace Celeste.Mod.auspicioushelper.channelmath;

public static class FmodIop{
  static Dictionary<int, EventInstance> evs;
  static HashSet<int> trusted;
  static int handleCtr = 0;
  static int GetPlayHandle(string thing, Vector2 pos){
    int handle = ++handleCtr;
    EventInstance ev = Audio.Play(thing, pos);
    ev.getPlaybackState(out var state);
    if(state == PLAYBACK_STATE.STOPPED) return 0;
    evs.Add(handle, ev);
    ev.setCallback((type,_,_)=>{
      if(type == EVENT_CALLBACK_TYPE.STOPPED){
        evs.Remove(handle);
        trusted.Remove(handle);
      }
      return FMOD.RESULT.OK;
    },EVENT_CALLBACK_TYPE.STOPPED);
    return handle;
  }
  public static void Register(){
    ChannelMathController.registerInterop("fmodPlay", (List<string> strs, List<int> ints)=>{
      Vector2 cpos = ChannelMathController.callingController.Position;
      Vector2 pos = strs.Count>2?FoundEntity.find(strs[2])?.Entity.Position??cpos:cpos;
      return GetPlayHandle(strs[1], pos);
    });
    ChannelMathController.registerInterop("fmodTrust", (List<string> strs, List<int> ints)=>{
      trusted.Add(ints[0]);
      return evs.ContainsKey(ints[0])?1:0;
    });
    ChannelMathController.registerInterop("fmodStop", (List<string> strs, List<int> ints)=>{
      bool has = evs.TryGetValue(ints[0], out var ev);
      if(has) Audio.Stop(ev);
      return has?1:0;
    });
    ChannelMathController.registerInterop("fmodParamS",(List<string> strs, List<int> ints)=>{
      bool has = evs.TryGetValue(ints[0], out var ev);
      if(has){
        ev.setParameterValue(strs[1], ints[1]);
      }
      return has?1:0;
    });
    ChannelMathController.registerInterop("fmodParamG",(List<string> strs, List<int> ints)=>{
      var ev = evs.GetValueOrDefault(ints[0]);
      ev.getParameterValue(strs[1], out var v, out var vf);
      return (int)Math.Round(v);
    });
    ChannelMathController.registerInterop("fmodParamF",(List<string> strs, List<int> ints)=>{
      var ev = evs.GetValueOrDefault(ints[0]);
      ev.getParameterValue(strs[1], out var v, out var vf);
      return (int)Math.Round(vf);
    });
    ChannelMathController.registerInterop("fmodActive",(List<string> strs, List<int> ints)=>{
      return evs.ContainsKey(ints[0])?1:0;
    });
    ChannelMathController.registerInterop("fmodVolume",(List<string> strs, List<int> ints)=>{
      if(!evs.TryGetValue(ints[0], out var ev)) return 0;
      ev.setVolume((float)ints[1]/100);
      return 1;
    });
    ChannelMathController.registerInterop("fmodTimeline",(List<string> strs, List<int> ints)=>{
      if(!evs.TryGetValue(ints[0], out var ev)) return 0;
      ev.setTimelinePosition(ints[1]);
      return 1;
    });
  }
  public static HookManager cbs = new HookManager(Register,bool ()=>{
    foreach(var pair in evs){
      if(!trusted.Contains(pair.Key))Audio.Stop(pair.Value);
    }
    evs.Clear();
    trusted.Clear();
    return false;
  },auspicioushelperModule.OnReset);
}
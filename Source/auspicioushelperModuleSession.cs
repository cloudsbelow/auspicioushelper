

using System.Collections.Generic;
using Celeste.Mods.auspicioushelper;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public class auspicioushelperModuleSession : EverestModuleSession {
  public Dictionary<string,int> channelData = new Dictionary<string, int>();

  public void save(){
    DebugConsole.Write("Saving channel state");
    channelData.Clear();
    foreach(KeyValuePair<string, int> p in ChannelState.channelStates){
      channelData.Add(p.Key, p.Value);
    }
  }
  public void load(bool initialize){
    DebugConsole.Write("Loading channel state");
    if(initialize){
      channelData.Clear();
    }
    ChannelState.channelStates.Clear();
    foreach(KeyValuePair<string,int> p in channelData){
      ChannelState.channelStates.Add(p.Key,p.Value);
    }
    if(initialize) save();
  } 
}


using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public class auspicioushelperModuleSession : EverestModuleSession {
  public class EntityDataId{
    public EntityData data;
    public EntityID id;
    public EntityDataId(EntityData d, EntityID id){
      data = d;
      this.id = id;
    }
  }
  public Dictionary<string,int> channelData = new Dictionary<string, int>();
  public List<EntityDataId> PersistentFollowers = new List<EntityDataId>();
  public HashSet<string> collectedTrackedCassettes = new HashSet<string>();
  public HashSet<int> openedGates = new HashSet<int>();
  public HashSet<string> brokenTempaltes = new();

  public void save(){
    if(respDat==null) channelData = ChannelState.save();
  }
  public void load(bool initialize){
    if(initialize){
      channelData.Clear();
    }
    ChannelState.load(channelData);
    if(initialize) save();
  } 

  public class RespawnData {
    public enum RespawnType {
      CampfireRespawn, Basic
    }
    public Vector2 loc;
    public string level;
    RespawnType ty;
    public RespawnData(RespawnType t = RespawnType.Basic){
      ty = t;
    }
    static void Hook(On.Celeste.Level.orig_Reload orig, Level s){
      if(auspicioushelperModule.Session?.respDat is {} r){
        if(s.Session.Level!=r.level) auspicioushelperModule.OnNewScreen.run(); 
        s.Session.Level=r.level; 
        s.Session.RespawnPoint=r.loc;
        orig(s);
        switch(r.ty){
          case RespawnType.CampfireRespawn:
            CampfireThing.Callback(s,r);
            break;
        }
      } else orig(s);
    }
    [OnLoad]
    public static HookManager hooks = new(()=>{
      On.Celeste.Level.Reload+=Hook;
    },()=>{
      On.Celeste.Level.Reload-=Hook;
    });
  }
  public RespawnData respDat;
}
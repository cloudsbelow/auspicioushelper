

using System;
using System.Collections.Generic;
using Celeste.Editor;
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
  public Dictionary<string,double> channelData = new ();
  public List<EntityDataId> PersistentFollowers = new List<EntityDataId>();
  public HashSet<string> collectedTrackedCassettes = new HashSet<string>();
  public HashSet<int> openedGates = new HashSet<int>();
  public HashSet<string> brokenTempaltes = new();

  public void save(){
    if(respDat==null) channelData = ChannelState.save();
  }
  public void load(){
    ChannelState.load(channelData);
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
    static void Hook(On.Celeste.Editor.MapEditor.orig_LoadLevel orig, MapEditor self, LevelTemplate level, Vector2 at){
      if(auspicioushelperModule.Session is {} s)s.respDat = null;
      orig(self,level,at);
    }
    [OnLoad]
    public static HookManager hooks = new(()=>{
      On.Celeste.Level.Reload+=Hook;
      On.Celeste.Editor.MapEditor.LoadLevel+=Hook;
    },()=>{
      On.Celeste.Level.Reload-=Hook;
      On.Celeste.Editor.MapEditor.LoadLevel-=Hook;
    });
  }
  public RespawnData respDat;
}
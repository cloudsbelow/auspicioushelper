
using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class ChannelTracker : OnAnyRemoveComp{
  public string channel {get; set;}
  public int value;
  Action<int> onChannelChange;
  ChannelTrackerList inList=null;
  public ChannelTracker(string channel, Action<int> onChannelChange, bool immediateInvoke = false):base(false, false){
    this.channel=channel;
    this.onChannelChange=onChannelChange;
    value = ChannelState.watch(this);
    if(immediateInvoke) onChannelChange(value);
  }
  public ChannelTracker(string channel):this(channel, static (_)=>{}, false){}
  public void setChVal(int val){
    value = val;
    onChannelChange(val);
  }
  public ChannelTracker AddTo(Entity e){
    e.Add(this);
    return this;
  }
  public override void OnRemove() {
    inList?.Remove(this);
  }
  public class ChannelTrackerList{
    List<ChannelTracker> list = new();
    HashSet<ChannelTracker> toRemove = new();
    List<ChannelTracker> deferredAdd = new();
    List<ChannelTracker> deferredRemove = new();
    bool locked = false;
    public void Apply(int n){
      locked = true;
      if(toRemove.Count==0) foreach(var ct in list)ct.setChVal(n);
      else {
        List<ChannelTracker> nlist = new();
        foreach(var ct in list){
          if(toRemove.Contains(ct)) continue;
          ct.setChVal(n);
          nlist.Add(ct);
        }
        toRemove.Clear();
        list = nlist;
      }
      locked = false;
      Catchup();
    }
    void Catchup(){
      if(deferredAdd.Count==0 && deferredRemove.Count==0) return;
      foreach(var v in deferredAdd) Add(v);
      deferredAdd.Clear();
      foreach(var v in deferredRemove) Add(v);
      deferredRemove.Clear();
    }
    public void Remove(ChannelTracker c){
      if(locked){
        deferredRemove.Add(c); 
        return;
      }
      toRemove.Add(c);
      if(toRemove.Count>list.Count/3){
        List<ChannelTracker> nlist = new();
        foreach(var ct in list){
          if(toRemove.Contains(ct)) continue;
          nlist.Add(ct);
        }
        toRemove.Clear();
        list = nlist;
      }
    }
    public bool RemoveTemp(){
      List<ChannelTracker> nlist = new();
      foreach(var ct in list){
        if(ct.Entity is {} en && (en.TagCheck(Tags.Persistent) || en.TagCheck(Tags.Global))){
          if(!toRemove.Contains(ct))nlist.Add(ct);
        }
        toRemove.Add(ct);//This also removes inexplicable duplicates
      }
      toRemove.Clear();
      list = nlist;
      return nlist.Count>0;
    }
    public void Add(ChannelTracker ct){
      if(locked){
        deferredAdd.Add(ct);
        return;
      }
      if(ct.inList!=null){
        DebugConsole.WriteFailure("Rewatching watched channel tracker");
      }
      list.Add(ct);
      ct.inList = this;
    }
  }
}
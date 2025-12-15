


using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelTriggerTrigger")]
public class ChannelTriggerTrigger:Entity{
  string channel;
  float delay = 0;
  Vector2[] nodes=null;
  Queue<(float, int)> enqd = new();
  UpdateHook upd;
  float ct=0;
  public ChannelTriggerTrigger(EntityData d, Vector2 o):base(d.Position+o){
    if(d.Nodes?.Length>0) nodes = d.NodesWithPosition(o);
    delay = d.Float("delay", -1);
    channel = d.Attr("channel","");
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    Add(upd=new());
    Add(new ChannelTracker(channel, OnChange, true));
  }
  void OnChange(double nval){
    if(delay<0) TriggerAt((int)nval);
    else enqd.Enqueue(new(ct+delay-(upd.updatedThisFrame?0.0001f:0),(int)nval));
  }
  public override void Update() {
    while(enqd.Count>0 && enqd.Peek().Item1<ct){
      TriggerAt(enqd.Dequeue().Item2);
    }
    base.Update();
    ct+=Engine.DeltaTime;
  }
  void TriggerAt(int idx){
    Vector2 loc = Position;
    if(nodes==null){
      if(idx==0) return;
    } else {
      if(idx<0 || idx>=nodes.Length) return;
      loc = nodes[idx];
    }
    Trigger smallestTrigger = null;
    float carea = float.PositiveInfinity;
    foreach(Trigger t in Scene.Tracker.GetEntities<Trigger>()){
      if(t.Collidable && t.CollidePoint(loc) && t.Width*t.Height<carea){
        smallestTrigger = t;
        carea = t.Width*t.Height;
      }
    }
    smallestTrigger?.OnEnter(Scene.Tracker.GetEntity<Player>() ?? Util.GetUninitializedEntWithComp<Player>());
  }
}



using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/dultraThing")]
public class DultraHelper:Trigger{
  public DultraHelper(EntityData d, Vector2 o):base(d,o){}
  public override void OnStay(Player p) {
    base.OnStay(p);
    if(p.DashDir.Y>0 && p.StateMachine.State == Player.StNormal){
      float old = p.Speed.X;
      p.MoveV(1,p.OnCollideV);
      if(p.DashDir.Y==0) DebugConsole.Write($"Dultra given: {old} to {p.Speed.X}");
      else DebugConsole.Write("Dultra suck");
    }
  }
}

[CustomEntity("auspicioushelper/speedNormal")]
public class SpeedConsist:Trigger{
  float to;
  float min;
  float max;
  int amult;
  float? setYSpeed;
  bool tryOnlyOnce;
  bool applyOnlyOnce;
  public SpeedConsist(EntityData d, Vector2 o):base(d,o){
    to=d.Float("speed");
    min=d.Float("min",to-50);
    max=d.Float("max",to+50);
    amult=d.Int("dir",0);
    tryOnlyOnce=d.Bool("tryOnlyOnce",false);
    applyOnlyOnce=d.Bool("applyOnlyOnce",true);
    setYSpeed=d.tryGetStr("setYSpeed",out var st) && float.TryParse(st, out float f)?f:null;
  }
  public void thing(Player player){
    float val = Math.Abs(player.Speed.X);
    int dir = Math.Sign(player.Speed.X);
    if(val>=min && val<=max && dir*amult>=0){
      Vector2 old = player.Speed;
      player.Speed.X = to*dir;
      if(setYSpeed is float f) player.Speed.Y = f;
      DebugConsole.Write($"Normalized speed from {old} to {player.Speed}");
      if(applyOnlyOnce) RemoveSelf();
    }
    if(tryOnlyOnce) RemoveSelf();
  }
  public override void OnStay(Player player) {
    base.OnStay(player);
    thing(player);
  }
  public override void OnEnter(Player player) {
    base.OnEnter(player);
    thing(player);
  }
}
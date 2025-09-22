


using System.Collections.Concurrent;
using FMOD.Studio;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class AudioMangler:OnAnyRemoveComp{
  float time;
  EventInstance ev;
  public override void OnRemove() {
    Audio.Stop(ev);
  }
  public AudioMangler(EventInstance ev, float duration, int startpos=0):base(true,false){
    time = duration;
    this.ev = ev;
    ev.setTimelinePosition(startpos);
  }
  public override void Update() {
    time-=Engine.DeltaTime;
    if(time<0){
      Entity.Remove(this);
      OnRemove();
    }
  }
}

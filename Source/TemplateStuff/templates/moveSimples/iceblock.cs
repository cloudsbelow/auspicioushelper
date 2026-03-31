



using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateIceblock")]
public class TemplateIceblock:TemplateDisappearer,ITemplateTriggerable{
  Vector2 offset = Vector2.Zero;
  protected override Vector2 virtLoc => Position+offset;
  public override Vector2 gatheredLiftspeed => disconnected?ownLiftspeed:base.gatheredLiftspeed;
  public override void relposTo(Vector2 loc, Vector2 parentLiftspeed) {
    if(disconnected) return;
    base.relposTo(loc, parentLiftspeed);
  }
  float sinkTime;
  Vector2 sinkDir;
  float respawnTimer=0;
  float respawnTime = 2;
  bool triggerable;
  bool ridingTriggers;
  bool disconnect=false;
  bool disconnected=false;
  int quiet=0;
  public TemplateIceblock(EntityData d, Vector2 o):this(d,o,d.Int("depthoffset",0)){}
  public TemplateIceblock(EntityData d, Vector2 o, int depthoffset)
  :base(d,o+d.Position,depthoffset){
    sinkTime = d.Float("sinkTime",1);
    var li = Util.csparseflat(d.Attr("sinkDist","12"));
    sinkDir = li.Length switch{
      0=>Vector2.UnitY*12,
      1=>Vector2.UnitY*li[0],
      _=>new(li[0],li[1])
    };
    respawnTime = d.Float("respawnTime",1.6f);
    triggerable = d.Bool("triggerable",true);
    ridingTriggers = d.Bool("ridingTriggers",true);
    disconnect = d.Bool("disconnect", false);
    quiet = d.Int("quiet",0);
  }
  Coroutine routine;
  IEnumerator iceRoutine(){
    float time = 0;
    if(sinkTime!=0){
      shake(0.1f);
      if((quiet%4)<2)Add(new AudioMangler(Audio.Play("event:/game/09_core/iceblock_touch", Position), 0.3f));
      ownLiftspeed = sinkDir/sinkTime;
      while(time<sinkTime-0.20){
        time = time+Engine.DeltaTime;
        offset = sinkDir*time/sinkTime;
        childRelposSafe();
        yield return null;
      }
      shake(0.20f);
      if(quiet%2==0)Add(new AudioMangler(Audio.Play("event:/game/09_core/iceblock_touch", Position), 1f,1000));

      while(time<sinkTime){
        time = time+Engine.DeltaTime;
        offset = sinkDir*time/sinkTime;
        childRelposSafe();
        yield return null;
      }
    }
    destroyChildren();
    yield return null;
    respawnTimer = respawnTime;
    offset = Vector2.Zero;
    routine = null;
  }
  void trigger(){
    if(disconnect){
      disconnected = true;
      parent?.GetFromTree<IRemovableContainer>()?.RemoveChild(this);
    }
    if(routine == null) Add(routine = new Coroutine(iceRoutine()));
  }
  void ITemplateTriggerable.OnTrigger(TriggerInfo info) {
    if(!triggerable){
      info.Pass(this);
      return;
    }
    if(!info.TestPass(this)) return;
    trigger();
  }
  bool reforming = false;
  public override void Update() {
    base.Update();
    if(reforming && !(UpdateHook.cachedPlayer is {} player && hasInside(player))){
      reforming = false;
      setVisCol(true,true);
      if(quiet<4)Audio.Play("event:/game/09_core/iceblock_reappear",Position);
    }
    if(respawnTimer>0){
      respawnTimer-=Engine.DeltaTime;
      if(respawnTimer<=0){
        if(disconnected){
          disconnected = false;
          if(parent?.GetFromTree<IRemovableContainer>() is {} cont){
            if(!cont.RestoreChild(this)){
              destroy(false);
              return;
            }
          }
          parent?.relposOne(this);
        }
        remake(()=>{
          if(PlayerIsInside()){
            reforming = true;
            setVisCol(false,false);
          } else if(quiet<4) Audio.Play("event:/game/09_core/iceblock_reappear",Position);
        });
      }
    } else if(ridingTriggers && routine == null && hasPlayerRider()) trigger();
  }
}
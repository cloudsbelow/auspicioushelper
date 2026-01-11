


using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateCloud")]
public class TemplateCloud:TemplateDisappearer, ITemplateTriggerable{
  bool fragile;
  bool triggered;
  bool fromRiding;
  float respawnTime = 1;
  float pos = 0;
  Vector2 cloudDir = new(0,1);
  protected override Vector2 virtLoc=>Position+pos*cloudDir;
  float speed;
  bool disableCbb;
  public TemplateCloud(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateCloud(EntityData d, Vector2 offset, int depthoffset):base(d,d.Position+offset,depthoffset){
    Add(new Coroutine(cloudRoutine()));
    fragile = d.Bool("fragile");
    respawnTime = d.Float("respawnTime",2.5f);
    fromRiding = d.Bool("fromRiding",true);
    var l = Util.csparseflat(d.String("cloudDir","1"));
    cloudDir = l.Length switch {1=>new Vector2(0,l[0]), >=2=>new Vector2(l[0],l[1]), _=>new Vector2(0,12)};
    if(disableCbb = d.Bool("noDoubleBoost",true)) ResetEvents.LazyEnable(typeof(CloudboostBlocker));
  }
  void Move(){
    pos+=Engine.DeltaTime*speed;
    ownLiftspeed = (speed<0?-220:speed)*cloudDir;
    childRelposSafe();
  }
  public IEnumerator cloudRoutine(){
    waiting:
      triggered = false;
      while(true){
        if(triggered || (fromRiding && hasPlayerRider())){    
          GetFromTree<ITemplateTriggerable>()?.OnTrigger(new TriggerInfo.EntInfo("tcloud/begin",this,false));
          speed = 180;
          Audio.Play(fragile?"event:/game/04_cliffside/cloud_pink_boost":"event:/game/04_cliffside/cloud_blue_boost", Position);
          goto going;
        }
        yield return null;
      }
    going:
      Move();
      yield return null;
      if(speed>=-100 && pos<0){
        GetFromTree<ITemplateTriggerable>()?.OnTrigger(new TriggerInfo.EntInfo("tcloud/end",this,true));
        if(UpdateHook.cachedPlayer is Player p && hasPlayerRider() && p.Speed.Y>=0){
          if(disableCbb) p.Add(new CloudboostBlocker(-200*cloudDir.X));
          p.Speed=-200*cloudDir;
        }
        if(!fragile) goto returning;
        destroyChildren(true);
        goto respawning;
      }
      speed -= 1200*Engine.DeltaTime*Math.Sign(pos);
      goto going;
    returning:
      speed = Calc.Approach(speed, 180f, 600f * Engine.DeltaTime);
      pos = Calc.Approach(pos, 0, speed*Engine.DeltaTime);
      ownLiftspeed = (speed<0?-220:speed)*cloudDir;
      childRelposSafe();
      if(pos==0) goto waiting;
      yield return null;
      goto returning;
    respawning:
      if(respawnTime<0){
        RemoveSelf();
        yield break;
      }
      setVisCol(false,false);
      yield return respawnTime;
      pos = 0;
      remake();
      UpdateHook.AddAfterUpdate(enforce, false, true);
      yield return null;
      while(UpdateHook.cachedPlayer is {} p && hasInside(p)) yield return null;
      setVisCol(true,true);
      Audio.Play("event:/game/04_cliffside/cloud_pink_reappear",Position);
      goto waiting;
  }
  void ITemplateTriggerable.OnTrigger(TriggerInfo s) {
    if(!TriggerInfo.TestPass(s,this)) return;
    triggered = true;
  }



  class CloudboostBlocker:Component{
    float amount;
    float lgt=float.PositiveInfinity;
    public CloudboostBlocker(float amount):base(true,false){
      this.amount=amount;
    }
    public override void Update() {
      base.Update();
      if(Entity is not Player p)throw new Exception("you're mean");
      else {
        if(p.jumpGraceTimer<=0 || p.jumpGraceTimer>lgt)Entity.Remove(this);
        lgt = p.jumpGraceTimer;
      }
    }
    [ResetEvents.OnHook(typeof(Player),nameof(Player.Jump))]
    static void Hook(On.Celeste.Player.orig_Jump orig, Player p, bool a, bool b){
      if(p.Get<CloudboostBlocker>() is {} cbb){
        var sign = Math.Sign(p.Speed.X);
        p.Speed.X=sign*Math.Max(0,p.Speed.X*sign-cbb.amount*sign);
      }
      orig(p,a,b);
    }
  }
}
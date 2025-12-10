


using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ConditionalStrawbCollectTrigger")]
public class ConditionalStrawbCollectTrigger:Trigger {
  public string idstr;
  bool onSafeGround=false;
  public ConditionalStrawbCollectTrigger(EntityData data, Vector2 offset):base(data, offset){
    idstr=data.Attr("strawberry_id");
    onSafeGround = data.Bool("playerOnSafeGround",false);
  }
  public override void OnStay(Player player){
    base.OnStay(player);
    if(onSafeGround && !player.OnSafeGround) return;
    foreach(ConditionalStrawb s in Scene.Tracker.GetEntities<ConditionalStrawb>()){
      if(s.idstr == idstr && s.follower.Leader==player.Leader){
        s.OnCollect();
      }
    }
  }
}
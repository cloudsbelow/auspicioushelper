


using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.auspicioushelper;
using System;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelSprite")]
public class ChannelSprite:ChannelBaseEntity{
  public int num;
  public Sprite sprite;
  public enum edgeTypes{
    loop, clamp, hide,
  }
  edgeTypes ty; 
  public ChannelSprite(EntityData d, Vector2 offset):base(d.Position+offset){
    channel=d.Attr("channel","");
    Add(sprite=GFX.SpriteBank.Create(d.Attr("xml_spritename")));
    sprite.Position.X=d.Int("offsetX",0);
    sprite.Position.Y=d.Int("offsetY",0);
    if(d.Bool("attached",false)){
      Add(new StaticMover{
        SolidChecker = checkSolid
      });
    }
    num = d.Int("cases",1);
    ty=d.Attr("edge_type","") switch {
      "loop"=>edgeTypes.loop,
      "clamp"=>edgeTypes.clamp,
      _=>edgeTypes.hide,
    };
    Depth=d.Int("depth",2);
  }
  private bool checkSolid(Solid solid){
    return Collide.CheckPoint(solid, Position);
  }
  public override void setChVal(int val){
    if(val<0 || val>=num){
      switch(ty){
        case edgeTypes.loop: val=(val%num+num)%num; break;
        case edgeTypes.clamp: val=Math.Clamp(val,0,num-1);break;
        default:
          sprite.Visible=false;
          return;
      }
    }
    sprite.Visible=true;
    sprite.Play("case"+val.ToString());
  }
  public override void Added(Scene scene){
    base.Added(scene);
    setChVal(ChannelState.readChannel(channel));
  }
}
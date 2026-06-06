


using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.auspicioushelper;
using System;
using Celeste.Mod.Entities;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelSprite")]
public class ChannelSprite:Entity{
  public int num;
  public Sprite sprite;
  public Image image;
  public enum edgeTypes{
    loop, clamp, hide,
  }
  edgeTypes ty; 
  string channel;
  ChannelState.FloatCh scaleX, scaleY, rotation;
  public ChannelSprite(EntityData d, Vector2 offset):base(d.Position+offset){
    channel=d.Attr("channel","");
    if(d.String("image_path") is {} istr) Add(image=new(GFX.Game[istr]));
    image?.CenterOrigin();
    if(d.String("xml_spritename") is {} sstr) Add(sprite=GFX.SpriteBank.Create(sstr));

    if(d.Bool("attached",false) || d.Nodes?.Length>0){
      Vector2 pos = d.Nodes?.Length>0? d.Nodes[0]:Position; 
      Add(new StaticMover{
        SolidChecker = solid=>solid.CollidePoint(pos)
      });
    }
    num = d.Int("cases",1);
    ty=d.Attr("edge_type","") switch {
      "loop"=>edgeTypes.loop,
      "clamp"=>edgeTypes.clamp,
      _=>edgeTypes.hide,
    };
    Depth=d.Int("depth",2);

    scaleX = d.ChannelFloat("scaleX",1);
    scaleY = d.ChannelFloat("scaleY",1);
    rotation = d.ChannelFloat("rotation",0);
  }
  public void setChVal(double got){
    int val = (int) Math.Floor(got);
    if(val<0 || val>=num){
      switch(ty){
        case edgeTypes.loop: val=(val%num+num)%num; break;
        case edgeTypes.clamp: val=Math.Clamp(val,0,num-1);break;
        default:
          sprite?.Visible=false;
          return;
      }
    }
    sprite.Visible=true;
    sprite.Play("case"+val.ToString());
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(!string.IsNullOrEmpty(channel))Add(new ChannelTracker(channel, setChVal, true));
  }
  public override void Update() {
    base.Update();
    if(sprite!=null){
      sprite.Rotation = rotation*MathF.PI/360;
      sprite.Scale = new(scaleX,scaleY);
    }
    if(image!=null){
      image.Rotation = rotation*MathF.PI/360;
      image.Scale = new(scaleX,scaleY);
    }
  }
}
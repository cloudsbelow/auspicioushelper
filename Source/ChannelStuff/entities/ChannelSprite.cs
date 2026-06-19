


using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.auspicioushelper;
using System;
using Celeste.Mod.Entities;
using System.Collections.Generic;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelSprite")]
public class ChannelSprite:Entity{
  public Sprite sprite;
  List<string> anims;
  List<string> layers;
  Image image;
  string channel = null;
  ChannelState.FloatCh rotation;
  ChannelState.Vec2Ch scale, origin;
  public ChannelSprite(EntityData d, Vector2 offset):base(d.Position+offset){
    if(d.String("image_path") is {} istr) Add(image=new(GFX.Game[istr]));
    image?.CenterOrigin();
    if(d.String("xml_spritename") is {} sstr){
      Add(sprite=GFX.SpriteBank.Create(sstr));
      channel=d.Attr("channel","");
      anims = Util.listparseflat(d.Attr("animationNames",""));
    } 
    var tint = Util.hexToColor(d.String("tint","fff"));
    image?.SetColor(tint);
    sprite?.SetColor(tint);

    if(d.Bool("attached",false) || d.Nodes?.Length>0){
      Vector2 pos = d.Nodes?.Length>0? d.Nodes[0]:Position; 
      Add(new StaticMover{
        SolidChecker = solid=>solid.CollidePoint(pos)
      });
    }
    Depth=d.Int("depth",2);

    scale = d.ChannelVecOrScalar("scale",1);
    origin = d.ChannelVec2("origin", 0,0,true);
    rotation = d.ChannelFloat("rotation",0);
    layers = Util.listparseflat(d.Attr("materialIdentifiers",""));
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(sprite!=null){
      if(anims.Count==0) throw new Exception("No animations given");
      Add(new ChannelTracker(channel, n=>{
        int val = (int) Math.Floor(n);
        sprite.Play(anims[Util.SafeMod(val,anims.Count)]);
      }, true));
    }
    if(layers.Count>0){
      var blockedByLayer=true;
      var comp = OverrideVisualComponent.Get(this);
      for(int i=0; i<layers.Count; i++){
        var l = layers[i];
        if(l=="normal") blockedByLayer=false;
        else{
          bool steal = blockedByLayer && i==layers.Count-1;
          var m = MaterialController.getLayer(l) as IOverrideVisuals;
          if(m!=null) comp.AddToOverride(new(m,0,steal,true)); 
          else DebugConsole.Write($"Could not find layer {l} in {this}");
        }
      }
    }
  }
  public override void Update() {
    base.Update();
    if(sprite!=null){
      sprite.Rotation = rotation*MathF.PI/180;
      sprite.Scale = scale;
      var t = sprite.Texture;
      var j = sprite.Justify ?? Vector2.Zero;
      sprite.Origin = new Vector2(t.Width*j.X + origin.X, t.Height*j.Y + origin.Y);
    }
    if(image!=null){
      image.Rotation = rotation*MathF.PI/180;
      image.Scale = scale;
      image.Origin = new Vector2(image.Width,image.Height)/2f + origin;
    }
  }
}
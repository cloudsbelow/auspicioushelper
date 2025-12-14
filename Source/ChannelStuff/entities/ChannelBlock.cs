using System;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
namespace Celeste.Mod.auspicioushelper;

[Tracked]
[CustomEntity("auspicioushelper/ChannelBlock")]
public class ChannelBlock:Entity, ICustomMatRender {
  public bool inverted;
  public float width;
  public float height;
  public bool safe;
  public bool alwayspresent;
  enum SolidState {
    gone,
    trying,
    there,
  }
  SolidState curstate;
  Solid solid;
  string channel;
  public ChannelBlock(EntityData data, Vector2 offset):base(data.Position+offset){
    Depth=-9000;
    channel = data.Attr("channel","");
    inverted = data.Bool("inverted",false);
    safe = data.Bool("safe",false);
    width = data.Width;
    height = data.Height;
    alwayspresent = data.Bool("alwayspresent",false);
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(ChannelMaterialsA.layerA is {} layera && layera.enabled){
      OverrideVisualComponent.Get(this).AddToOverride(new(layera,-100,true));
    }
    scene.Add(solid = new Solid(Position, width, height, safe));
    new ChannelTracker(channel, setChVal, true).AddTo(this);
  }
  void setChVal(double val){
    curstate = (val!=0) != inverted?SolidState.there:SolidState.gone;
    solid.Collidable = curstate == SolidState.there;
  }
  public override void Render(){
    if(curstate == SolidState.there){
      Draw.Rect(Position, width, height, inverted? Color.Blue:Color.Red);
    } else {
      Draw.HollowRect(Position, width, height, Color.Red);
    }
  }
  void ICustomMatRender.MatRender(){
    if(curstate == SolidState.there){
      Draw.SpriteBatch.Draw(Draw.Pixel.Texture.Texture_Safe, new Rectangle((int)Position.X, (int)Position.Y, (int)width, (int)height), Draw.Pixel.ClipRect, new Color(1,0,0,255));
    }
  }
}
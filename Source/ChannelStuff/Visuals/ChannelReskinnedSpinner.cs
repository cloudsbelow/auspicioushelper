


using System;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ChannelReskinnedSpinner")]
public class ChannelReskinnedSpinner:Entity, IMaterialEnt{

  public float offset;
  public int randomSeed;
  public bool otherVisible=true;
  public static MTexture bigspinner=null;
  public static MTexture minispinner=null;
  public bool mini;
  MTexture sprite;
  Vector2 origin;
  public ChannelReskinnedSpinner(EntityData d, Vector2 offset):base(d.Position+offset){
    if(bigspinner == null){
      bigspinner = GFX.Game.GetAtlasSubtextureFromAtlasAt("objects/auspicioushelper/channelcrystal/crystalfg",0);
      minispinner = GFX.Game.GetAtlasSubtextureFromAtlasAt("objects/auspicioushelper/channelcrystal/crystalminifg",0);
    }
    mini = d.Bool("mini",false);
    sprite = mini?minispinner:bigspinner;
    origin = mini?new Vector2(8,8):new Vector2(12,12);
    base.Tag = Tags.TransitionUpdate;
    base.Collider = mini?new Circle(4f):new ColliderList(new Circle(6f), new Hitbox(16f, 4f, -8f, -3f));
    this.offset = Calc.Random.NextFloat();
    Collidable = false;
    Visible = false;
    Add(new PlayerCollider(OnPlayer));
    Add(new LedgeBlocker());
    base.Depth = -8001;
    if(d.Nodes.Length !=0){
      Add(new StaticMover(){
        OnMove = (Vector2 amount)=>Position+=amount,
        SolidChecker = (Solid s)=>{
          return s.CollidePoint(d.Nodes[0]+offset);
        }
      });
    }
    randomSeed = Calc.Random.Next();
  }
  public override void Awake(Scene scene){
    if(ChannelBaseEntity.layerA?.enabled??false) otherVisible = false;
    ChannelBaseEntity.layerA?.addEnt(this);
  }
  private bool InView(){
    Camera c = (base.Scene as Level).Camera;
    return X > c.X - 16f && Y > c.Y - 16f && X < c.X + 320f + 16f && Y < c.Y + 180f + 16f;
  }
  private void OnPlayer(Player player){
    player.Die((player.Position - Position).SafeNormalize());
  }
  public override void Update(){
    base.Update();
    if(!Visible){
      if(InView()){
        Collidable=true;
        Visible=true;
      }
    }
    if(Visible){
      if (base.Scene.OnInterval(0.25f, offset) && !InView()){
        Visible=false; Collidable=false;
      }
      if (base.Scene.OnInterval(0.05f, offset)){
        Player entity = base.Scene.Tracker.GetEntity<Player>();
        if (entity != null){
          Collidable = Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f;
        }
      }
    }
  }
  public override void Render(){
    if(!otherVisible) return;
    //Draw.Rect(Position-new Vector2(12,12),24,24,Color.Red);
    base.Render();
    Draw.SpriteBatch.Draw(sprite.Texture.Texture_Safe, Position, sprite.ClipRect, Color.White, 
      ((float)Math.PI/2)*(float)(randomSeed & 3), origin,1,(randomSeed&4) !=0?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);
    /*Draw.SpriteBatch.Draw(innersprite.Texture.Texture_Safe, Position, innersprite.ClipRect, new Color(1,1,1,1), 
      (float)Math.PI/2*(randomSeed & 3), new Vector2(12f, 12f), 1, (randomSeed&4) !=0?SpriteEffects.FlipHorizontally:SpriteEffects.None, 0f);*/
  }
  public void renderMaterial(IMaterialLayer l, Camera c){
    Draw.SpriteBatch.Draw(sprite.Texture.Texture_Safe, Position, sprite.ClipRect, Color.White, 
      ((float)Math.PI/2)*(float)(randomSeed & 3), origin,1,(randomSeed&4) !=0?SpriteEffects.FlipHorizontally:SpriteEffects.None,0);
    // Vector2 pos = Position-new Vector2(2,2);
    // if(state[currentState]!=BoosterType.none)pos+=sprite.Position;
    // pos=pos.Floor();
    // sb.Draw(Draw.Pixel.Texture.Texture_Safe,new Rectangle(
    //   (int)pos.X,(int)pos.Y, 4,4
    // ),Draw.Pixel.ClipRect,iinnerColor);
  }
}


using System;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/SpiralBossBeam")]
public class SpiralBossBeam:Entity {
  Sprite beamSprite;
  float angle;
  float dt;
  float nrad;
  float nradkill=14;
  float mfrad;
  float far;
  int frame;
  Random randSource = new Random();
  public static ParticleType P_DissipateDense = new ParticleType{
      Color = Calc.HexToColor("e60022"),
      Size = 1f,
      FadeMode = ParticleType.FadeModes.Late,
      SpeedMin = 15f,
      SpeedMax = 30f,
      DirectionRange = (float)Math.PI / 3f,
      LifeMin = 0f,
      LifeMax = 0.15f
  };
  public static ParticleType P_DissipateSparse = new ParticleType{
      Color = Calc.HexToColor("e60022"),
      Size = 1f,
      FadeMode = ParticleType.FadeModes.Late,
      SpeedMin = 15f,
      SpeedMax = 30f,
      DirectionRange = (float)Math.PI / 3f,
      LifeMin = 0f,
      LifeMax = 0.5f
  };
  public static ParticleType P_Hit = new ParticleType{
      Color = Calc.HexToColor("e60022"),
      Size = 1f,
      FadeMode = ParticleType.FadeModes.Late,
      SpeedMin = 35f,
      SpeedMax = 100f,
      DirectionRange = (float)Math.PI /2,
      LifeMin = 0.15f,
      LifeMax = 0.3f
  };
  public SpiralBossBeam(EntityData data, Vector2 offset):base(data.Position+offset){
    Add(beamSprite = GFX.SpriteBank.Create("badeline_beam"));
    angle=data.Float("start_angle",0);
    dt = data.Float("speed");
    nrad = data.Float("near_radius",8);
    mfrad = data.Float("max_range",2000f);
    Depth=-10005;
    frame=0;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    beamSprite.Play("shoot",true);
  }
  public override void Update(){
    base.Update();
    angle+=Engine.DeltaTime*dt;
    var dir = new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle));
    far = mfrad;
    foreach(BeamBlocker b in Scene.Tracker.GetEntities<BeamBlocker>()){
      far = Math.Min(far, b.rayCollision(Position+dir*nrad,dir)+nrad);
    }
    doParticles(far<mfrad);
    handlePlayer();
  }
  public void doParticles(bool hit){
    var dir = new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle));
    var orth = new Vector2(-(float)Math.Sin(angle),(float)Math.Cos(angle));
    Level level = SceneAs<Level>();
    Vector2 padding=new Vector2(20f,20f);
    Vector2 camcorner1 = level.Camera.Position-padding;
    Vector2 camcorner2 = level.Camera.position+new Vector2(320f, 180f)+padding;
    Vector2 tsp = (camcorner1-Position)/dir;
    Vector2 tsn = (camcorner2-Position)/dir;
    float texit = Math.Min(far,Math.Min(Math.Max(tsp.X,tsn.X),Math.Max(tsp.Y,tsn.Y)));
    float tenter = Math.Max(nrad+5,Math.Max(Math.Min(tsp.X,tsn.X),Math.Min(tsp.Y,tsn.Y)));
    for(float i=tenter; i<texit; i+=10){
      level.Particles.Emit(P_DissipateDense,1, Position+i*dir, new Vector2(5,5),angle+Math.Sign(dt));
      if(randSource.NextFloat()<0.15){
        level.Particles.Emit(P_DissipateSparse,1, Position+i*dir, new Vector2(5,5),angle+Math.Sign(dt));
      }
    }
  }
  public void handlePlayer(){
    if(!Collidable || far<nradkill) return;
    var dir = new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle));
    var orth = new Vector2(-(float)Math.Sin(angle),(float)Math.Cos(angle));
    Player p = base.Scene.CollideFirst<Player>(Position+dir*nradkill,Position+dir*far)??
      base.Scene.CollideFirst<Player>(Position+dir*nradkill+orth*2,Position+dir*far+orth*2)??
      base.Scene.CollideFirst<Player>(Position+dir*nradkill-orth*2,Position+dir*far-orth*2);
    if(p!=null){
      p.Die(new Vector2(0,0));
    }
  }

  public override void Render(){
    //frame=(frame+1)%40;
    if(frame==0)beamSprite.Play("shoot",true);
    var dir = new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle));
    float dist = nrad;
    beamSprite.Rotation=angle;
    for(int i=0; i<16; i++){
      if(dist+beamSprite.width>far) break;
      beamSprite.RenderPosition=Position+dir*dist;
      beamSprite.Render();
      dist+=beamSprite.width;
    }
    if(far-dist>0 && dist+beamSprite.width>far){
      var o = beamSprite.Texture.ClipRect;
      float ratio = (far-dist)/beamSprite.width;
      Draw.SpriteBatch.Draw(
        beamSprite.Texture.Texture.Texture_Safe, Position+dist*dir, 
        new Rectangle(o.X,o.Y,(int)Math.Ceiling(o.Width*ratio),o.Height), 
        beamSprite.Color, beamSprite.Rotation, 
        (beamSprite.Origin - beamSprite.Texture.DrawOffset) / beamSprite.Texture.ScaleFix, 
        beamSprite.Scale * beamSprite.Texture.ScaleFix, 0, 0f);
      
      var ho = Position+dir*far;
      Draw.SpriteBatch.Draw(Draw.Pixel.Texture.Texture_Safe, new Rectangle((int)ho.X-5,(int)ho.Y-5,10,10), 
        Draw.Pixel.ClipRect, new Color(1f,0f,0f,1f));
   
    }
  }
}
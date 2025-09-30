


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked(true)]
public class FastDebris:Actor{
  Int2 intpos=>Int2.Round(Position);
  Int2 radius;
  public Int2 Radius {
    set {
      radius = value;
      Collider = new Hitbox(value.x*2,value.y*2,-value.x,-value.y);
    }
  }
  public Vector2 speed;
  public Collision onCollideV;
  public Collision onCollideH;
  public bool onGround;
  public FastDebris(Vector2 pos):base(Int2.Round(pos)){
    Tag = Tags.Persistent;
    Depth = 2000;
    radius = new(2,2);
    Collider = new Hitbox(4,4,-2,-2);
    onCollideH=defaultOch;
    onCollideV=defaultOcv;
    SquishCallback=defaultSquish;
    //hooks.enable();
  }
  void defaultSquish(CollisionData d){
    RemoveSelf();
  }
  void defaultOch(CollisionData d){
    //speed.X = Math.Abs(speed.X)>60? speed.X*-0.8f:0; 
    speed.X*=-0.8f;
  }
  void defaultOcv(CollisionData d){
    speed.Y = Math.Abs(speed.Y)>60? speed.Y*-0.4f:0; 
  }
  void updateCollision(bool useStructure){
    if(!useStructure){
      MoveH(speed.X*Engine.DeltaTime, onCollideH);
      MoveV(speed.Y*Engine.DeltaTime, onCollideV);
      onGround = OnGround();
    } else {
      fastMoveH(speed.X*Engine.DeltaTime);
      fastMoveV(speed.Y*Engine.DeltaTime);
      onGround=SolidMiptree.Test(Int2.Round(Position+Vector2.UnitY),radius,CollisionDirection.ground)!=null;
    }
  }
  public void fastMoveH(float moveH){
    movementCounter.X += moveH;
    int num = (int)Math.Round(movementCounter.X, MidpointRounding.ToEven);
    movementCounter.X-=num;
    if(num==0) return;
    int dir=Math.Sign(num);
    Int2 pos = intpos;
    CollisionDirection d = Util.getCollisionDir(Vector2.UnitX*dir);
    //DebugConsole.Write(d.ToString());
    for(int i=0; i!=num; i+=dir){
      Entity e = SolidMiptree.Test(new(pos.x+i+dir,pos.y),radius,d);
      if(e!=null){
        onCollideH(new(){
          Hit=e as Platform, 
          TargetPosition=new(pos.x+num,pos.y),
          Direction=Vector2.UnitX*dir,
          Moved=Vector2.UnitX*dir*i
        });
        return;
      } else Position.X+=dir;
    }
  }
  public void fastMoveV(float moveV){
    movementCounter.Y += moveV;
    int num = (int)Math.Round(movementCounter.Y, MidpointRounding.ToEven);
    movementCounter.Y-=num;
    if(num==0) return;
    int dir=Math.Sign(num);
    Int2 pos = intpos;
    CollisionDirection d = Util.getCollisionDir(Vector2.UnitY*dir);
    for(int i=0; i!=num; i+=dir){
      Entity e = SolidMiptree.Test(new(pos.x,pos.y+i+dir),radius,d);
      if(e!=null){
        onCollideV(new(){
          Hit=e as Platform, 
          TargetPosition=new(pos.x,pos.y+num),
          Direction=Vector2.UnitY*dir,
          Moved=Vector2.UnitY*dir*i
        });
        return;
      } else Position.Y+=dir;
    }
  }
  public static void UpdateDebris(Level lv){
    var l = lv.Tracker.GetEntities<FastDebris>();
    //14 chosen completely arbitrarily
    if(l.Count<14)foreach(FastDebris e in l)e.updateCollision(false);
    else {
      SolidMiptree.Construct(lv,((IntRect)lv.Bounds).expandAll_(32));
      foreach(FastDebris e in l)e.updateCollision(true);
    }
  }

  static bool Hook(On.Celeste.Actor.orig_MoveHExact orig, Actor a, int move, Collision oncollide, Solid pusher){
    if(a is FastDebris d && pusher != null){
      d.NaiveMove(Vector2.UnitX*move);
      return false;
    }
    return orig(a,move,oncollide,pusher);
  }
  static bool Hook(On.Celeste.Actor.orig_MoveVExact orig, Actor a, int move, Collision oncollide, Solid pusher){
    if(a is FastDebris d && pusher != null){
      d.NaiveMove(Vector2.UnitY*move);
      return false;
    }
    return orig(a,move,oncollide,pusher);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Actor.MoveHExact+=Hook;
    On.Celeste.Actor.MoveVExact+=Hook;
  },()=>{
    On.Celeste.Actor.MoveHExact-=Hook;
    On.Celeste.Actor.MoveVExact-=Hook;
  });
}

[Pooled]
public class TileDebris:FastDebris{
  Image image;
  bool hasHitGround = false;
  char tileset;
  float rotateSign;
  float fadeLerp;
  float lifeTimer;
  float alpha;
  public TileDebris():base(Vector2.Zero){
    base.Tag = Tags.Persistent;
    Add(image = new Image(null));
    onCollideV = OnCollideV;
    onCollideH = OnCollideH;
  }
  void OnCollideV(CollisionData d){
    if (speed.Y > 0f)hasHitGround=true;
    speed.Y *= -0.6f;
    if (speed.Y < 0f && speed.Y > -50f)speed.Y=0;
    if (speed.Y != 0f || !hasHitGround){
      ImpactSfx(Math.Abs(speed.Y));
    }
  }
  void OnCollideH(CollisionData d){
    speed.X = speed.X*-0.8f;
    if(Math.Abs(speed.X)>50)ImpactSfx(Math.Abs(speed.X));
  }
  bool playSound = true;
  public void ImpactSfx(float spd){
    if (playSound){
      string path = "event:/game/general/debris_dirt";
      if (tileset == '4' || tileset == '5' || tileset == '6' || tileset == '7' || tileset == 'a' || tileset == 'c' || tileset == 'd' || tileset == 'e' || tileset == 'f' || tileset == 'd' || tileset == 'g'){
        path = "event:/game/general/debris_stone";
      } else if (tileset == '9'){
        path = "event:/game/general/debris_wood";
      }
      Audio.Play(path, Position, "debris_velocity", Calc.ClampedMap(spd, 0f, 150f));
    }
  }
  public override void Update(){
    base.Update();
    image.Rotation += Math.Abs(speed.X) * (float)rotateSign * Engine.DeltaTime;
    if (fadeLerp < 1f) fadeLerp = Calc.Approach(fadeLerp, 1f, 2f * Engine.DeltaTime);

    speed.X = Calc.Approach(speed.X, 0f, ( onGround? 50f : 20f) * Engine.DeltaTime);
    if (!onGround){
      speed.Y = Calc.Approach(speed.Y, 100f, 400f * Engine.DeltaTime);
    }
    if (lifeTimer > 0f) lifeTimer-=Engine.DeltaTime;
    else if (alpha > 0f){
      alpha -= 4f * Engine.DeltaTime;
      if (alpha <= 0f) RemoveSelf();
    }
    image.Color = Color.Lerp(Color.White, Color.Gray, fadeLerp) * alpha;
  }
  public TileDebris Init(Vector2 pos, char tileset, bool playSound = true){
    Position = pos;
    this.tileset = tileset;
    this.playSound = playSound;
    lifeTimer = Calc.Random.Range(0.6f, 2.6f);
    alpha = 1f;
    hasHitGround = false;
    speed = Vector2.Zero;
    fadeLerp = 0f;
    rotateSign = Calc.Random.Choose(1, -1);
    if(GFX.FGAutotiler.TryGetCustomDebris(out var path, tileset)){
      List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("debris/" + path);
      image.Texture = Calc.Random.Choose(atlasSubtextures);
    }else if (GFX.Game.Has("debris/" + tileset)){
      image.Texture = GFX.Game["debris/" + tileset];
    }else{
      image.Texture = GFX.Game["debris/1"];
    }

    image.CenterOrigin();
    image.Color = Color.White * alpha;
    image.Rotation = Calc.Random.NextAngle();
    image.Scale.X = Calc.Random.Range(0.5f, 1f);
    image.Scale.Y = Calc.Random.Range(0.5f, 1f);
    image.FlipX = Calc.Random.Chance(0.5f);
    image.FlipY = Calc.Random.Chance(0.5f);
    return this;
  }
  public TileDebris RandFrom(Vector2 liftspeed){
    if(DebrisSource.fn is {} fn){
      speed = fn(Position, liftspeed);
    } else {
      float length = Calc.Random.Range(30, 40);
      speed = -Vector2.UnitY*length;
      speed = speed.Rotate(Calc.Random.Range(-MathF.PI, MathF.PI))+liftspeed;
    }
    return this;
  }
}


public class DebrisSource:IDisposable{
  public delegate Vector2 SpeedCalcLs(Vector2 debrisPos, Vector2 liftspeed);
  public delegate Vector2 SpeedCalc(Vector2 debrisPos);
  public static SpeedCalcLs fn = null;
  bool root;
  public DebrisSource(SpeedCalcLs f){
    root = fn==null;
    if(root) fn=f;
  }
  public DebrisSource(SpeedCalc f):this(Vector2 (Vector2 debrisPos, Vector2 ls)=>f(debrisPos)+ls){}
  public DebrisSource(Vector2 from, float angle=MathF.PI/12f, int high=40, float frac = 0.75f):this((pos, ls)=>{
    float length = Calc.Random.Range(high*frac, high);
    Vector2 speed = (pos - from).SafeNormalize(length);
    return speed.Rotate(Calc.Random.Range(-angle, angle))+ls;
  }){}
  public DebrisSource(Vector2 from, Vector2 add, float angle=MathF.PI/12f, int high=40, float frac = 0.75f):this((pos, ls)=>{
    float length = Calc.Random.Range(high*frac, high);
    Vector2 speed = (pos - from).SafeNormalize(length)+add*(length/high);
    return speed.Rotate(Calc.Random.Range(-angle, angle))+ls;
  }){}
  void IDisposable.Dispose() {
    if(root) fn=null;
  }
}
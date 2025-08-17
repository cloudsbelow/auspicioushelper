



using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper.Wrappers;
[Tracked]
[CustomEntity("auspicioushelper/spinner")]
public class Spinner:CrystalStaticSpinner, ISimpleEnt{
  public Template parent {get;set;} 
  static int uidctr = 0;
  int id;
  public static CrystalColor GetColor(EntityData d){
    if(!string.IsNullOrWhiteSpace(d.Attr("customColor"))) return CrystalColor.Rainbow;
    if(Enum.TryParse<CrystalColor>(d.Attr("color"), ignoreCase: true, out var res)) return res;
    return CrystalColor.Blue;
  }
  public static CrystalColor GetAndPrint(EntityData d){
    var res = GetColor(d);
    DebugConsole.Write(res.ToString());
    return res;
  }
  public Spinner(EntityData d, Vector2 offset):base(d.Position+offset, false, GetColor(d)){
    id = uidctr++;
    hooks.enable();
    makeFiller = d.Bool("makeFiller",true);
    if(!string.IsNullOrWhiteSpace(d.Attr("customColor"))){
      hasCustomCOlor = true;
      customColor = Util.hexToColor(d.Attr("customColor"));
    }
  }
  bool hvisible = true;
  bool hcollidable = true;
  bool scollidable = false;
  bool inView = false;
  bool makeFiller = true;
  Color customColor;
  bool hasCustomCOlor;
  public override void Update(){
    if(!inView){
      Collidable = false;
      if(InView()){
        inView=true;
        if(!expanded) CreateSprites();
        if(color == CrystalColor.Rainbow && !hasCustomCOlor) UpdateHue();
      }
      Visible = hvisible && inView;
    } else {
      //base.Update();
      if(color == CrystalColor.Rainbow && !hasCustomCOlor && base.Scene.OnInterval(0.08f, offset)) UpdateHue();
      if (base.Scene.OnInterval(0.25f, offset) && !InView()){
        inView = false; Visible = false; Collidable = false;
      }
      if (base.Scene.OnInterval(0.05f, offset)){
        Player entity = UpdateHook.cachedPlayer;
        if(entity != null) scollidable = Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f;
      }
      Collidable = hcollidable && scollidable;
      Visible = hvisible && inView;
    }
    if(filler!=null) filler.Position=Position;
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0){
      hvisible = vis>0;
      Visible = vis>0;
    }
    if(col!=0){
      hcollidable = col>0;
      Collidable = col>0;
    }
    if(act!=0) Active = act>0;
  }
  public Vector2 toffset {get;set;}
  public void relposTo(Vector2 loc, Vector2 liftspeed){
    Position=(loc+toffset).Round();
  }
  public void setOffset(Vector2 ppos){
    toffset = Position-ppos;
    Depth+=parent.depthoffset;
  }
  static bool CheckSolidHook(On.Celeste.CrystalStaticSpinner.orig_SolidCheck orig, CrystalStaticSpinner self, Vector2 pos){
    if(self is Spinner s){
      return s.parent?.fgt?.CollidePoint(pos)??false;
    }
    return orig(self,pos);
  }
  //On.Celeste.CrystalStaticSpinner.orig_CreateSprites orig, CrystalStaticSpinner self
  bool SolidCheck_(Vector2 pos){
    if(parent == null) return (Scene as Level)?.SolidTiles?.CollidePoint(pos)??false;
    return parent.fgt?.CollidePoint(pos)??false;
  }
  void CreateSpritesOther(){
    if (expanded)return;
    Calc.PushRandom(randomSeed);
    List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(fgTextureLookup[this.color]);
    MTexture mTexture = Calc.Random.Choose(atlasSubtextures);
    Color color = Color.White;
    if(hasCustomCOlor) color = customColor;
    else if (this.color == CrystalColor.Rainbow) color = GetHue(Position);

    if(!SolidCheck_(new Vector2(base.X - 4f, base.Y - 4f))){
      Add(new Image(mTexture.GetSubtexture(0, 0, 14, 14)).SetOrigin(12f, 12f).SetColor(color));
    }
    if(!SolidCheck_(new Vector2(base.X + 4f, base.Y - 4f))){
      Add(new Image(mTexture.GetSubtexture(10, 0, 14, 14)).SetOrigin(2f, 12f).SetColor(color));
    }
    if(!SolidCheck_(new Vector2(base.X + 4f, base.Y + 4f))){
      Add(new Image(mTexture.GetSubtexture(10, 10, 14, 14)).SetOrigin(2f, 2f).SetColor(color));
    }

    if(!SolidCheck_(new Vector2(base.X - 4f, base.Y + 4f))){
      Add(new Image(mTexture.GetSubtexture(0, 10, 14, 14)).SetOrigin(12f, 2f).SetColor(color));
    }

    if(makeFiller) foreach (CrystalStaticSpinner entity in base.Scene.Tracker.GetEntities<Spinner>()){
      if(entity is Spinner o && o.makeFiller){
        if(parent != o.parent) continue;
        if (o.id > id && (entity.Position - Position).LengthSquared() < 576f){
          AddSprite((Position + entity.Position) / 2f - Position);
        }
      }
    }

    base.Scene.Add(border = new Border(this, filler));
    expanded = true;
    Calc.PopRandom();
    if(hasCustomCOlor && filler!=null) foreach(var f in filler.Components) if(f is Image i){
      i.Color = customColor;
    }
  }
  Color getColor(){
    Color color = Color.White;
    if (this.color == CrystalColor.Red) color = Calc.HexToColor("ff4f4f");
    else if (this.color == CrystalColor.Blue) color = Calc.HexToColor("639bff");
    else if (this.color == CrystalColor.Purple) color = Calc.HexToColor("ff4fef");
    return color;
  }
  public void destroy(bool yes){
    if(yes)SpinnerDebris.CreateBurst(Scene,4,Position,parent.gatheredLiftspeed,getColor());
    RemoveSelf();
  }

  static void addSpritesHook(On.Celeste.CrystalStaticSpinner.orig_CreateSprites orig, CrystalStaticSpinner self){
    if(self is Spinner s) s.CreateSpritesOther();
    else orig(self);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.CrystalStaticSpinner.SolidCheck+=CheckSolidHook;
    On.Celeste.CrystalStaticSpinner.CreateSprites+=addSpritesHook;
  },()=>{
    On.Celeste.CrystalStaticSpinner.SolidCheck-=CheckSolidHook;
    On.Celeste.CrystalStaticSpinner.CreateSprites-=addSpritesHook;
  },auspicioushelperModule.OnEnterMap);
}

public class DustSpinner:DustStaticSpinner, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public DustSpinner(EntityData e, Vector2 o):base(e,o){}
  public override void Awake(Scene s){
    Depth = Math.Min(Depth,-50);
    base.Awake(s);
  }
}

[Pooled]
public class SpinnerDebris:FastDebris{
  Image image;
  Color color;
  float percent;
  float duration;
  public SpinnerDebris():base(Vector2.Zero){
    base.Depth = -9990;
    Radius = new Int2(1,1);
    image = new Image(GFX.Game["particles/shard"]);
    image.CenterOrigin();
    Add(image);
    onCollideH = (CollisionData d)=>{
      speed.X=speed.X*-0.8f;
    };
    onCollideV = (CollisionData d)=>{
      if (Math.Sign(speed.X) != 0) speed.X += Math.Sign(speed.X) * 5;
      else speed.X += Calc.Random.Choose(-1, 1) * 5;
      speed.Y *= -1f;
    };
  }
  public SpinnerDebris Init(Vector2 position, Color color, Vector2 speed){
    Position = position;
    image.Color = (this.color = color);
    image.Scale = Vector2.One;
    percent = 0f;
    duration = Calc.Random.Range(1f, 2f);
    this.speed = speed;
    return this;
  }
  public override void Update() {
    base.Update();
    if (percent > 1f){
      RemoveSelf();
      return;
    }
    percent += Engine.DeltaTime / duration;
    speed.X = Util.Clamp(Calc.Approach(speed.X, 0f, Engine.DeltaTime * 20f),-10000,10000);
    speed.Y = Util.Clamp(speed.Y+200f*Engine.DeltaTime,-10000,10000);

    if (speed.Length() > 0f)image.Rotation = speed.Angle();
    image.Scale = Vector2.One * Calc.ClampedMap(percent, 0.8f, 1f, 1f, 0f);
    image.Scale.X *= Calc.ClampedMap(speed.Length(), 0f, 400f, 1f, 2f);
    image.Scale.Y *= Calc.ClampedMap(speed.Length(), 0f, 400f, 1f, 0.2f);
    if (base.Scene.OnInterval(0.05f)){
      (base.Scene as Level).ParticlesFG.Emit(CrystalDebris.P_Dust, Position);
    }
  }
  public override void Render(){
    if(!MaterialPipe.clipBounds.CollidePoint(Int2.Round(Position))) return;
    image.Color = Color.Black;
    image.Position = new Vector2(-1f, 0f);
    image.Render();
    image.Position = new Vector2(0f, -1f);
    image.Render();
    image.Position = new Vector2(1f, 0f);
    image.Render();
    image.Position = new Vector2(0f, 1f);
    image.Render();
    image.Position = Vector2.Zero;
    image.Color = color;
    base.Render();
  }
  public static void CreateBurst(Scene scene, int count, Vector2 loc, Vector2 ls, Color color){
    for(int i=0; i<count; i++){
      bool isotropic = Calc.Random.Chance(0.4f);
      float angle = Util.randomizeAngleQuad(isotropic?Calc.Random.Range(0,MathF.PI/2):Calc.Random.Range(0,MathF.PI/6));
      Vector2 speed = Calc.AngleToVector(angle+Calc.Random.Range(-0.3f,0.3f),!isotropic?Calc.Random.Range(260,400):Calc.Random.Range(160,200));
      scene.Add(Engine.Pooler.Create<SpinnerDebris>().Init(loc+speed.SafeNormalize(Calc.Random.Range(0,5)), color,ls+speed));
    }
  }
}
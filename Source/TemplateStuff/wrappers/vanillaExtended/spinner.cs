



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
    if(Enum.TryParse<CrystalColor>(d.Attr("color"), ignoreCase: true, out var r))return r;
    return d.Name == "spinner"? CrystalColor.Blue:CrystalColor.Rainbow;
  }
  string fancyRecolor;
  bool dontStun;
  MTexture debrisTex;
  public static Color ColorFromCrystal(CrystalColor c)=>c switch{
    CrystalColor.Blue=>new Color(99, 155, 255), 
    CrystalColor.Red=>new Color(235, 42, 58), 
    CrystalColor.Purple=>new Color(199, 42, 235), 
    _=>new Color(200,200,200)
  };
  public Spinner(EntityData d, Vector2 offset):base(d.Position+offset, false, GetColor(d)){
    id = uidctr++;
    hooks.enable();
    makeFiller = d.Bool("makeFiller",true);
    fancyRecolor = d.StringOrNull("fancy");
    debrisTex = GFX.Game["particles/shard"];
    if(fancyRecolor.HasContent())
      debrisTex = Util.ColorRemap.Get(fancyRecolor).RemapTexTintFirst(debrisTex, ColorFromCrystal(color));
    
    if(!string.IsNullOrWhiteSpace(d.Attr("customColor"))){
      hasCustomCOlor = true;
    }
    customColor = Util.hexToColor(d.Attr("customColor", "ffffff"));
    numdebris = d.Int("numDebris",4);
    dreamThru = d.Bool("dreamThru",false);
    neverClip = d.Bool("neverClip",false);
    Get<PlayerCollider>().OnCollide = OtherOnPlayer;
    Depth = d.Int("depth", -8500);
    dontStun = d.Name!="spinner";
    borderColor = Util.hexToColor(d.Attr("border","000"));
  }
  void OtherOnPlayer(Player p){
    if(dreamThru && (p.StateMachine.State == Player.StDreamDash || Import.CommunalHelperIop.InTunnel(p))) return;
    p.Die((p.Position - Position).SafeNormalize());
  }
  bool hvisible = true;
  bool hcollidable = true;
  bool scollidable = false;
  bool inView = false;
  bool makeFiller = true;
  Color customColor;
  Color borderColor = Color.Red;
  bool hasCustomCOlor;
  bool dreamThru;
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
      if (dontStun||base.Scene.OnInterval(0.05f, offset)){
        Player entity = UpdateHook.cachedPlayer;
        if(entity != null) scollidable = Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f;
      }
      Collidable = hcollidable && scollidable;
      Visible = hvisible && inView;
    }
    if(filler!=null){
      filler.Position=Position;
      filler.Visible = Visible;
    }
    if(border!=null){
      border.Position=Position;
      border.Visible = Visible;
    }
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(InView()){
      inView = true;
      if(!expanded) CreateSprites();
      if(color == CrystalColor.Rainbow && !hasCustomCOlor) UpdateHue();
      Visible = hvisible && inView;
    }
  }
  new bool InView()=>MaterialPipe.clipBounds.CollidePointExpand(Int2.Round(Position),16);
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0){
      hvisible = vis>0;
      Visible = vis>0;
      if(filler!=null) filler.Visible=Visible;
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
  bool neverClip;
  static bool CheckSolidHook(On.Celeste.CrystalStaticSpinner.orig_SolidCheck orig, CrystalStaticSpinner self, Vector2 pos){
    if(self is Spinner s){
      if(s.neverClip) return false;
      return s.parent?.fgt?.CollidePoint(pos)??false;
    }
    return orig(self,pos);
  }
  //On.Celeste.CrystalStaticSpinner.orig_CreateSprites orig, CrystalStaticSpinner self
  bool SolidCheck_(Vector2 pos){
    if(neverClip)return false;
    if(parent == null) return (Scene as Level)?.SolidTiles?.CollidePoint(pos)??false;
    return parent.fgt?.CollidePoint(pos)??false;
  }
  public class SpinnerFiller:Entity{
    public SpinnerFiller(Vector2 position):base(position){}
  }
  public class CustomBorder:CrystalStaticSpinner.Border{
    List<(MTexture, Vector2, float rot)> items;
    public CustomBorder(Entity parent, Entity fill, List<(MTexture, Vector2, float)> items):base(parent,fill){
      this.items = items;
    }
    public override void Render() {
      Spinner parent = (Spinner)drawing[0];
      if(!parent.Visible) return;
      Vector2 rpos = parent.Position.Round();
      foreach(var (tex, pos, rot) in items){
        tex.Draw(rpos+pos, Vector2.Zero, parent.borderColor, Vector2.One, rot);
      }
    }
  }
  void CreateSpritesOther(){
    if (expanded)return;
    Calc.PushRandom(randomSeed);
    List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(fgTextureLookup[this.color]);
    MTexture mTexture = Calc.Random.Choose(atlasSubtextures);
    MTexture borderMain = BorderGenerator.GetBorder(mTexture);
    List<(MTexture, Vector2, float)> borderList = new();
    if(fancyRecolor != null) mTexture = Util.ColorRemap.Get(fancyRecolor).RemapTex(mTexture);
    Color color = Color.White;
    if(hasCustomCOlor) color = customColor;
    else if (this.color == CrystalColor.Rainbow) color = GetHue(Position);

    if(!neverClip){
      if(!SolidCheck_(new Vector2(base.X - 4f, base.Y - 4f))){
        Add(new Image(mTexture.GetSubtexture(0, 0, 14, 14)).SetOrigin(12f, 12f).SetColor(color));
        borderList.Add((borderMain.GetSubtexture(0,0,15,15), new(-13,-13), 0));
      }
      if(!SolidCheck_(new Vector2(base.X + 4f, base.Y - 4f))){
        Add(new Image(mTexture.GetSubtexture(10, 0, 14, 14)).SetOrigin(2f, 12f).SetColor(color));
        borderList.Add((borderMain.GetSubtexture(11,0,15,15), new(-2,-13), 0));
      }
      if(!SolidCheck_(new Vector2(base.X + 4f, base.Y + 4f))){
        Add(new Image(mTexture.GetSubtexture(10, 10, 14, 14)).SetOrigin(2f, 2f).SetColor(color));
        borderList.Add((borderMain.GetSubtexture(11,11,15,15), new(-2,-2), 0));
      }
      if(!SolidCheck_(new Vector2(base.X - 4f, base.Y + 4f))){
        Add(new Image(mTexture.GetSubtexture(0, 10, 14, 14)).SetOrigin(12f, 2f).SetColor(color));
        borderList.Add((borderMain.GetSubtexture(0,11,15,15), new(-13,-2), 0));
      }
    } else {
      Add(new Image(mTexture).SetColor(color).SetOrigin(12f,12f));
      borderList.Add((borderMain, new(-13,-13), 0));
    }

    if(makeFiller) foreach (CrystalStaticSpinner entity in base.Scene.Tracker.GetEntities<Spinner>()){
      if(entity is Spinner o && o.makeFiller){
        if(parent != o.parent) continue;
        if (o.id > id && (entity.Position - Position).LengthSquared() < 576f){
          if (filler == null){
            base.Scene.Add(filler = new SpinnerFiller(Position));
            filler.Depth = base.Depth + 1;
          }
          Vector2 offsetPos = ((Position + entity.Position) / 2f - Position).Round();
          float imRot = (float)Calc.Random.Choose(0, 1, 2, 3) * (MathF.PI / 2f);
          MTexture ftex = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(bgTextureLookup[this.color]));
          borderList.Add((BorderGenerator.GetBorder(ftex), offsetPos-new Vector2(ftex.Width/2+1, ftex.Height/2+1).Rotate(imRot), imRot));
          if(fancyRecolor!=null) ftex=Util.ColorRemap.Get(fancyRecolor).RemapTex(ftex);
          Image image = new Image(ftex);
          image.Position = offsetPos;
          image.Rotation = imRot;
          image.CenterOrigin();
          image.Color=color;
          filler.Add(image);
        }
      }
    }

    if(borderColor != new Color(0,0,0,0))base.Scene.Add(border = new CustomBorder(this, filler, borderList));
    expanded = true;
    Calc.PopRandom();
    if(hasCustomCOlor && filler!=null) foreach(var f in filler.Components) if(f is Image i){
      i.Color = customColor;
    }
    if(parent is {} temp){
      if(filler != null)temp.AddNewEnts([filler]);
      if(border != null)temp.AddNewEnts([border]);
    }
  }
  void ITemplateChild.AddAllChildren(List<Entity> l){
    l.Add(this);
    if(filler!=null)l.Add(filler);
    if(border!=null)l.Add(border);
  }
  Color getColor(){
    Color color = Color.White;
    if (this.color == CrystalColor.Red) color = Calc.HexToColor("ff4f4f");
    else if (this.color == CrystalColor.Blue) color = Calc.HexToColor("639bff");
    else if (this.color == CrystalColor.Purple) color = Calc.HexToColor("ff4fef");
    return color;
  }
  int numdebris = 4;
  public void destroy(bool yes){
    if(yes){
      Color useColor = fancyRecolor.HasContent()? customColor:
        (customColor.ToVector4()*ColorFromCrystal(color).ToVector4()).toColor();
      SpinnerDebris.CreateBurst(Scene,numdebris,Position,parent.gatheredLiftspeed,useColor,debrisTex);
    }
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
  MTexture shard = GFX.Game["particles/shard"];
  public SpinnerDebris():base(Vector2.Zero){
    base.Depth = -9990;
    Radius = new Int2(1,1);
    image = new Image(shard);
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
  public SpinnerDebris Init(Vector2 position, Color color, Vector2 speed, MTexture debrisTexture){
    Position = position;
    image.Texture = debrisTexture;
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
  public static void CreateBurst(Scene scene, int count, Vector2 loc, Vector2 ls, Color color, MTexture debrisTex){
    for(int i=0; i<count; i++){
      bool isotropic = Calc.Random.Chance(0.4f);
      float angle = Util.randomizeAngleQuad(isotropic?Calc.Random.Range(0,MathF.PI/2):Calc.Random.Range(0,MathF.PI/6));
      Vector2 speed = Calc.AngleToVector(angle+Calc.Random.Range(-0.3f,0.3f),!isotropic?Calc.Random.Range(260,400):Calc.Random.Range(160,200));
      scene.Add(Engine.Pooler.Create<SpinnerDebris>().Init(loc+speed.SafeNormalize(Calc.Random.Range(0,5)), color,ls+speed, debrisTex));
    }
  }
}
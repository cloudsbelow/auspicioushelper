



using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;
[Tracked]
public class Spinner:CrystalStaticSpinner, ISimpleEnt{
  public Template parent {get;set;} 
  static int uidctr = 0;
  int id;
  public static CrystalColor GetColor(EntityData d){
    if(Enum.TryParse<CrystalColor>(d.Attr("color"), ignoreCase: true, out var res)) return res;
    return CrystalColor.Blue;
  }
  public Spinner(EntityData d, Vector2 offset):base(d.Position+offset, false, GetColor(d)){
    id = uidctr++;
    hooks.enable();
  }
  bool hvisible = true;
  bool hcollidable = true;
  bool scollidable = false;
  bool inView = false;
  public override void Update(){
    if(!inView){
      Collidable = false;
      if(InView()){
        inView=true;
        if(!expanded) CreateSprites();
        if(color == CrystalColor.Rainbow) UpdateHue();
      }
      Visible = hvisible && inView;
    } else {
      base.Update();
      if(color == CrystalColor.Rainbow && base.Scene.OnInterval(0.08f, offset)) UpdateHue();
      if (base.Scene.OnInterval(0.25f, offset) && !InView()){
        inView = false; Visible = false; Collidable = false;
      }
      if (base.Scene.OnInterval(0.05f, offset)){
        Player entity = base.Scene.Tracker.GetEntity<Player>();
        if(entity != null) scollidable = Math.Abs(entity.X - base.X) < 128f && Math.Abs(entity.Y - base.Y) < 128f;
      }
      Collidable = hcollidable && scollidable;
      Visible = hvisible && inView;
    }
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
    Position=loc+toffset;
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
    return parent.fgt?.CollidePoint(pos)??false;
  }
  void CreateSpritesOther(){
    if (expanded)return;
    Calc.PushRandom(randomSeed);
    List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures(fgTextureLookup[this.color]);
    MTexture mTexture = Calc.Random.Choose(atlasSubtextures);
    Color color = Color.White;
    if (this.color == CrystalColor.Rainbow){
      color = GetHue(Position);
    }

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

    foreach (CrystalStaticSpinner entity in base.Scene.Tracker.GetEntities<Spinner>()){
      if(entity is Spinner o){
        if(parent != o.parent) continue;
        if (o.id > id && (entity.Position - Position).LengthSquared() < 576f){
          AddSprite((Position + entity.Position) / 2f - Position);
        }
      }
    }

    base.Scene.Add(border = new Border(this, filler));
    expanded = true;
    Calc.PopRandom();
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
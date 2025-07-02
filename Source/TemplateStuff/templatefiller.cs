


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/templateFiller")]
public class templateFiller:Entity{
  
  internal string name;
  internal LevelData roomdat;
  Rectangle tr;
  // char[,] fgtiles;
  // char[,] bgtiles;
  internal VirtualMap<char> fgt;
  internal VirtualMap<char> bgt;
  internal Vector2 offset;
  internal Vector2 origin=>-offset+Position;
  internal List<DecalData> decals = new List<DecalData>();
  internal List<EntityData> ChildEntities = new();
  Vector2 leveloffset;
  Vector2 tiletlc => leveloffset+Position;
  internal MarkedRoomParser.TemplateRoom room;
  internal templateFiller(EntityData d, Vector2 offset):base(d.Position){
    this.Collider = new Hitbox(d.Width, d.Height);
    name = d.Attr("template_name","");
    tr = new Rectangle((int)d.Position.X/8, (int)d.Position.Y/8, d.Width/8,d.Height/8);
    this.offset = d.Nodes.Count()>0?d.Position-d.Nodes[0]:Vector2.Zero;
    leveloffset = offset;
  }
  internal templateFiller(){}
  public override void Awake(Scene scene){
    RemoveSelf();  
  }

  internal void setTiles(string fg, string bg){
    Regex regex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
    char[,] fgtiles = new char[tr.Width,tr.Height];
    char[,] bgtiles = new char[tr.Width,tr.Height];
    bool keepfg = false;
    bool keepbg = false;
    string[] fglines= regex.Split(fg);
    string[] bglines= regex.Split(bg);
    for(int i=0; i<tr.Height; i++){
      for(int j=0; j<tr.Width; j++){
        int r = i+tr.Top;
        int c = j+tr.Left;
        if(r>=0 && c>=0 && r<fglines.Length && c<fglines[r].Length){
          fgtiles[j,i]=fglines[r][c];
          keepfg |= fglines[r][c]!='0';
        } else {
          fgtiles[j,i]='0';
        }
        if(r>=0 && c>=0 && r<bglines.Length && c<bglines[r].Length){
          bgtiles[j,i]=bglines[r][c];
          keepbg |= bglines[r][c]!='0';
        } else {
          bgtiles[j,i]='0';
        }
      }
    }

    // Autotiler.Behaviour b = new Autotiler.Behaviour{
    //   EdgesIgnoreOutOfLevel = true,
    //   PaddingIgnoreOutOfLevel = true,
    //   EdgesExtend = false,
    // };
    fgt = keepfg? new VirtualMap<char>(fgtiles):null;
    bgt = keepbg? new VirtualMap<char>(bgtiles):null;
  }

  public class TileView{
    VirtualMap<MTexture> tiles;
    VirtualMap<List<AnimatedTiles.Tile>> anims;
    AnimatedTilesBank bank;
    public bool hasAnimatedtiles=false; 
    int w;
    int h;
    public void Fill(TileGrid t, AnimatedTiles a, int xstart, int ystart, int w, int h){
      this.w=w; this.h=h;
      tiles = new(w,h);
      anims = new(w,h);
      bank = a.Bank;
      for(int i=0; i<w; i++){
        for(int j=0; j<h; j++){
          int x = i+xstart;
          int y = j+ystart;
          tiles[i,j] = t.Tiles[x,y];
          anims[i,j] = a.tiles[x,y];
          if(a.tiles[x,y]!=null) hasAnimatedtiles=true;
        }
      }
    }
    static TileView intercept;
    static Autotiler.Generated subhook(On.Celeste.Autotiler.orig_GenerateMap_VirtualMap1_bool orig, Autotiler self, VirtualMap<char> d, bool piol){
      if(intercept == null) return orig(self,d,piol);
      Autotiler.Generated ret;
      ret.TileGrid = new TileGrid(8,8,0,0);
      ret.TileGrid.Tiles = intercept.tiles;
      if(intercept.hasAnimatedtiles){
        ret.SpriteOverlay = new AnimatedTiles(intercept.w,intercept.h, intercept.bank);
        var from = intercept.anims;
        for(int x=0; x<intercept.w; x++){
          for(int y=0; y<intercept.h; y++){
            if(from[x,y]!=null){
              List<AnimatedTiles.Tile> arr = new();
              foreach(AnimatedTiles.Tile t in from[x,y]){
                arr.Add(new AnimatedTiles.Tile{
                  AnimationID = t.AnimationID,
                  Frame = t.Frame,
                  Scale = t.Scale,
                });
              }
              ret.SpriteOverlay.tiles[x,y] = arr;
            }
          }
        }
      } else {
        ret.SpriteOverlay = new AnimatedTiles(intercept.w,intercept.h,intercept.bank){Visible=false,Active=false};
      } 
      intercept = null;
      return ret;
    }
    static HookManager hooks = new HookManager(()=>{
      On.Celeste.Autotiler.GenerateMap_VirtualMap1_bool+=subhook;
    },()=>{
      On.Celeste.Autotiler.GenerateMap_VirtualMap1_bool-=subhook;
    });
    public void InterceptNext(){
      hooks.enable();
      intercept = this;
    }
  } 
  public bool created = false;
  public TileView Fgt = null;
  public TileView Bgt = null;
  public void initDynamic(Level l){
    if(created) return;
    created = true;
    if(fgt!=null){
      SolidTiles st = l.SolidTiles;
      Vector2 sto = ((tiletlc-st.Position)/8).Round();
      Fgt = new(); 
      Fgt.Fill(st.Tiles, st.AnimatedTiles,(int)sto.X,(int)sto.Y,tr.Width,tr.Height);
    }
    if(bgt!=null){
      BackgroundTiles st = l.BgTiles;
      Vector2 sto = ((tiletlc-st.Position)/8).Round();
      Bgt = new();
      Bgt.Fill(st.Tiles, st.AnimatedTiles,(int)sto.X,(int)sto.Y,tr.Width,tr.Height);
    }
  }
  public void initStatic(SolidTiles solid, BackgroundTiles back){
    if(created) return;
    created = true;
    if(fgt!=null){
      Vector2 sto = ((tiletlc-solid.Position)/8).Round();
      Fgt = new(); 
      Fgt.Fill(solid.Tiles, solid.AnimatedTiles,(int)sto.X,(int)sto.Y,tr.Width,tr.Height);
    }
    if(bgt!=null){
      Vector2 sto = ((tiletlc-back.Position)/8).Round();
      Bgt = new(); 
      Bgt.Fill(back.Tiles, back.AnimatedTiles,(int)sto.X,(int)sto.Y,tr.Width,tr.Height);
    }
  }
  public void AddTilesTo(Template tem){
    initDynamic(tem.SceneAs<Level>());
    if(Fgt != null){
      Fgt.InterceptNext();
      tem.addEnt(tem.fgt = new FgTiles(this, tem.virtLoc, tem.depthoffset));
    }
    if(Bgt != null){
      Bgt.InterceptNext();
      tem.addEnt(new BgTiles(this, tem.virtLoc, tem.depthoffset));
    }
  }
  

  internal TemplateBehaviorChain.Chain chain=null;
  
  public static templateFiller MkNestingFiller(EntityData internalTemplate,TemplateBehaviorChain.Chain chain = null){
    var f=new templateFiller();
    f.created = true;
    f.Fgt = null;
    f.Bgt = null;
    f.ChildEntities.Add(internalTemplate);
    f.Position = internalTemplate.Position;
    f.chain = chain;
    return f;
  }
}
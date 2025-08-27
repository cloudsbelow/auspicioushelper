

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using Celeste.Mod.Helpers;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

public class PartialTiles{
  static Dictionary<Autotiler.Tiles, List<ulong>> tiledict = new();
  public static MTexture tex = null;
  public static MTexture ttex = null;
  const ulong FULL = ulong.MaxValue;
  static void HookBefore(XmlElement tileset){
    if(tileset.HasAttribute("ausp_partialTiles")){
      tex = GFX.Game["tilesets/" + tileset.Attr("ausp_partialTiles")];
      ttex = GFX.Game["tilesets/" + tileset.Attr("path")];
      if(tex.Width!=ttex.Width || tex.Height != ttex.Height){
        DebugConsole.WriteFailure("Sizes of tileset and partial tile mask don't match",true);
      }
    } else {
      tex = (ttex = null);
    }
  }
  static void Add(Autotiler.Tiles tiles, ulong[,] masks){
    if(tex==null) return;
    List<ulong> l = new();
    Int2 po = new(ttex.ClipRect.X,ttex.ClipRect.Y);
    IntRect tlcbound = new(po.x,po.y,ttex.ClipRect.Width-8,ttex.ClipRect.Height-8);

    foreach(var t in tiles.Textures){
      Int2 offset = new(t.ClipRect.X,t.ClipRect.Y);
      if(t.Texture!=ttex.Texture || !tlcbound.CollidePointCompact(offset)){
        DebugConsole.WriteFailure("Mtexture not inside supposed parent in partial tiler", true);
      }
      //im so paranoid
      Int2 dif = offset-tlcbound.tlc;
      if((dif.y&7)!=0 || (dif.x&7)!=0){
        DebugConsole.WriteFailure("Tileset is not 8-px aligned",true);
      }
      var c = dif/8;
      l.Add(masks[c.x,c.y]);
    }
    tiledict.Add(tiles,l);
  }
  static void HookAfter(Autotiler.TerrainType ter){
    if(tex == null) return;
    int w = tex.ClipRect.Width;
    int h = tex.ClipRect.Height;
    Color[] data = new Color[w*h];
    tex.Texture.Texture.GetData(0,tex.ClipRect,data,0,data.Length);
    ulong[,] masks = new ulong[w/8,h/8];
    for(int x=0; x<w; x+=8){
      for(int y=0; y<h; y+=8){
        ulong dat = 0;
        ulong num = 1;
        for(int j=0; j<8; j++){
          for(int i=0; i<8; i++){
            Color c = data[(y+j)*w+x+i];
            if(c.R>64 && c.A>64) dat |= num;
            num<<=1;
          }
        }
        masks[x,y]=dat;
      }
    }
    if(ter.Center!=null) Add(ter.Center,masks);
    if(ter.Padded!=null) Add(ter.Padded,masks);
    if(ter.CustomFills!=null) foreach(var c in ter.CustomFills) Add(c,masks);
    if(ter.Masked!=null) foreach(var m in ter.Masked) Add(m.Tiles,masks);
    tex = (ttex = null);
  }
  public static void Hook(ILContext ctx){
    var c = new ILCursor(ctx);
    if(c.TryGotoNextBestFit(MoveType.After, 
      i=>i.MatchNewobj<Tileset>(),
      i=>i.MatchStloc(4),
      i=>i.MatchLdloc3(),
      i=>i.MatchNewobj<Autotiler.TerrainType>()
    )){
      c.Index++;
      c.EmitLdloc2();
      c.EmitDelegate(HookBefore);
    } else goto bad;
    Type dt = typeof(Dictionary<char, Autotiler.TerrainType>);
    if(c.TryGotoNextBestFit(MoveType.After,
      i=>i.MatchLdfld<Autotiler>("lookup"),
      i=>i.MatchLdloc3(), i=>i.MatchLdloc(5),
      i=>i.MatchCallvirt(dt,"Add")
    )){
      c.EmitLdloc(5);
      c.EmitDelegate(HookAfter);
    } else goto bad;
    return;
    bad:
      DebugConsole.WriteFailure("Could not set up partial tile hooks");
  }
  public static HookManager hooks = new(()=>{
    IL.Celeste.Autotiler.ctor += Hook;
  },()=>{
    IL.Celeste.Autotiler.ctor -= Hook;
  });
}
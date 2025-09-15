

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

internal class PartialTiles{
  static Dictionary<Autotiler.Tiles, List<ulong>> tiledict = new();
  public static MTexture tex = null;
  public static MTexture ttex = null;
  const ulong FULL = ulong.MaxValue;
  public static bool usingPartialtiles = false;
  static void HookBefore(XmlElement tileset){
    if(tileset.HasAttribute("ausp_partialtiles")){
      tex = GFX.Game["tilesets/" + tileset.Attr("ausp_partialtiles")];
      ttex = GFX.Game["tilesets/" + tileset.Attr("path")];
      if(tex.Width!=ttex.Width || tex.Height != ttex.Height){
        DebugConsole.WriteFailure("Sizes of tileset and partial tile mask don't match",true);
      }
      DebugConsole.Write($"Setting up PartialTiles for {(tileset.HasAttribute("displayName")?tileset.Attr("displayName"):tileset.Attr("path"))}");
      usingPartialtiles = true;
      PixelLeniencyTrigger.hooks.enable();
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
    if(tex.Texture.Texture.Format != Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color){
      throw new Exception("Texture does not have the right format (color)."+
        " The fix for these cases has not been implemented. Ask cloudsbelow to fix;"+ 
        "it's not hard. they don't feel like coding it today and also doubt that it can happen.");
    }
    tex.Texture.Texture.GetData(0,tex.ClipRect,data,0,data.Length);
    ulong[,] masks = new ulong[w/8,h/8];
    for(int x=0; x<w; x+=8){
      for(int y=0; y<h; y+=8){
        ulong dat = 0;
        ulong num = 1;
        for(int j=0; j<8; j++){
          for(int i=0; i<8; i++){
            Color c = data[(y+j)*w+x+i];
            if(c.A>64) dat |= num;
            num<<=1;
          }
        }
        masks[x/8,y/8]=dat;
      }
    }
    if(ter.Center!=null) Add(ter.Center,masks);
    if(ter.Padded!=null) Add(ter.Padded,masks);
    if(ter.CustomFills!=null) foreach(var c in ter.CustomFills) Add(c,masks);
    if(ter.Masked!=null) foreach(var m in ter.Masked) Add(m.Tiles,masks);
    tex = (ttex = null);
  }
  public static void ParseHook(ILContext ctx){
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

  static MipGrid.Layer capturing = null;
  static MTexture HookChoose(int y, int x, Autotiler.Tiles tiles){
    if(usingPartialtiles && capturing is {} mg){
      if(tiledict.TryGetValue(tiles, out var ulongs)){
        var texs = tiles.Textures;
        int choice = Calc.Random.Next(texs.Count);
        capturing.SetBlock(ulongs[choice],x,y);
        return texs[choice];
      } else capturing.SetBlock(FULL,x,y);
    }
    return null;
  }
  static Autotiler.Generated CapturingHook(On.Celeste.Autotiler.orig_Generate orig, Autotiler self, 
    VirtualMap<char> mapData, int startX, int startY, int tilesX, int tilesY, bool forceSolid, char forceID, Autotiler.Behaviour behaviour
  ){
    if(templateFiller.TileView.intercepting) return orig(self, mapData, startX,startY,tilesX,tilesY,forceSolid,forceID,behaviour);
    Autotiler.Generated gen; 
    if(usingPartialtiles && building!=null){
      capturing = new(tilesX,tilesY);
      DebugConsole.Write($"Making capturing of size {tilesX}, {tilesY}");
      gen = orig(self, mapData, startX,startY,tilesX,tilesY,forceSolid,forceID,behaviour);
      building = null;
    } else gen = orig(self, mapData, startX,startY,tilesX,tilesY,forceSolid,forceID,behaviour);
    return gen;
  }
  static SolidTiles building;
  static void HookSolidtiles(On.Celeste.SolidTiles.orig_ctor orig, SolidTiles self, Vector2 position, VirtualMap<char> data){
    if(self is FgTiles || !usingPartialtiles){
      orig(self, position, data);
      return;
    }
    building = self;
    orig(self, position, data);
    self.Collider = new MiptileCollider(new(capturing), Vector2.One);
    //MipGrid m = new(capturing);
    capturing = null; 
    // DebugConsole.Write("Setting captruing to null");
    // DebugConsole.Write("layer height", m.layers.Count);
    // DebugConsole.Write(MipGrid.getBlockstr(m.layers[0].getBlock(18,18)));
    // //1 Int2{1817, 1581} Int2{1825, 1592} 2176 2024
    // DebugConsole.Write("Result:", m.collideInFrame(IntRect.fromCorners(new(1817, 1581),new(1825, 1592))));
  }
  static bool MatchGenarea(ILCursor c, MoveType m, int tilesloc){
    return c.TryGotoNextBestFit(m,
      i=>i.MatchLdsfld(typeof(Calc),"Random"),
      i=>i.MatchLdloc(tilesloc),
      i=>i.MatchLdfld<Autotiler.Tiles>("Textures"),
      i=>i.MatchCall(typeof(Calc),"Choose")//,
      //i=>i.MatchCallvirt(typeof(VirtualMap<Monocle.MTexture>), "set_Item")
    );
  }
  static void GenerateHook(ILContext ctx){
    ILCursor c= new(ctx);
    for(int i=0; i<2; i++){
      int tilesreg = i==1?12:9;
      if(MatchGenarea(c,MoveType.Before,tilesreg)){
        ILCursor d = c.Clone();
        if(MatchGenarea(d,MoveType.After,tilesreg)){
          c.EmitDup();
          c.EmitLdloc(i==1?10:5);
          c.EmitLdarg(2);
          c.EmitSub();
          c.EmitLdloc(tilesreg);
          c.EmitDelegate(HookChoose);
          c.EmitDup();
          c.EmitBrtrue(d.Next);
          c.EmitPop();
        } else goto bad;
      } else goto bad;
    }
    return;
    bad:
      DebugConsole.WriteFailure("Could not make hooks for partial capturing");
  }

  static PersistantAction resetState;
  [Command("ausp_usingPartialtiles","Whether the map is currently using partialtiles")]
  public static void ausp_usingPartialtiles(){
    Engine.Commands.Log($"Partial tiles: {(usingPartialtiles?"on":"off")}");
  }
  public static HookManager hooks = new(()=>{
    IL.Celeste.Autotiler.ctor += ParseHook;
    IL.Celeste.Autotiler.Generate += GenerateHook;
    On.Celeste.Autotiler.Generate += CapturingHook;
    On.Celeste.SolidTiles.ctor += HookSolidtiles;
    auspicioushelperModule.OnEnterMap.enroll(resetState = new(()=>{
      usingPartialtiles = false;
    }));
  },()=>{
    IL.Celeste.Autotiler.ctor -= ParseHook;
    IL.Celeste.Autotiler.Generate -= GenerateHook;
    On.Celeste.Autotiler.Generate -= CapturingHook;
    On.Celeste.SolidTiles.ctor -= HookSolidtiles;
    resetState.remove();
  });
}
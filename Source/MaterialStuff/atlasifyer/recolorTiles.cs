


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

public static class RecolorTiles{
  static Dictionary<char, string> copyRemap = new();
  public static Dictionary<char, MTexture[]> debris = new();
  static MTexture RecolorDelegate(MTexture tex,XmlElement elem){
    char c = elem.AttrChar("id");
    string recolorCode;
    if(elem.HasAttribute("ausp_recolor")){
      recolorCode = elem.Attr("ausp_recolor").AsClean();
      copyRemap.Add(c,recolorCode);
    } else if(elem.HasAttr("ausp_recolor_copy")){
      if(!copyRemap.TryGetValue(elem.AttrChar("ausp_recolor_copy"), out recolorCode)){
        DebugConsole.WriteFailure("Must declare recolor code to copy before copying");
      }
    } else return tex;

    Util.ColorRemap remap = Util.ColorRemap.Get(recolorCode);
    MTexture[] debrisTexs;
    if(elem.HasAttr("debris")) debrisTexs = GFX.Game.GetAtlasSubtextures("debris/" + elem.Attr("debris")).ToArray();
    else debrisTexs = [GFX.Game.Has("debris/"+c)? GFX.Game["debris/" + c] : GFX.Game["debris/1"]];
    debris[c] = debrisTexs.Map(remap.RemapTex);
    return remap.RemapTex(tex);
  }
  static void clearEx(){
    copyRemap.Clear();
    debris.Clear();
  }
  [OnLoad.ILHook(typeof(Autotiler),"",spec:[typeof(string)])]
  static void CtorHook(ILContext ctx){
    ILCursor c = new(ctx);
    c.EmitDelegate(clearEx);
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchLdstr("path"),
      itr=>itr.MatchCall(typeof(Calc),nameof(Calc.Attr)),
      itr=>itr.MatchCall<string>(nameof(string.Concat)),
      itr=>itr.MatchCallvirt<Atlas>("get_Item")
    )){
      c.EmitLdloc2();
      c.EmitDelegate(RecolorDelegate);
    } else DebugConsole.WriteFailure("Could not make recoloring hook",true);
  }
}
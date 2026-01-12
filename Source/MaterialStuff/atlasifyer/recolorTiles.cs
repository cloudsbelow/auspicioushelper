


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
  static MTexture RecolorDelegate(MTexture tex,XmlElement elem){
    //DebugConsole.Write("here", elem, tex);
    char c = elem.AttrChar("id");
    string recolorCode = null;
    if(elem.HasAttribute("ausp_recolor")){
      recolorCode = elem.Attr("ausp_recolor").AsClean();
      copyRemap.Add(c,recolorCode);
    } else if(elem.HasAttr("ausp_recolor_copy")){
      if(!copyRemap.TryGetValue(elem.AttrChar("ausp_recolor_copy"), out recolorCode)){
        DebugConsole.WriteFailure("Must declare recolor code to copy before copying");
      }
    } else return tex;
    DebugConsole.Write("Got a texture to recolor:",elem.AttrChar("id"),elem.Attr("path"));
    Util.ColorRemap remap = Util.ColorRemap.Get(recolorCode);
    var dat = Util.TexData(tex, out var w, out var h).Map(col=>remap.remapRgb(col).toColor());
    string ident = $"tileRecolor {elem.Attr("path")}: "+recolorCode;
    return Atlasifyer.PushToAtlas(dat,w,h,ident).MakeLike(tex);
  }
  static void clearCopyRemap()=>copyRemap.Clear();
  static void CtorHook(ILContext ctx){
    ILCursor c = new(ctx);
    c.EmitDelegate(clearCopyRemap);
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
  [OnLoad]
  public static HookManager hooks = new(()=>{
    IL.Celeste.Autotiler.ctor+=CtorHook;
  }, ()=>{
    IL.Celeste.Autotiler.ctor-=CtorHook;
  });
}
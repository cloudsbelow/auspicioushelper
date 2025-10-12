


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

  static MTexture RecolorDelegate(MTexture tex,XmlElement elem){
    //DebugConsole.Write("here", elem, tex);
    if(!elem.HasAttribute("ausp_recolor")) return tex;
    DebugConsole.Write("Got a texture to recolor with from ",elem.Attr("path"), elem.Attr("ausp_recolor"));
    Util.ColorRemap remap = new(elem.Attr("ausp_recolor"));
    var dat = Util.TexData(tex, out var w, out var h).Map(col=>remap.remapRgb(col).toColor());
    string ident = $"tileRecolor {elem.Attr("path")}: "+elem.Attr("ausp_recolor");
    return Atlasifyer.PushToAtlas(dat,w,h,ident).MakeLike(tex);
  }
  static void CtorHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchLdstr("path"),
      itr=>itr.MatchCall(typeof(Calc),nameof(Calc.Attr)),
      itr=>itr.MatchCall<string>(nameof(string.Concat)),
      itr=>itr.MatchCallvirt<Atlas>("get_Item")
    )){
      c.EmitLdloc2();
      c.EmitDelegate(RecolorDelegate);
    } else DebugConsole.WriteFailure("Could not make recoloring hook");
  }
  public static HookManager hooks = new(()=>{
    IL.Celeste.Autotiler.ctor+=CtorHook;
  }, ()=>{
    IL.Celeste.Autotiler.ctor-=CtorHook;
  });
}
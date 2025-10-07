


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
    var recolorCode = Util.listparseflat(elem.Attr("ausp_recolor"));
    if(recolorCode.Count<=2){
      recolorCode.Add(recolorCode[^1]);
      recolorCode.Add(recolorCode[^1]);
    }
    int w = tex.ClipRect.Width;
    int h = tex.ClipRect.Height;
    Color[] data = new Color[w*h];
    if(tex.Texture.Texture.Format != Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color){
      throw new  System.Exception("Texture does not have the right format (color)."+
        " The fix for these cases has not been implemented. Ask cloudsbelow to fix;"+ 
        "it's not hard. they don't feel like coding it today and also doubt that it can happen.");
    }
    tex.Texture.Texture.GetData(0,tex.ClipRect,data,0,data.Length);
    return tex;
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
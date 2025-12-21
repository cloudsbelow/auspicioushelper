


using System.Collections.Generic;
using Monocle;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;
public static class BorderGenerator{
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnEnter)]
  static Dictionary<string,MTexture> items = new();
  static T ReadAt<T>(this T[] arr, int w, int x, int y){
    if(x<0 || x>=w || y<0 || y>=arr.Length/w) return default;
    return arr[y*w+x];
  }
  public static MTexture GetBorder(MTexture tex){
    if(string.IsNullOrWhiteSpace(tex.AtlasPath)){
      DebugConsole.WriteFailure("Empty atlas path for target border-generating texture");
      return tex;
    }
    if(!items.TryGetValue(tex.AtlasPath, out var te)){
      var dat = Util.TexData(tex, out var w, out var h).Map(c=>c.A);
      int nw=w+2;
      int nh=h+2;
      var o = new Color[nw*nh];
      for(int j=0; j<nh; j++) for(int i=0; i<nw; i++){
        o[i+j*nw] = ((
          dat.ReadAt(w,i,j-1)   | dat.ReadAt(w,i-1,j) | 
          dat.ReadAt(w,i-2,j-1) | dat.ReadAt(w,i-1,j-2)) > 10
        )? new Color(1f,1f,1f,1f):new Color(0f,0f,0f,0f);
      }
      te =  Atlasifyer.PushToAtlas(o,nw,nh,"borderFor#"+tex.AtlasPath).MakeLike(tex);
      te.Width = tex.Width+2;
      te.Height = tex.Height+2;
      te.ScaleFix = tex.ScaleFix;
      te.DrawOffset = tex.DrawOffset;
      te.SetUtil();

      items.Add(tex.AtlasPath,te);
    }
    return te;
  }
}
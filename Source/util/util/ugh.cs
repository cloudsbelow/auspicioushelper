


using System.Collections;
using System.Reflection;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public partial class Util{
  public static Color[] TexData(MTexture tex, out int w, out int h){
    w = tex.ClipRect.Width;
    h = tex.ClipRect.Height;
    Color[] data = new Color[w*h];
    if(tex.Texture.Texture.Format != Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color){
      throw new System.Exception("Texture does not have the right format (color)."+
        " The fix for these cases has not been implemented. Ask cloudsbelow to fix;"+ 
        "it's not hard. they don't feel like coding it today and also doubt that it can happen.");
    }
    tex.Texture.Texture.GetData(0,tex.ClipRect,data,0,data.Length);
    return data;
  }
}
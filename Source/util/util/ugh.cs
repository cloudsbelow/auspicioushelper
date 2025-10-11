


using System;
using System.Collections;
using System.Reflection;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static partial class Util{
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
  static T[] Rotate90<T>(this T[] data, int w){
    T[] res = new T[data.Length];
    int h = data.Length/w;
    if(data.Length!=h*w) throw new System.Exception("Array size not divislbe by width");
    for(int y=0; y<h; y++) for(int x=0; x<w; x++){
      res[(w-x-1)*h+y] = data[y*w+x];
    }
    return res;
  }
  public static T[] Flip<T>(this T[] data, int w, bool flipH, bool flipV){
    T[] res = new T[data.Length];
    int h = data.Length/w;
    if(data.Length!=h*w) throw new System.Exception("Array size not divislbe by width");
    for(int y=0; y<h; y++) for(int x=0; x<w; x++){
      res[(flipV? h-y-1:y)*w + (flipH? w-x-1:x)] = data[y*w+x];
    }
    return res;
  }
  public static T[] Rotate<T>(this T[] data, int amt, ref int w, ref int h){
    if(data.Length!=h*w) throw new System.Exception("Array size not divislbe by width");
    amt = SafeMod(amt,4);
    var n = amt==1? data:data.Flip(w, amt>=2, amt>=2);
    if(amt%2!=0){
      n = n.Rotate90(w);
      int temp = w;
      w=h;
      h=temp;
    }
    return n;
  }
  public static T2[] Map<T1,T2>(this T1[] data, Func<T1,T2> pred){
    T2[] res = new T2[data.Length];
    for(int i=0; i<data.Length; i++)res[i] = pred(data[i]);
    return res;
  }
}



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
  public static List<T2> Map<T1,T2>(this List<T1> data, Func<T1,T2> pred){
    List<T2> res = new(data.Count);
    for(int i=0; i<data.Count; i++) res.Add(pred(data[i]));
    return res;
  } 

  public struct Double4{
    public double X;
    public double Y;
    public double Z;
    public double W;
    public Double4(double X_, double Y_, double Z_, double W_){
      X=X_; Y=Y_; Z=Z_; W=W_;
    }
    public static Double4 operator *(Double4 v, double o)=>new Double4(v.X*o, v.Y*o, v.Z*o, v.W*o);
    public static Double4 operator /(Double4 v, double o)=>new Double4(v.X/o, v.Y/o, v.Z/o, v.W/o);
    public static Double4 operator *(double o, Double4 v)=>new Double4(v.X*o, v.Y*o, v.Z*o, v.W*o);
    public static Double4 operator /(double o, Double4 v)=>new Double4(v.X/o, v.Y/o, v.Z/o, v.W/o);
    public static Double4 operator +(Double4 v, Double4 o)=>new Double4(v.X+o.X, v.Y+o.Y, v.Z+o.Z, v.W+o.W);
    public static Double4 operator -(Double4 v, Double4 o)=>new Double4(v.X-o.X, v.Y-o.Y, v.Z-o.Z, v.W-o.W);
    public static double Dot(Double4 v, Double4 o)=>v.X*o.X + v.Y*o.Y + v.Z*o.Z + v.W*o.W;
    public static implicit operator Double4(Vector4 v)=>new(v.X,v.Y,v.Z,v.W);
    public static implicit operator Double4(Color v)=>new((double)v.R/255.0,(double)v.G/255.0,(double)v.B/255.0,(double)v.A/255.0);
    public double LengthSquared()=>Dot(this,this);
    public double Length()=>Math.Sqrt(this.LengthSquared());
    public Color toColor()=>new Color((float)X,(float)Y,(float)Z,(float)W);
    public static Double4 Zero=>new(0,0,0,0);
  }

  public static float fromsrgb(float c)=> c<=0.04045? c/12.92f : MathF.Pow((c+0.055f)/1.055f, 2.4f);
  public static double fromsrgb(double c)=> c<=0.04045? c/12.92d : Math.Pow((c+0.055d)/1.055d, 2.4d);
  public static Vector4 fromSrgb(this Vector4 v)=>new(fromsrgb(v.X),fromsrgb(v.Y),fromsrgb(v.Z),v.W);
  public static Double4 fromSrgb(this Double4 v)=>new(fromsrgb(v.X),fromsrgb(v.Y),fromsrgb(v.Z),v.W);
  public static float tosrgb(float c)=> c<=0.0031308? 12.92f*c : 1.055f*MathF.Pow(c, 1f/2.4f)-0.055f;
  public static double tosrgb(double c)=> c<=0.0031308? 12.92d*c : 1.055d*Math.Pow(c, 1d/2.4d)-0.055d;
  public static Vector4 toSrgb(this Vector4 v)=>new(tosrgb(v.X),tosrgb(v.Y),tosrgb(v.Z),v.W);
  public static Double4 toSrgb(this Double4 v)=>new(tosrgb(v.X),tosrgb(v.Y),tosrgb(v.Z),v.W);
  public static (float,float,float) RgbToOklab(float r, float g, float b){
    float l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
	  float m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
	  float s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

    float l_ = MathF.Cbrt(l);
    float m_ = MathF.Cbrt(m);
    float s_ = MathF.Cbrt(s);

    return (
        0.2104542553f*l_ + 0.7936177850f*m_ - 0.0040720468f*s_,
        1.9779984951f*l_ - 2.4285922050f*m_ + 0.4505937099f*s_,
        0.0259040371f*l_ + 0.7827717662f*m_ - 0.8086757660f*s_
    );
  }
  public static (float,float,float) OklabToRgb(float pL, float pA, float pB){
    float l_ = pL + 0.3963377774f * pA + 0.2158037573f * pB;
    float m_ = pL - 0.1055613458f * pA - 0.0638541728f * pB;
    float s_ = pL - 0.0894841775f * pA - 1.2914855480f * pB;

    float l = l_*l_*l_;
    float m = m_*m_*m_;
    float s = s_*s_*s_;

    return (
      +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
      -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
      -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s
    );
  }
  public static Vector4 SrgbToOklab(this Vector4 v){
    v = v.fromSrgb();
    var (l,a,b) = RgbToOklab(v.X,v.Y,v.Z);
    return new(l,a,b,v.W);
  }
  public static Vector4 OklabToSrgb(this Vector4 v){
    var (r,g,b) = OklabToRgb(v.X,v.Y,v.Z);
    return new Vector4(r,g,b,v.W).toSrgb();
  }
  public static (double,double,double) RgbToOklab(double r, double g, double b){
    double l = 0.4122214708d * r + 0.5363325363d * g + 0.0514459929d * b;
	  double m = 0.2119034982d * r + 0.6806995451d * g + 0.1073969566d * b;
	  double s = 0.0883024619d * r + 0.2817188376d * g + 0.6299787005d * b;

    double l_ = Math.Cbrt(l);
    double m_ = Math.Cbrt(m);
    double s_ = Math.Cbrt(s);

    return (
        0.2104542553d*l_ + 0.7936177850d*m_ - 0.0040720468d*s_,
        1.9779984951d*l_ - 2.4285922050d*m_ + 0.4505937099d*s_,
        0.0259040371d*l_ + 0.7827717662d*m_ - 0.8086757660d*s_
    );
  }
  public static (double,double,double) OklabToRgb(double pL, double pA, double pB){
    double l_ = pL + 0.3963377774d * pA + 0.2158037573d * pB;
    double m_ = pL - 0.1055613458d * pA - 0.0638541728d * pB;
    double s_ = pL - 0.0894841775d * pA - 1.2914855480d * pB;

    double l = l_*l_*l_;
    double m = m_*m_*m_;
    double s = s_*s_*s_;

    return (
      +4.0767416621d * l - 3.3077115913d * m + 0.2309699292d * s,
      -1.2684380046d * l + 2.6097574011d * m - 0.3413193965d * s,
      -0.0041960863d * l - 0.7034186147d * m + 1.7076147010d * s
    );
  }
  public static Double4 SrgbToOklab(this Double4 v){
    v = v.fromSrgb();
    var (l,a,b) = RgbToOklab(v.X,v.Y,v.Z);
    return new(l,a,b,v.W);
  }
  public static Double4 OklabToSrgb(this Double4 v){
    var (r,g,b) = OklabToRgb(v.X,v.Y,v.Z);
    return new Double4(r,g,b,v.W).toSrgb();
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static partial class Util{
  public class ColorRemap{
    [ResetEvents.ClearOn(ResetEvents.RunTimes.OnEnter)]
    static Dictionary<string, ColorRemap> remaps = new();
    public static ColorRemap Get(string id){
      id = id.AsClean();
      if(!remaps.TryGetValue(id, out var ret)){
        remaps.Add(id,ret=new ColorRemap(id));
      }
      return ret;
    }
    public abstract class ColorWeightable{
      public abstract double WeightAndCol(Double4 i, out Double4 o);
      public virtual string DebugString()=>"NotImplement"; 
    }
    const double epsilon = 0.000000000001f;
    class Line{
      Double4 p1;
      Double4 del;
      double l;
      public Line(Double4 one, Double4 two){
        p1=one;
        del = two-one;
        l = del.LengthSquared()+epsilon;
      }
      public double distSquared(Double4 x, out double proj){
        var dis = x-p1;
        double n = Math.Clamp(proj = (Double4.Dot(dis, del)/l),0f,1f);
        return (n*del-dis).LengthSquared();
      }
      public Double4 get(double t){
        return p1+del*t;
      }
      public string debugString=>$"{p1}->{p1+del} ({l})";
    }
    class LineGroup{
      Line[] lines;
      double[] ts;
      public LineGroup(string ingest){
        double lt=0;
        int lidx=0;
        var li = Util.listparseflat(ingest, true).Where(x=>!string.IsNullOrEmpty(x)).ToList();
        ts = new double[li.Count];
        lines = new Line[li.Count-1];
        Double4 last = default;
        for(int i=0; i<li.Count; i++){
          string[] d = li[i].Split(':');
          double? here = null;
          if(d.Length>1 && double.TryParse(d[1], out double ptime)) here = Math.Clamp(ptime,0,1);
          else if(i==li.Count-1) here = 1;
          if(here is {} f){
            int dist = i-lidx;
            if(i==0) ts[0]=f;
            else{
              double interval = dist==0?1:(f-lt)/dist;
              for(int j=1; j<=dist; j++) ts[lidx+j]=lt+interval*j;
            }
            lidx = i;
            lt = f;
          }
          Double4 parsed = hexToOklabVec(d[0]);
          if(i>0) lines[i-1] = new(last, parsed);
          last = parsed;
        }
      }
      public double Weight(Double4 q, out double point, bool clamp){
        double tw=0;
        double tp=0;
        for(int i=0; i<lines.Length; i++){
          double o = lines[i].distSquared(q, out double p);
          double w = 1/(o*o+epsilon);
          tw+=w;
          tp+= w*((clamp?Math.Clamp(p,0,1):p)*(ts[i+1]-ts[i])+ts[i]);
        }
        point = tp/tw;
        return Math.Pow(tw,0.25f);
      }
      public Double4 Get(double point, bool clamp){
        int low = Math.Clamp(Util.bsearchLast(ts,point),0,ts.Length-2);
        double frac = Util.remap(point, ts[low], ts[low+1]);
        if(clamp) frac = Util.Saturate(frac);
        return lines[low].get(frac);
      }
      public class P:ColorWeightable{
        LineGroup i_;
        LineGroup o_;
        static public Regex  pattern = new Regex(
          @"^(\([^\),]+[^\)]+\))->?(\([^\),]+[^\)]+\))(?::(\{.+\})|())$",
          RegexOptions.Compiled
        );
        public P(Match ingest){
          i_ = new LineGroup(ingest.Groups[1].Value);
          o_ = new LineGroup(ingest.Groups[2].Value);
        }
        public override double WeightAndCol(Double4 i, out Double4 o) {
          double w = i_.Weight(i, out var p, true);
          o = o_.Get(p,true);
          return w;
        }
        public override string DebugString() {
          return $"\nin grad: \n"+i_.debugStr()+"out grad: \n"+o_.debugStr();
        }
      }
      string debugStr(){
        string str = "";
        for(int i=0; i<lines.Length;i++){
          str+=lines[i].debugString+$" {ts[i]}->{ts[i+1]}\n";
        }
        return str;
      }
    }
    public class PointRemap:ColorWeightable{
      Double4 inCol;
      Double4 outCol;
      static public Regex pattern = new Regex(
        @"^(#?[\da-f]+)->?(#?[\da-f]+)(?::(\{.+\})|())$",
        RegexOptions.Compiled
      );
      public PointRemap(Match ingest){
        inCol = hexToOklabVec(ingest.Groups[1].Value);
        outCol = hexToOklabVec(ingest.Groups[2].Value);
      }
      public override double WeightAndCol(Double4 i, out Double4 o) {
        o=outCol;
        return 1/((i-inCol).Length()+epsilon);
      }
      public override string DebugString() {
        return $"point remap {inCol}->{outCol}\n";
      }
    }
    public class PlaneRemap:ColorWeightable{
      //@"^(\([^\),]+[^\)]+\))->?(\([^\),]+[^\)]+\))(?::(\{.+\})|())$",
      //@"^(#?[\da-f]+)->?(#?[\da-f]+)(?::(\{.+\})|())$"
      Double4[] inbasis;
      Double4[] outbasis;
      static public Regex pattern = new Regex(
        @"^(\(#?[\da-f]+,#?[\da-f]+\)\(#?[\da-f]+\))->(\(#?[\da-f]+,#?[\da-f]+\)\(#?[\da-f]+\))?(?::(\{.+\})|())$",
        RegexOptions.Compiled
      );
      public override double WeightAndCol(Double4 i, out Double4 o) {
        
        throw new NotImplementedException("");
      }
    }
    List<ColorWeightable> things=new();
    List<Func<double,double>> weightMap=new();
    public string ident;
    ColorRemap(string inp){
      ident = inp.AsClean();
      foreach(var v in listparseflat(inp)){
        if(string.IsNullOrWhiteSpace(v)) continue;
        Match m;
        if((m=PointRemap.pattern.Match(v)).Success) things.Add(new PointRemap(m));
        else if((m=LineGroup.P.pattern.Match(v)).Success) things.Add(new LineGroup.P(m));
        else DebugConsole.WriteFailure("Bad pattern: "+v);
        if(m.Success){
          Func<double,double> fn = static(double d)=>d;
          var dict = new DictWrapper(kvparseflat(m.Groups[3].Value,true));
          if(dict.TryFloat(["radius","rad","r","size","s"],out var rad)){
            float pow = dict.Float(["pow","p","strength","str"],4);
            weightMap.Add((d)=>{
              return 1/(double.Exp2M1(1/Math.Pow(d*rad,pow))+epsilon);
            });
          } else if(dict.TryFloat(["falloff","f"],out var falloff)){
            weightMap.Add((d)=>d/falloff);
          }
          weightMap.Add(fn);
        }
      }
    }
    public Double4 remapRgb(Double4 srgbIn){
      Double4 loc = srgbIn.SrgbToOklab();
      Double4 f=Double4.Zero;
      double tw=0;
      for(int i=0; i<things.Count; i++){
        var w = weightMap[i](things[i].WeightAndCol(loc, out Double4 v));
        tw+=w;
        f+=v*w;
      }
      return ((tw>0?f/tw:Double4.Zero)*loc.W).OklabToSrgb();
    }
    public void DebugPrint(){
      foreach(var t in things)DebugConsole.Write(t.DebugString());
    }
    Dictionary<string,MTexture> remappedTexs = new();
    public MTexture RemapTex(MTexture tex){
      if(string.IsNullOrWhiteSpace(tex.AtlasPath)){
        DebugConsole.WriteFailure("Empty atlas path for target remapping texture");
        return tex;
      }
      if(!remappedTexs.TryGetValue(tex.AtlasPath, out var te)){
        var dat = Util.TexData(tex, out var w, out var h).Map(col=>remapRgb(col).toColor());
        te =  Atlasifyer.PushToAtlas(dat,w,h,ident+tex.AtlasPath).MakeLike(tex);
        remappedTexs.Add(tex.AtlasPath,te);
        DebugConsole.Write("Cache miss", tex.AtlasPath, remappedTexs.Count);
      } else DebugConsole.Write("Cache hit", tex.AtlasPath);
      return te;
    }
  }
  public static Color toColor(this Vector4 v)=>new Color(v.X,v.Y,v.Z,v.W);
}
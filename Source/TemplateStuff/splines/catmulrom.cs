


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;


public class CatRomSegDenorm{
  int segs;
  float alpha = 0.5f;
  float[] ts;
  struct Segment{
    public Vector2 a,b,c,d;
  }
  Segment[] sp;
  public CatRomSegDenorm(Vector2[] points, Vector2? first=null, Vector2? last=null, float alpha=0.5f,float tension=0){
    this.alpha=alpha;
    List<Vector2> n=new();
    n.Add(first??(2*points[0]-points[1]));
    foreach(var p in points)n.Add(p);
    n.Add(last??(2*points[points.Length-1]-points[points.Length-2]));

    segs = points.Length-1;
    ts = new float[n.Count];
    ts[0]=0;
    sp = new Segment[n.Count-3];
    for(int i=1; i<n.Count; i++)ts[i]=ts[i-1]+MathF.Pow(Vector2.Distance(n[i],n[i-1]),alpha);
    for(int i=0; i<n.Count-3; i++){
      Vector2 p0=n[i]; Vector2 p1=n[i+1]; Vector2 p2=n[i+2]; Vector2 p3=n[i+3];
      float t0=ts[i]; float t1=ts[i+1]; float t2=ts[i+2]; float t3=ts[i+4];
      Vector2 m1 = (1.0f - tension) * (t2 - t1) *
        ((p1 - p0) / (t1 - t0) - (p2 - p0) / (t2 - t0) + (p2 - p1) / (t2 - t1));
      Vector2 m2 = (1.0f - tension) * (t2 - t1) *
        ((p2 - p1) / (t2 - t1) - (p3 - p1) / (t3 - t1) + (p3 - p2) / (t3 - t2));
      sp[i].a = 2.0f * (p1 - p2) + m1 + m2;
      sp[i].b = -3.0f * (p1 - p2) - m1 - m1 - m2;
      sp[i].c = m1;
      sp[i].d = p1;
    }
  }
  // public Vector2 point(float t){
  //   if(t<=ts[1]) return sp[0].d;
  //   if(t>=ts[^2]) return sp[^1].a+sp[^1].b+sp[^1].c+sp[^1].d;
  //   var seg = sp[Util.bsearchLast(ts,t)-1];
    
  // }
}
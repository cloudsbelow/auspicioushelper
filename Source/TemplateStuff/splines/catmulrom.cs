


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


public class CatRomSegDenorm{
  public int segs;
  float[] ts;
  struct Segment{
    public Vector2 a,b,c,d;
  }
  Segment[] sp;
  public CatRomSegDenorm(Vector2[] points, Vector2? first=null, Vector2? last=null, float alpha=0.5f,float tension=0){
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
      float t0=ts[i]; float t1=ts[i+1]; float t2=ts[i+2]; float t3=ts[i+3];
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
  public Vector2 point(float t){
    if(t<0) return sp[0].d;
    if(t>=1){
      Segment s = sp[^-1];
      return s.a + s.b + s.c + s.d;
    }
    t=t*(ts[^2]-ts[1])+ts[1];
    var jdx = Math.Clamp(Util.bsearchLast(ts,t)-1,0,sp.Length-1);
    var seg = sp[jdx];
    float low = ts[jdx+1];
    float high = ts[jdx+2];
    float u = (t-low)/(high-low);
    float u2 = u * u;
    float u3 = u2 * u;
    return seg.a*u3 + seg.b*u2 + seg.c*u + seg.d;
  }
  public Vector2 point(float t, out Vector2 deriv){
    if(t<0){
      deriv = sp[0].c;
      return sp[0].d;
    }
    if(t>=1){
      Segment s = sp[^-1];
      deriv = s.a*3+s.b*2+s.c;
      return s.a + s.b + s.c + s.d;
    }
    t=t*(ts[^2]-ts[1])+ts[1];
    var jdx = Math.Clamp(Util.bsearchLast(ts,t)-1,0,sp.Length-1);
    var seg = sp[jdx];
    float low = ts[jdx+1];
    float high = ts[jdx+2];
    //tjhe signs of someone who does not trust float arithmetic
    float u = Math.Clamp(high==low?0:(t-low)/(high-low),0,1);
    float u2 = u * u;
    float u3 = u2 * u;
    deriv = u2*3*seg.a + u*2*seg.b + seg.c;
    return seg.a*u3 + seg.b*u2 + seg.c*u + seg.d;
  }
}

public class CatRomSegNorm{
  CatRomSegDenorm b;
  float[] invmap;
  int segs=>b.segs;
  const int sampledensity = 64;
  const int invdensity = 32;
  float totalLength;
  public CatRomSegNorm(Vector2[] points, Vector2? first=null, Vector2? last=null, float alpha=0.5f,float tension=0){
    b = new CatRomSegDenorm(points, first, last, alpha, tension);
    int ns = segs*sampledensity;
    float[] samples = new float[ns];
    invmap = new float[invdensity*segs];
    Vector2 point = b.point(0);
    float total = 0;
    for(int j=0; j<ns; j++){
      Vector2 p = b.point((j+1)/ns);
      total = (samples[j] = total+(point-p).Length());
      point = p;
    }
    int i=0;
    for(int j=0; j<invmap.Length; j++){
      float t = j*total/invmap.Length;
      while(t>samples[i] && i<samples.Length-1) i++;
      float low = i==0?0:samples[i-1];
      float high = samples[i];
      float a = Math.Clamp(high==low?0:(t-low)/(high-low),0,1);
      invmap[j] = (low*(1-a)+a*high)/total;
    }
    totalLength = total;
  }
  public Vector2 point(float t){
    int idx = Math.Clamp((int)MathF.Floor(t*invmap.Length),0,invmap.Length-1);
    float a = Math.Clamp(t*invmap.Length-idx,0,1);
    float low = idx==0?0:invmap[idx-1];
    return b.point((1-a)*low+invmap[idx]*a);
  }
  public Vector2 point(float t, out Vector2 derivative){
    int idx = Math.Clamp((int)MathF.Floor(t*invmap.Length),0,invmap.Length-1);
    float a = Math.Clamp(t*invmap.Length-idx,0,1);
    float low = idx==0?0:invmap[idx-1];
    Vector2 res =  b.point((1-a)*low+invmap[idx]*a, out var d);
    derivative = d.SafeNormalize(totalLength);
    return res;
  }
}

public class CatmullDenorm:Spline{
  CatRomSegDenorm[] segs;
  public CatmullDenorm(){}
  public override Vector2 getPos(float t) {
    float loc = Util.SafeMod(t,segments);
    int idx = (int)MathF.Floor(loc);
    return segs[idx].point(loc-idx);
  }
  public override Vector2 getPos(float t, out Vector2 derivative) {
    float loc = Util.SafeMod(t,segments);
    int idx = (int)MathF.Floor(loc);
    return segs[idx].point(loc-idx, out derivative);
  }
  public override void fromNodes(Vector2[] nodes) {
    base.fromNodes(nodes);
    segs = new CatRomSegDenorm[segments];
    for(int i=0; i<segments; i++){
      List<Vector2> n=new();
      int end = knotindices[(i+1)%knotindices.Length];
      int j=knotindices[i];
      while(j!=end){
        n.Add(nodes[j]);
        j=(j+1)%nodes.Length;
      }
      n.Add(nodes[end]);
      segs[i] = new CatRomSegDenorm(n.ToArray());
    }
  }
}


public class CatmullNorm:Spline{
  CatRomSegNorm[] segs;
  public CatmullNorm(){}
  public override Vector2 getPos(float t) {
    float loc = Util.SafeMod(t,segments);
    int idx = (int)MathF.Floor(loc);
    return segs[idx].point(loc-idx);
  }
  public override Vector2 getPos(float t, out Vector2 derivative) {
    float loc = Util.SafeMod(t,segments);
    int idx = (int)MathF.Floor(loc);
    return segs[idx].point(loc-idx, out derivative);
  }
  public override void fromNodes(Vector2[] nodes) {
    base.fromNodes(nodes);
    segs = new CatRomSegNorm[segments];
    for(int i=0; i<segments; i++){
      List<Vector2> n=new();
      int end = knotindices[(i+1)%knotindices.Length];
      int j=knotindices[i];
      while(j!=end){
        n.Add(nodes[j]);
        j=(j+1)%nodes.Length;
      }
      n.Add(nodes[end]);
      segs[i] = new CatRomSegNorm(n.ToArray());
    }
  }
}
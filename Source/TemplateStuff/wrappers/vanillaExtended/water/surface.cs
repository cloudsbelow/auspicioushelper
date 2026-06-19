


using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public partial class FancyWater{
  const float SurfaceInset = 4;
  int maxRaySegs = 4;
  Color[] rayColors;
  Vector2 rayLengthRange = new(8,64);
  Vector2 rayWidthRange = new(4,12);
  ChannelState.FloatCh rayDir;
  float rayDensity;
  bool doInvRays;
  class FakeTopsurface:Water.Surface{
    public FancyWater fw;
    public FakeTopsurface(FancyWater w):base(Vector2.Zero,-Vector2.UnitY,0,0){
      fw=w;
    }
  }
  [ResetEvents.OnHook(typeof(Water.Surface),nameof(Water.Surface.DoRipple))]
  static void Hook(On.Celeste.Water.Surface.orig_DoRipple o, Water.Surface s, Vector2 p, float str){
    if(s is FakeTopsurface fts) fts.fw.DoRipple(p,str);
    else o(s,p,str);
  }

  struct Edgepoint{
    public Vector2 point;
    public Vector2 normal;
    public Edgepoint(Vector2 p, Vector2 n){
      point = p; normal = n;
    }
    public override string ToString()=>$"Edgepoint<{normal}>({point})";
  }
  List<BentSurface> surfaces = new();
  class BentSurface{
    Edgepoint[] points;
    public VertexPositionColor[] mesh;
    float[] heights;
    int surfaceidx;
    public int rayidx;
    bool loop;
    class Ripple{
      public float pos=0, speed=0, height=3, percent=0, duration=2;
    }
    List<Ripple> ripples = new();
    class Ray{
      public float pos, percent, duration, width, length;
      public Color c;
      public Ray ResetFor(BentSurface s, float percent){
        this.percent = percent;
        var fw = s.parent;
        float mw = fw.rayWidthRange.X/4;
        width = s.points.Length > fw.maxRaySegs? 
          Calc.Random.NextFloat()*(fw.rayWidthRange.Y/4-mw) + mw:
          s.points.Length;
        width = Math.Min(width, fw.maxRaySegs-1);

        pos = Calc.Random.NextFloat()*(s.points.Length-(s.loop? 1:width+1));
        duration = Calc.Random.Range(2f, 8f);
        length = Calc.Random.Range(fw.rayLengthRange.X, fw.rayLengthRange.Y);
        c = Calc.Random.Choose(fw.rayColors);
        return this;
      }
    }
    List<Ray> rays = new();
    float timer=0, waviness=0.45f;
    FancyWater parent;
    public BentSurface(List<Edgepoint> arr, List<int> donot, bool loop, FancyWater parent){
      this.parent=parent;
      this.loop=loop;
      points = arr.ToArray();
      heights = new float[points.Length];
      rayidx = (arr.Count-(loop?0:1))*2*3;
      int numrays = (int)Math.Round((points.Length-1)*parent.rayDensity);
      for(int i=0; i<numrays; i++) rays.Add(new Ray().ResetFor(this, Calc.Random.NextFloat()));
      surfaceidx = rayidx + numrays*parent.maxRaySegs*3;

      mesh = new VertexPositionColor[rayidx+surfaceidx];
      for(int i=0; i<rayidx; i++){
        mesh[i].Color = parent.fillColor;
        mesh[i+surfaceidx].Color = parent.surfaceColor;
      }

      int sssss = loop?0:points.Length-1;
      Vector3 p11 = points[sssss].point.Expand();
      for(int i=points.Length - (loop? 1:2); i>=0; i--){
        int o = i*6;
        Vector3 p21 = points[i].point.Expand();
        mesh[o].Position = p11;//new Vector3(float.NaN, float.NaN, float.NaN);
        mesh[o+2].Position = p21;
        mesh[o+3].Position = p21;
        p11 = p21;
      }
      foreach(var i in donot){
        mesh[(i-1)*6].Position = new(float.NaN, float.NaN, float.NaN);
      }
    }
    public void Render(Vector2 loc){
      var m = (Engine.Instance.scene as Level).Camera.matrix;
      m.M41 += loc.X;
      m.M42 += loc.Y;
      GFX.DrawVertices(m, mesh, mesh.Length);
    }
    float heightAt(int n)=>loop||(n>=0&&n<points.Length)? heights[Util.SafeMod(n,points.Length)]:0;
    float heightLerp(float n){
      int b = (int)MathF.Floor(n);
      float l = heightAt(b);
      float h = heightAt(b+1);
      float f=n-b;
      return l*(1-f)+f*h;
    }
    public void Update(float dt){
      if(dt==0) return;
      timer+=dt;
      int n = points.Length-1;
      for(int i=0; i<heights.Length; i++) heights[i]=SurfaceInset-1;
      ripples.RemoveAll(r=>{
        r.percent+=dt/r.duration;
        if(r.percent>1) return true;;
        r.pos+=dt*r.speed;
        if(r.pos<0 || r.pos>n){
          if(loop){
            r.pos=Util.SafeMod(r.pos,n);
          } else {
            r.speed=-r.speed;
            r.pos=r.pos>n? 2*n-r.pos : -r.pos;
          }
        }
        int k=0;
        int start = (int)Math.Floor(r.pos-8);
        for(int j = start>=0?start: (loop? Util.SafeMod(start,n):0); k++<17; j++){
          if(j>n){
            if(loop) j=0;
            else break;
          }
          float d = Math.Abs(r.pos - j);
          float s = d>=4? Util.CRemap(d,4,8,-0.75f,0) : Util.CRemap(d,0,4,1,-0.75f);
          heights[j]+= s*r.height * Util.CubeIn(1f-r.percent);
        }
        return false;
      });

      float taus = n/2f/MathF.PI;
      float f=!loop?waviness:MathF.Max(1f,MathF.Round(taus*waviness))/taus;
      for(int i=0; i<heights.Length; i++){
        heights[i]=Util.Clamp(heights[i]+MathF.Sin(timer + i*f),0,12);
      }

      foreach(Player p in parent.Scene.Tracker.GetEntities<Player>()){
        var pos = p.Center-parent.Position;
        if(p.StateMachine.State!=Player.StSwim || bestMatchPoint(pos,8) is not {} best) continue;
        if(inBasis(best.Item1, pos).X>8) continue;
        var down = GelperIop.downVec(p);

        foreach(var (loc, dist) in allMatchPoint(pos,32)){
          Vector2 basis = inBasis(loc, pos);
          if(basis.X>6) continue;
          float d = Util.CRemap(Math.Abs(basis.Y), 0,16, 1,0);
          float nscore = Calc.ClampedMap(Vector2.Dot(points[loc].normal, down), -0.6f, 0.4f);
          heights[loc]+= Util.CubeOut(d) * Calc.ClampedMap(basis.X, -4,4) * 4f * nscore;
        }
      }
      if(loop)heights[^1]=heights[0];


      int sssss = loop?0:points.Length-1;
      Vector3 p12 = (points[sssss].point+points[sssss].normal*heights[sssss]).Expand();
      Vector3 p13 = (points[sssss].point+points[sssss].normal*(heights[sssss]+1)).Expand();
      for(int i=points.Length - (loop? 1:2); i>=0; i--){
        int o = i*6;
        Vector3 p22 = (points[i].point+points[i].normal*heights[i]).Expand();
        Vector3 p23 = (points[i].point+points[i].normal*(heights[i]+1)).Expand();
        mesh[o+1].Position = p12;
        mesh[o+4].Position = p12;
        mesh[o+5].Position = p22;
        int o2 = o+surfaceidx;
        mesh[o2].Position = p12;
        mesh[o2+1].Position = p13;
        mesh[o2+2].Position = p22;
        mesh[o2+3].Position = p22;
        mesh[o2+4].Position = p13;
        mesh[o2+5].Position = p23;
        p12 = p22;
        p13 = p23;
      }

      var radian = parent.rayDir*MathF.PI/180;
      Vector2 rayAngle = new(-MathF.Cos(radian), MathF.Sin(radian));
      for(int i=0; i<rays.Count; i++){
        var ray = rays[i];
        ray.percent += dt / ray.duration;
        if (ray.percent > 1f){
          ray.ResetFor(this, ray.percent-MathF.Floor(ray.percent));
        }
        var rayCenter = (ray.pos+ray.width/2)%n;

        var norm = normalAt(rayCenter);
        var endCenter = pointAt(rayCenter)+rayAngle*ray.length;
        float str = Math.Min(1, 5-10*Math.Abs(ray.percent-0.5f));
        bool inv = Vector2.Dot(rayAngle, norm)>=0;
        if(inv){
          if(!parent.doInvRays) str=0;
          else endCenter -= rayAngle*ray.length*2;
        }

        var o = rayidx + i*parent.maxRaySegs*3;
        var lastPos = surfaceAt(ray.pos).Expand();
        int low = (int) Math.Floor(ray.pos);
        int j=1;
        for(; j<ray.width+1; j++){
          var oo = o+j*3;
          var next = j<ray.width? surfaceAt((low+j)%n) : surfaceAt(ray.pos+ray.width);
          mesh[oo+0].Position = lastPos;
          mesh[oo+0].Color = ray.c*str;
          mesh[oo+1].Position = lastPos = next.Expand();
          mesh[oo+1].Color = ray.c*str;
          mesh[oo+2].Position = endCenter.Expand();
        }
        for(; j<parent.maxRaySegs; j++){
          var oo = o+j*3;
          mesh[oo+0].Color = new(0,0,0,0);
          mesh[oo+1].Color = new(0,0,0,0);
        }
      }
    }


    Vector2 pointAt(int i)=>points[i].point+points[i].normal*SurfaceInset;
    Vector2 pointAt(float i){
      if(loop) i = (i+points.Length-1)%(points.Length-1);
      int l = loop? (int)Math.Floor(i):Math.Clamp((int)Math.Floor(i), 0, points.Length-2);
      float fac =  i-l;
      return pointAt(l)*(1-fac) + pointAt(l+1)*fac;
    }
    Vector2 surfaceAt(int i)=>points[i].point+points[i].normal*heights[i];
    Vector2 surfaceAt(float i){
      if(loop) i = (i+points.Length-1)%(points.Length-1);
      int l = loop? (int)Math.Floor(i):Math.Clamp((int)Math.Floor(i), 0, points.Length-2);
      float fac =  i-l;
      return surfaceAt(l)*(1-fac) + surfaceAt(l+1)*fac;
    }
    Vector2 normalAt(float i){
      if(loop) i = (i+points.Length-1)%(points.Length-1);
      int l = loop? (int)Math.Floor(i):Math.Clamp((int)Math.Floor(i), 0, points.Length-2);
      float fac =  i-l;
      return (points[l].normal*(1-fac) + points[l+1].normal*fac).SafeNormalize(1);
    }
    (float,float)? bestMatchPoint(Vector2 p, float maxRange){
      var n = points.Length - (loop?1:0);
      float best = -1;
      maxRange+=4;
      float bestl = maxRange*1.5f;
      for(int i=0; i<n; i++){
        var d = (pointAt(i)-p).Length();
        if(d>3.5*maxRange){
          i += (int) Math.Floor((d-2.5*maxRange)/4);
          continue;
        }
        if(d<bestl){
          best=i;
          bestl=d;
        }
      }
      if(best==-1) return null;
      float bf = 0;
      for(float i=-0.5f; i<=0.5f; i+=0.25f){
        var d = (pointAt(best+i)-p).Length();
        if(d<bestl){
          bf=i;
          bestl=d;
        }
      }
      if(bestl > maxRange-4) return null;
      return (best+bf, bestl);
    }
    IEnumerable<(int,float)> allMatchPoint(Vector2 p, float maxRange){
      var n = points.Length - (loop?1:0);
      for(int i=0; i<n; i++){
        var d = (pointAt(i)-p).Length();
        if(d>2*maxRange){
          i += (int) Math.Floor((d-2*maxRange)/4);
          continue;
        }
        if(d<=maxRange) yield return (i,d);
      }
    }
    Vector2 inBasis(float i, Vector2 loc){
      Vector2 inFrame = loc-pointAt(i);
      var v = normalAt(i);
      return new(Vector2.Dot(inFrame,v), Vector2.Dot(inFrame,new(-v.Y,v.X)));
    }
    public void tryRipple(Vector2 p, float multiplier){
      if(bestMatchPoint(p, 12) is not {} pair) return;
      float dur = 3f;
      if(points.Length<51){
        dur *= Calc.ClampedMap(points.Length-1, 0f, 50, 0.25f);
        multiplier *= Calc.ClampedMap(points.Length-1, 0f, 50, 0.5f);
      }
      if(pair.Item2>6) multiplier*=Calc.ClampedMap(pair.Item2, 6,12,1,0);

      ripples.Add(new Ripple{
        pos=pair.Item1, speed=-20, height=2*multiplier, percent=0f, duration=dur
      });
      ripples.Add(new Ripple{
        pos=pair.Item1, speed=20, height=2*multiplier, percent=0f, duration=dur
      });
    }
  }
  public void DoRipple(Vector2 p, float str){
    if(leader is { } l) l.DoRipple(p,str);
    else for(int i=0; i<surfaces.Count; i++) surfaces[i].tryRipple(p-Position,str);
  }



  BentSurface ParseSurface(List<EdgeFinder.Segment> li, bool loop){
    List<Edgepoint> current = new();
    List<int> donot = new();

    for(int i=0; i<li.Count; i++){
      Edges dir = li[i].edge;
      Edges nextdir = i< li.Count-1? li[i+1].edge:(loop? li[0].edge:Edges.none);
      Edges prevdir = i!=0? li[i-1].edge:(loop? li[li.Count-1].edge:Edges.none);
      Vector2 a = li[i].a;
      Vector2 b = li[i].b;
      var l = (b-a).LInf();
      float start = (prevdir==nextCCWOuter(dir))? SurfaceInset:0;
      float end = (nextdir==nextCCWInner(dir))? l-SurfaceInset:l;
      int num = (int) MathF.Ceiling((end-start)/4);
      Vector2 norm = edgeToVec(dir);
      
      for(int s=0; s<=num; s++){
        float lerp = (start*(num-s) + end*s) / (num*l);
        Vector2 point = a*(1-lerp) + b*lerp;
        current.Add(new(point-SurfaceInset*norm, norm));
      }
      if(nextdir==nextCCWInner(dir)){
        Vector2 nextnorm = edgeToVec(nextdir);
        var pivot = b - SurfaceInset*(norm+nextnorm);
        current.Add(new(pivot, sixty(norm,nextnorm)));
        current.Add(new(pivot, sixty(nextnorm,norm)));
      } else donot.Add(current.Count);
    }
    if(loop) current.Add(current[0]);
    return new(current, donot,loop,this);
  }
  
  static float sixtyfac = MathF.Sqrt(3)/2;
  static Vector2 sixty(Vector2 m, Vector2 s)=>m*sixtyfac+s/2;
  static Int2 edgeToVec(Edges e)=> e switch{
    Edges.left=>new(-1,0),
    Edges.right=>new(1,0),
    Edges.top=>new(0,-1),
    Edges.bottom=>new(0,1),
    _=>throw new Exception($"non singular edge {e}")
  };
  static Edges nextCCWInner(Edges e)=> e switch{
    Edges.left=>Edges.bottom,
    Edges.right=>Edges.top,
    Edges.top=>Edges.left,
    Edges.bottom=>Edges.right,
    _=>throw new Exception("non singular edge")
  };
  static Edges nextCCWOuter(Edges e)=> e switch{
    Edges.left=>Edges.top,
    Edges.right=>Edges.bottom,
    Edges.top=>Edges.right,
    Edges.bottom=>Edges.left,
    _=>throw new Exception("non singular edge")
  };
  static class EdgeFinder{
    static (List<float>, Dictionary<float,int>) Ordering(IEnumerable<float> items){
      Dictionary<float,int> found = new();
      foreach(var i in items) found[i]=0;
      List<float> sorted = found.Keys.ToList();
      sorted.Sort();
      for(int i=0; i<sorted.Count; i++) found[sorted[i]]=i;
      return (sorted,found);
    }
    record struct EdgeInnerSegment(){
      public int a,b;
      public bool c1,c2,used;
      public Edges edge;
      public override string ToString()=> $"Edge<{edge}>(a<{c1}>:{a}, b<{c2}>:{b})";
    }
    struct RawEdge(){
      public int r,a,b;
      public Edges edge = Edges.none;
      public override string ToString()=> $"RawEdge<{edge}>({r}: {a},{b})";
    }
    public struct Segment(){
      public Vector2 a,b;
      public Edges edge;
      public override string ToString()=> $"Segment<{edge}>({a} -> {b})";
    }
    static Edges getInRange(List<RawEdge> items, int low, int high, int interval){
      Edges ret = Edges.none;
      for(int i=low; i<high; i++){
        var descr = items[i];
        if(descr.a<=interval && descr.b>interval) ret|=descr.edge;
      }
      return ret;
    }
    static List<EdgeInnerSegment>[] getSegments(int rs, int w, bool reverse, List<RawEdge> opening, List<RawEdge> closing){
      opening.Sort(reverse? static(a,b)=>b.r-a.r : static (a,b)=>a.r-b.r);
      closing.Sort(reverse? static(a,b)=>b.r-a.r : static (a,b)=>a.r-b.r);
      int oi = 0;
      int ci = 0;
      int[] c = new int[w];
      int[] d = new int[w];
      var ret = new List<EdgeInnerSegment>[rs];

      for(int r=reverse? rs-1:0; r<rs && r>=0; r+=reverse? -1:1){
        int loi = oi;
        int lci = ci;
        for(int i=0; i<w; i++) d[i]=c[i];
        while(oi<opening.Count && opening[oi].r==r){
          var descr = opening[oi];
          for(int i=descr.a; i<descr.b; i++) c[i]++;
          oi++;
        }
        while(ci<closing.Count && closing[ci].r==r){
          var descr = closing[ci];
          for(int i=descr.a; i<descr.b; i++) c[i]--;
          ci++;
        }

        List<EdgeInnerSegment> ne = new();
        EdgeInnerSegment oe = new(){a=0};
        EdgeInnerSegment ce = new(){a=0};
        for(int i=0; i<w; i++){
          Edges hereOpen = (c[i]!=0 && d[i]==0)? getInRange(opening, loi, oi, i) : Edges.none;
          Edges hereClose = (c[i]==0 && d[i]!=0)? getInRange(closing, lci, ci, i) : Edges.none;
          if(oe.edge != hereOpen){
            if(oe.edge != Edges.none) ne.Add(oe with {b=i, c2 = d[i]!=0});
            oe = new(){a=i, edge=hereOpen, c1 = i!=0 && d[i-1]!=0};
          }
          if(ce.edge != hereClose){
            if(ce.edge != Edges.none) ne.Add(ce with {a=i, c1 = c[i]!=0});
            ce = new(){b=i, edge=hereClose, c2 = i!=0 && c[i-1]!=0};
          }
        }
        ret[r] = ne.Count>0? ne:null;
      }
      return ret;
    }
    static (int, int, bool)? nextEdge(List<EdgeInnerSegment>[] h, List<EdgeInnerSegment>[] v,  (int, int, bool) edge, bool prev){
      var (r,idx,vert) = edge;
      EdgeInnerSegment e = (vert? v:h)[r][idx];
      Edges ntype = (prev? !e.c1:e.c2)? nextCCWOuter(e.edge) : nextCCWInner(e.edge);
      var arr = (vert? h:v)[prev? e.a:e.b];
      if(arr!=null) for(int i=0; i<arr.Count; i++){
         if(arr[i].edge==ntype && (prev? arr[i].b:arr[i].a)==r) return (prev? e.a:e.b, i, !vert); 
      }
      return null;
    }
    static (List<(int, int, bool)>, bool) Extract(List<EdgeInnerSegment>[] h, List<EdgeInnerSegment>[] v,  (int, int, bool) start){
      var cur = start;
      while(nextEdge(h,v,cur,true) is { } prev && (cur=prev)!=start);
      bool loop = cur==start && nextEdge(h,v,cur,true)!=null;

      var res = new List<(int, int, bool)>();
      while(true){
        var (r,i,vert) = cur;
        var e = (vert? v:h)[r][i];
        if(e.used) break;
        (vert? v:h)[r][i] = e with {used=true};
        res.Add(cur);
        if(nextEdge(h,v,cur,false) is not {} next) break;
        cur = next;
      }
      return (res,loop);
    }

    static List<IntRect> dif(List<IntRect> items, int numLow, int rs, int w){
      var starts = items.ArgSort((a,b)=> a.x-b.x);
      var ends = items.ArgSort((a,b)=> a.right - b.right);

      int[] c = new int[w];
      int si=0;
      int ei=0;
      Dictionary<(int,int), (int,int)> active = new();
      List<IntRect> done = new();
      for(int r=0; r<rs; r++){
        while(si<starts.Length && items[starts[si]].x==r){
          var idx = starts[si++];
          var item = items[idx];
          for(int i=item.top; i<item.bottom; i++) c[i] += idx<numLow? 1:-numLow;
        }
        while(ei<ends.Length && items[ends[ei]].right==r){
          var idx = ends[ei++];
          var item = items[idx];
          for(int i=item.top; i<item.bottom; i++) c[i] -= idx<numLow? 1:-numLow;
        }
        int b=-1;
        // DebugConsole.Write($"Line {r} ({string.Join(" ",c)})");
        for(int i=0; i<w; i++){
          if(b==-1 && c[i]>0) b=i;
          if(b>=0 && c[i]<=0) {
            if(active.TryGetValue((b,i), out var cur)) active[(b,i)]=(cur.Item1, r);
            else active.Add((b,i),(r,r));
            b=-1;
          }
        }
        int ridx = done.Count;
        foreach(var ((y1,y2),(x1,x2)) in active) if(x2!=r) done.Add(new(x1,y1, r-x1,y2-y1));
        for(int i=ridx; i<done.Count; i++) active.Remove((done[i].y, done[i].bottom));
      }
      return done;
    }

    public static (List<(List<Segment>,bool)>, List<FloatRect>) Find(List<FloatRect> rects, List<Edges> edges){
      var (iToX, XToI) = Ordering(rects.Select(r=>r.x).Concat(rects.Select(r=>r.x+r.w)));
      var (iToY, YToI) = Ordering(rects.Select(r=>r.y).Concat(rects.Select(r=>r.y+r.h)));
      var toi = (Vector2 v)=>new Int2(XToI[v.X], YToI[v.Y]);
      var tof = (int x, int y)=>new Vector2(iToX[x], iToY[y]);
      var tofv = (Int2 v)=>new Vector2(iToX[v.x], iToY[v.y]);

      var irs = rects.Map(r=>IntRect.fromCorners(toi(r.tlc), toi(r.brc)));
      var hseg = getSegments(iToY.Count, iToX.Count, true,
        irs.Map((ir, i)=>new RawEdge(){r=ir.y+ir.h, a=ir.x, b=ir.x+ir.w, edge=edges[i]&Edges.bottom}),
        irs.Map((ir, i)=>new RawEdge(){r=ir.y, a=ir.x, b=ir.x+ir.w, edge=edges[i]&Edges.top})
      );
      var vseg = getSegments(iToX.Count, iToY.Count, false,
        irs.Map((ir, i)=>new RawEdge(){r=ir.x, a=ir.y, b=ir.y+ir.h, edge=edges[i]&Edges.left}),
        irs.Map((ir, i)=>new RawEdge(){r=ir.x+ir.w, a=ir.y, b=ir.y+ir.h, edge=edges[i]&Edges.right})
      );

      List<(List<Segment>,bool)> ret = new();
      List<FloatRect> neg = new();
      for(int vert_=0; vert_<2; vert_++){
        var li = vert_!=0? vseg:hseg;
        for(int r=0; r<li.Length; r++) if(li[r]!=null){
          for(int i=0; i<li[r].Count; i++) if(!li[r][i].used){
            var (arr,loop) = Extract(hseg, vseg, (r,i,vert_!=0));
            ret.Add((arr.Map(x=>{
              var (r,i,vert) = x;
              var e = (vert? vseg:hseg)[r][i];
              Segment s = new(){
                edge=e.edge, 
                a=vert? tof(r, e.a):tof(e.a, r), 
                b=vert? tof(r, e.b):tof(e.b, r)
              };
              neg.Add(FloatRect.fromCornersUnordered(s.a, s.b-SurfaceInset*(Vector2) edgeToVec(e.edge)));
              return s;
            }),loop));
          }
        }
      }

      (iToX, XToI) = Ordering(iToX.Concat(neg.Select(r=>r.x)).Concat(neg.Select(r=>r.x+r.w)));
      (iToY, YToI) = Ordering(iToY.Concat(neg.Select(r=>r.y)).Concat(neg.Select(r=>r.y+r.h)));
      var yirs = rects.Select(r=>IntRect.fromCorners(toi(r.tlc), toi(r.brc)));
      var nirs = neg.Select(r=>IntRect.fromCorners(toi(r.tlc), toi(r.brc)));
      var rs = dif(yirs.Concat(nirs).ToList(), irs.Count, iToX.Count, iToY.Count);

      return (ret, rs.Map(r=>FloatRect.fromCorners(tofv(r.tlc),tofv(r.brc))));
    }
  }
}
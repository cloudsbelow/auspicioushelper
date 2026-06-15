


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/water")]
//[TrackedAs(typeof(Water))]
[Tracked]
public class FancyWater:Entity,ISimpleEnt{
  const float SurfaceInset = 5;
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Template.Propagation ITemplateChild.prop=>Template.Propagation.Riding | Template.Propagation.Shake;
  float drag = 1;
  [Flags]
  enum Edges{
    none=0, left=1, right=2, top=4, bottom=8, all=15
  }
  Edges edges = Edges.none;
  FloatRect relativeDraw;
  public FancyWater(EntityData d, Vector2 o):base(d.Position+o/*,false,false,d.Width,d.Height*/){
    if(d.Bool("hasTop",true))edges|=Edges.top;
    if(d.Bool("hasBottom",false))edges|=Edges.bottom;
    if(d.Bool("hasLeft",false))edges|=Edges.left;
    if(d.Bool("hasRight",false))edges|=Edges.right;
    Vector2 tlc = new(edges.HasFlag(Edges.left)?8:0,edges.HasFlag(Edges.top)?8:0);
    Vector2 brc = new Vector2(Width,Height)-new Vector2(edges.HasFlag(Edges.right)?8:0,edges.HasFlag(Edges.bottom)?8:0);
    relativeDraw = FloatRect.fromCorners(tlc,brc);
    //addSurfacesSimple(new(Width,Height));
    //TopSurface = new FakeTopsurface(this);
    Collider = new Hitbox(d.Width,d.Height);
  }
  void ITemplateChild.relposTo(Vector2 loc, Vector2 ls){
    Vector2 delta = (loc+toffset)-Position;
    foreach(Actor a in Scene.Tracker.GetEntities<Actor>()) if(a.CollideCheck(this)){
      a.MoveH(delta.X*drag);
      a.MoveV(delta.Y*drag);
      a.LiftSpeed = ls*drag;
    }
    Position = loc+toffset;
  }
  public override void Render(){
    // Draw.Rect(Position.X+relativeDraw.x,Position.Y+relativeDraw.y, relativeDraw.w,relativeDraw.h,Color.Red);
    // if(surfaces.Count>0){
    //   GameplayRenderer.End();
    //   foreach(var surface in surfaces) surface.Render(Position);
    //   GameplayRenderer.Begin();
    // }
    if(!leader) return;
    var (s,f) = thing;
    foreach(var r in f){
      Draw.Rect(r.x,r.y,r.w,r.h,Color.Blue*0.3f);
      Draw.HollowRect(r.x,r.y,r.w,r.h,Color.Blue);
    }
    var cs = new List<Color>(){Color.White, Color.Yellow, Color.Green, Color.LightGray};
    for(int i=0; i<s.Count; i++){
      var c = cs[i%cs.Count];
      foreach(var e in s[i]){
        Draw.Line(e.a,e.b,c);
      }
    }
  }
  bool leader = true;
  (List<List<EdgeFinder.Segment>>, List<FloatRect>) thing;
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(!leader) return;
    List<FloatRect> bounds = new();
    List<Edges> edges = new();
    foreach(FancyWater fw in scene.Tracker.GetEntities<FancyWater>()){
      if(fw!=this) fw.leader=false;
      bounds.Add(new(fw));
      edges.Add(fw.edges);
    }
    try{
      thing = EdgeFinder.Find(bounds,edges);
    } catch(Exception ex){
      DebugConsole.Write("ex",ex);
    }
  }
  bool ITemplateChild.hasPlayerRider()=>UpdateHook.cachedPlayer?.CollideCheck(this)??false;
  struct Edgepoint{
    public Vector2 point;
    public Vector2 normal;
    public Edgepoint(Vector2 p, Vector2 n){
      point = p; normal = n;
    }
  }
  /*List<BentSurface> surfaces = new();
  class BentSurface{
    Edgepoint[] points;
    VertexPositionColor[] mesh;
    float[] heights;
    int surfaceidx;
    bool loop;
    class Ripple{
      public float pos=0;
      public float speed=0;
      public float height=3;
      public float percent=0;
      public float duration=2;
    }
    List<Ripple> ripples;
    class Tension{
      public float pos=0;
      public float str=4;
    }
    List<Tension> tensions;
    float timer=0;
    float waviness=0.05f;
    public BentSurface(List<Edgepoint> arr, bool loop){
      this.loop=loop;
      points = arr.ToArray();
      heights = new float[points.Length];
      surfaceidx = (arr.Count-(loop?0:1))*2*3;
      mesh = new VertexPositionColor[surfaceidx*2];
      for(int i=0; i<surfaceidx; i++){
        mesh[i].Color = FillColor;
        mesh[i+surfaceidx].Color = SurfaceColor;
      }
    }
    public void Render(Vector2 loc){
      int start = loop?points.Length-1:0;
      Vector3 p11 = (points[start].point+loc).Expand();
      Vector3 p12 = (points[start].point+loc+points[start].normal*heights[start]).Expand();
      Vector3 p13 = (points[start].point+loc+points[start].normal*(heights[start]+1)).Expand();
      for(int i=loop?0:1; i<points.Length; i++){
        int o = i*6;
        Vector3 p21 = (points[i].point+loc).Expand();
        Vector3 p22 = (points[i].point+loc+points[i].normal*heights[i]).Expand();
        Vector3 p23 = (points[i].point+loc+points[i].normal*(heights[i]+1)).Expand();
        mesh[o].Position = p11;
        mesh[o+1].Position = p12;
        mesh[o+2].Position = p21;
        mesh[o+3].Position = p21;
        mesh[o+4].Position = p12;
        mesh[o+5].Position = p22;
        int o2 = o+surfaceidx;
        mesh[o2].Position = p12;
        mesh[o2+1].Position = p13;
        mesh[o2+2].Position = p22;
        mesh[o2+3].Position = p22;
        mesh[o2+4].Position = p13;
        mesh[o2+5].Position = p23;
        p11 = p21;
        p12 = p22;
        p13 = p23;
      }
      GFX.DrawVertices((Engine.Instance.scene as Level).Camera.matrix, mesh, mesh.Length);
    }
    float heightAt(int n)=>loop||(n>=0&&n<points.Length)? heights[Util.SafeMod(n,points.Length)]:0;
    float heightLerp(float n){
      int b = (int)MathF.Floor(n);
      float l = heightAt(b);
      float h = heightAt(b+1);
      float f=n-b;
      return l*(1-f)+f*h;
    }
    public void Update(){
      int n = points.Length-(loop?0:1);
      for(int i=0; i<heights.Length; i++) heights[i]=0;
      for (int i = ripples.Count - 1; i >= 0; i--){
        var r = ripples[i];
        if(r.percent>1) ripples.RemoveAt(i);
        r.percent+=Engine.DeltaTime/r.duration;
        r.pos+=Engine.DeltaTime*r.speed;
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
          float s = j>=4? Util.CRemap(d,4,8,-0.75f,0) : Util.CRemap(d,0,4,1,-0.75f);
          heights[j]+= s*r.height * Util.CubeIn(1f-r.percent);
        }
      }
      float taus = n/2f/MathF.PI;
      float f=!loop?waviness:MathF.Max(1f,MathF.Round(taus*waviness))/taus;
      for(int i=0; i<heights.Length; i++){
        heights[i]=Util.Clamp(heights[i],-4,4);
        Math.Sin(timer + i*f);
      }
      foreach (Tension t in tensions){
        int start = (int)Math.Floor(t.pos-8);
        int k=0;
        for(int j = start>=0?start: (loop? Util.SafeMod(start,n):0); k++<13; j++){
          if(j>=n){
            if(loop) j=0;
            else break;
          }
          float d = Util.CRemap(Math.Abs(t.pos-j), 0,6, 1,0);
          heights[j]+= Util.CubeOut(d) * t.str *12f;
        }
      }
      if(loop)heights[^1]=heights[0];
    }
  }
  void DoRipple(Vector2 p, float str){

  }
  void addSurfacesSimple(Vector2 size){
    List<Edgepoint> current = new();
    Edges dir = Edges.top;
    if(edges.HasFlag(Edges.left))size.X+=4;
    if(edges.HasFlag(Edges.right))size.X+=4;
    Vector2 center = size/2;
    if(edges.HasFlag(Edges.left))center.X-=2;
    if(edges.HasFlag(Edges.right))center.X+=2;
    bool loop=true;
    do{
      bool has = edges.HasFlag(dir);
      if(!has){
        if(current.Count!=0) surfaces.Add(new(current,loop=false));
        current.Clear();
        continue;
      }
      bool lasthas = edges.HasFlag(nextCCWInner(dir));
      var norm = edgeToVec(dir);
      var lvec = edgeToVec(nextCCWInner(dir));
      if(has && lasthas){
        Vector2 pivot = center+(norm+lvec)*(size/2-Vector2.One*8);
        //if(current.Count==0)current.Add(new (pivot,lvec));
        current.Add(new(pivot, sixty(lvec, norm)));
        current.Add(new(pivot, sixty(norm,lvec)));
      }
      float stop = (size*lvec).L1()-(edges.HasFlag(nextCCWOuter(dir))?8:0);
      Vector2 start = center+lvec*size/2+norm*(size/2-Vector2.One*8);
      for(int i=(has&&lasthas)?8:0; i<=stop; i+=4){
        current.Add(new(start-lvec*i,norm));
      }
      
    }while((dir=nextCCWOuter(dir))!=Edges.top);
    if(current.Count>0) surfaces.Add(new(current,loop));
  }
  */
  static float sixtyfac = MathF.Sqrt(3)/2;
  static Vector2 sixty(Vector2 m, Vector2 s)=>m*sixtyfac+s/2;
  static Int2 edgeToVec(Edges e)=> e switch{
    Edges.left=>new(-1,0),
    Edges.right=>new(1,0),
    Edges.top=>new(0,-1),
    Edges.bottom=>new(0,1),
    _=>throw new Exception("non singular edge")
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
  class FakeTopsurface:Water.Surface{
    FancyWater fw;
    public FakeTopsurface(FancyWater w):base(Vector2.Zero,-Vector2.UnitY,0,0){
      fw=w;
    }
    [OnLoad.OnHook(typeof(Water.Surface),nameof(Water.Surface.DoRipple))]
    static void Hook(On.Celeste.Water.Surface.orig_DoRipple o, Water.Surface s, Vector2 p, float str){
      if(s is FakeTopsurface fts){}//fts.fw.DoRipple(p,str)
      else o(s,p,str);
    }
  }
  



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
      // DebugConsole.Write($"get ({string.Join(" ", opening)}) ({string.Join(" ", closing)})");

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
        // DebugConsole.Write($"Line {r} ({string.Join(" ",d)}) -> ({string.Join(" ",c)})");
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
    static List<(int, int, bool)> Extract(List<EdgeInnerSegment>[] h, List<EdgeInnerSegment>[] v,  (int, int, bool) start){
      var cur = start;
      while(nextEdge(h,v,cur,true) is { } prev && prev!=start) cur=prev;

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
      return res;
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

    public static (List<List<Segment>>, List<FloatRect>) Find(List<FloatRect> rects, List<Edges> edges){
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

      List<List<Segment>> ret = new();
      List<FloatRect> neg = new();
      for(int vert_=0; vert_<2; vert_++){
        var li = vert_!=0? vseg:hseg;
        for(int r=0; r<li.Length; r++) if(li[r]!=null){
          for(int i=0; i<li[r].Count; i++) if(!li[r][i].used){
            var arr = Extract(hseg, vseg, (r,i,vert_!=0));
            ret.Add(arr.Map(x=>{
              var (r,i,vert) = x;
              var e = (vert? vseg:hseg)[r][i];
              Segment s = new(){
                edge=e.edge, 
                a=vert? tof(r, e.a):tof(e.a, r), 
                b=vert? tof(r, e.b):tof(e.b, r)
              };
              neg.Add(FloatRect.fromCornersUnordered(s.a, s.b-SurfaceInset*(Vector2) edgeToVec(e.edge)));
              return s;
            }));
          }
        }
      }
      //foreach(var s in ret) DebugConsole.Write($"\n{string.Join(", ",s)}\n");

      (iToX, XToI) = Ordering(iToX.Concat(neg.Select(r=>r.x)).Concat(neg.Select(r=>r.x+r.w)));
      (iToY, YToI) = Ordering(iToY.Concat(neg.Select(r=>r.y)).Concat(neg.Select(r=>r.y+r.h)));
      var yirs = rects.Select(r=>IntRect.fromCorners(toi(r.tlc), toi(r.brc)));
      var nirs = neg.Select(r=>IntRect.fromCorners(toi(r.tlc), toi(r.brc)));
      var rs = dif(yirs.Concat(nirs).ToList(), irs.Count, iToX.Count, iToY.Count);
      //foreach(var r in rs) DebugConsole.Write($"{r}");

      return (ret, rs.Map(r=>FloatRect.fromCorners(tofv(r.tlc),tofv(r.brc))));
    }
    [OnLoad]
    static void Test(){
      try{
        Find([new(0,0,3,3),new(3,3,3,3), new(0,6,6,3), new(0,12,6,3)],[Edges.all,Edges.all,Edges.all,Edges.all]);
      } catch(Exception e){
        DebugConsole.Write("error",e);
      }
    }
  }
}
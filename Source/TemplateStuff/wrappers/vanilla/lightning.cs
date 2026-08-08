



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
public class LightningW:Lightning, ISimpleEnt{
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Template.Propagation ITemplateChild.prop=> Template.Propagation.None;
  public class LightningComp():Component(false,false){ 
    public Dictionary<(Vector2?, float),(Vector2,List<FloatRect>)> all = new();
  }
  List<FloatRect> items;
  List<FloatRect> rects;
  List<(Vector2,Vector2)> edges;
  Vector2 moveDir = Vector2.UnitY*32;
  public LightningW(Vector2 firstpos, List<FloatRect> itemsRef, Vector2? move, float moveTime):base(firstpos, 0,0,null,1){
    items = itemsRef;
    if(move.HasValue){
      moveDir=move.Value;
      Add(new Coroutine(MoveRoutine(moveTime)));
    }
    ResetEvents.Hooks<LightningW>.enable();
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    (edges,rects) = EdgeFinder.Find(items);
    Collider = new CachedColliderlist(items.Map(x=>new Hitbox(x.w-2,x.h-2,x.x+1,x.y+1)).ToArray());
    items.Clear();
    items=null;
  }
  public IEnumerator MoveRoutine(float moveTime){
    float timer = 0;
    Vector2 origOffset = toffset;
    while(true){
      timer += Engine.DeltaTime/moveTime;
      toffset = origOffset + (-MathF.Cos(timer*MathF.PI)/2+0.5f)*moveDir;
      Position = parent.roundLoc+toffset;
      timer = timer%2;
      yield return null;
    }
  }
  
  public static void RenderInsides(LightningRenderer self){
    foreach(LightningW l in self.Scene.Tracker.GetEntities<LightningW>()){
      foreach(var r in l.rects) Draw.SpriteBatch.Draw(
        (RenderTarget2D)GameplayBuffers.Lightning,
        l.Position+r.tlc, r.shift_(l.Position).munane(), Color.White
      );
    }
  }
  [ResetEvents.ILHook(typeof(LightningRenderer), nameof(LightningRenderer.Render))]
  static void RenderHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchLdfld(typeof(LightningRenderer).GetField(nameof(LightningRenderer.edges),Util.GoodBindingFlags)),
      itr=>itr.MatchCallvirt<List<LightningRenderer.Edge>>("get_Count")
    )){
      c.EmitDelegate(RenderInsides);
      c.EmitLdarg0();
    } else DebugConsole.WriteFailure("couldn't add lightning render hook", true);
  }
  [ResetEvents.OnHook(typeof(LightningRenderer),nameof(LightningRenderer.Track))]
  [ResetEvents.OnHook(typeof(LightningRenderer),nameof(LightningRenderer.Untrack))]
  static void Hook(Action<LightningRenderer, Lightning> orig, LightningRenderer self, Lightning block){
    if(block is LightningW){
      self.dirty = true;
      return;
    }
    else orig(self,block);
  }
  [ResetEvents.OnHook(typeof(LightningRenderer), nameof(LightningRenderer.RebuildEdges))]
  static void Hook(On.Celeste.LightningRenderer.orig_RebuildEdges orig, LightningRenderer self){
    orig(self);
    foreach(LightningW l in self.Scene.Tracker.GetEntities<LightningW>()){
      foreach(var e in l.edges) self.edges.Add(new(l,e.Item1,e.Item2));
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
      public int a,b,facing;
      public override string ToString()=> $"Edge<{facing}>({a}, {b})";
    }
    struct RawEdge(){
      public int r,a,b;
      public override string ToString()=> $"RawEdge({r}: {a},{b})";
    }

    static List<EdgeInnerSegment>[] getSegments(int rs, int w, List<RawEdge> opening, List<RawEdge> closing, List<IntRect> done=null){
      opening.Sort(static (a,b)=>a.r-b.r);
      closing.Sort(static (a,b)=>a.r-b.r);
      int oi = 0;
      int ci = 0;
      int[] c = new int[w];
      int[] d = new int[w];
      var ret = new List<EdgeInnerSegment>[rs];
      Dictionary<(int,int), (int,int)> active = done!=null? new():null;

      for(int r=0; r<rs; r+=1){
        int loi = oi;
        int lci = ci;
        for(int i=0; i<w; i++) d[i]=c[i];
        while(oi<opening.Count && opening[oi].r==r){
          var descr = opening[oi++];
          for(int i=descr.a; i<descr.b; i++) c[i]++;
        }
        while(ci<closing.Count && closing[ci].r==r){
          var descr = closing[ci++];
          for(int i=descr.a; i<descr.b; i++) c[i]--;
        }

        List<EdgeInnerSegment> ne = new();
        EdgeInnerSegment cur = new(){a=0};
        int b=-1;
        for(int i=0; i<w; i++){
          int facing = (c[i]!=0? 0:1) - (d[i]!=0? 0:1);
          if(cur.facing != facing){
            if(cur.facing !=0) ne.Add(cur with {b=i});
            cur = new(){a=i, facing=facing};
          }

          if(done==null) continue;
          if(b==-1 && c[i]>0) b=i;
          if(b>=0 && c[i]<=0) {
            if(active.TryGetValue((b,i), out var act)) active[(b,i)]=(act.Item1, r);
            else active.Add((b,i),(r,r));
            b=-1;
          }
        }
        ret[r] = ne.Count>0? ne:null;
        if(done!=null){
          int ridx = done.Count;
          foreach(var ((y1,y2),(x1,x2)) in active) if(x2!=r) done.Add(new(x1,y1, r-x1,y2-y1));
          for(int i=ridx; i<done.Count; i++) active.Remove((done[i].y, done[i].bottom));
        }
      }
      return ret;
    }

    public static (List<(Vector2, Vector2)>, List<FloatRect>) Find(List<FloatRect> rects){
      var (iToX, XToI) = Ordering(rects.Select(r=>r.x).Concat(rects.Select(r=>r.x+r.w)));
      var (iToY, YToI) = Ordering(rects.Select(r=>r.y).Concat(rects.Select(r=>r.y+r.h)));
      var toi = (Vector2 v)=>new Int2(XToI[v.X], YToI[v.Y]);
      var tof = (int x, int y)=>new Vector2(iToX[x], iToY[y]);
      var tofv = (Int2 v)=>new Vector2(iToX[v.x], iToY[v.y]);

      List<IntRect> done = new();
      var irs = rects.Map(r=>IntRect.fromCorners(toi(r.tlc), toi(r.brc)));
      var hseg = getSegments(iToY.Count, iToX.Count,
        irs.Map((ir, i)=>new RawEdge(){r=ir.y+ir.h, a=ir.x, b=ir.x+ir.w}),
        irs.Map((ir, i)=>new RawEdge(){r=ir.y, a=ir.x, b=ir.x+ir.w})
      );
      var vseg = getSegments(iToX.Count, iToY.Count,
        irs.Map((ir, i)=>new RawEdge(){r=ir.x, a=ir.y, b=ir.y+ir.h}),
        irs.Map((ir, i)=>new RawEdge(){r=ir.x+ir.w, a=ir.y, b=ir.y+ir.h}),
      done);

      List<(Vector2, Vector2)> ret = new();
      for(int i=0; i<hseg.Length; i++) if(hseg[i]!=null) foreach(var e in hseg[i]){
        bool reverse = e.facing<0;
        ret.Add(new(tof(reverse? e.a:e.b,i),tof(reverse? e.b:e.a,i)));
      }
      for(int i=0; i<vseg.Length; i++) if(vseg[i]!=null) foreach(var e in vseg[i]){
        bool reverse = e.facing<0;
        ret.Add(new(tof(i, reverse? e.a:e.b),tof(i, reverse? e.b:e.a)));
      }
      return (ret, done.Map(r=>FloatRect.fromCorners(tofv(r.tlc),tofv(r.brc))));
    }
  }
}
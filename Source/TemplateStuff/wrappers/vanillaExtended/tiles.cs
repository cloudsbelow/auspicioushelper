

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Helpers;
using MonoMod.Cil;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.auspicioushelper.Wrappers;

internal static class TileHooks{
  static Rectangle hookAnimT(On.Celeste.AnimatedTiles.orig_GetClippedRenderTiles orig, AnimatedTiles a, int extend){
    Rectangle r = orig(a,extend);
    if(a.Entity is IBoundsHaver e){
      return e.GetTilebounds(a.Entity.Position+a.Position, r);
    }
    return r;
  }
  static Rectangle hookTiles(On.Monocle.TileGrid.orig_GetClippedRenderTiles orig, TileGrid a){
    Rectangle r = orig(a);
    if(a.Entity is IBoundsHaver e){
      return e.GetTilebounds(a.Entity.Position+a.Position, r);
    }
    return r;
  }
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.AnimatedTiles.GetClippedRenderTiles+=hookAnimT;
    On.Monocle.TileGrid.GetClippedRenderTiles+=hookTiles;
  },void ()=>{
    On.Celeste.AnimatedTiles.GetClippedRenderTiles-=hookAnimT;
    On.Monocle.TileGrid.GetClippedRenderTiles-=hookTiles;
  },auspicioushelperModule.OnEnterMap);
}

public interface IBoundsHaver{
  FloatRect bounds {get; set;}
  public Rectangle GetTilebounds(Vector2 loc, Rectangle isect){
    Vector2 tlc = ((bounds.tlc-loc)/8).Floor();
    Vector2 brc = ((bounds.brc-loc)/8).Ceiling();
    FloatRect levelclip = FloatRect.fromCorners(tlc, brc);
    levelclip.expandAll(0);
    return levelclip._intersect(new FloatRect(isect)).munane();
  }
}

internal class BgTiles:BackgroundTiles, ISimpleEnt, IBoundsHaver{
  public Template.Propagation prop => Template.Propagation.Shake;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public FloatRect bounds {get;set;}
  public BgTiles(templateFiller t, Vector2 posoffset, int depthoffset):base(posoffset+t.data.offset, t.tiledata.bgt){
    toffset = t.data.offset;
    Depth+=depthoffset;
    TileHooks.hooks.enable();
    RemoveTag(Tags.Global);
  }
  public void setOffset(Vector2 ppos){}
  public void relposTo(Vector2 loc, Vector2 liftspeed){
    Position = loc+toffset;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    bounds = new FloatRect(SceneAs<Level>().Bounds);
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0)Collidable = col>0;
    if(act!=0)Active = act>0;
  }
}

internal class FgTiles:SolidTiles, ISimpleEnt, IBoundsHaver, IChildShaker{
  public Template.Propagation prop=> Template.Propagation.All;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public FloatRect bounds {get;set;}
  public FgTiles(templateFiller t, Vector2 posoffset, int depthoffset):base(posoffset+t.data.offset, t.tiledata.fgt){
    toffset = t.data.offset;
    Depth+=depthoffset;
    TileHooks.hooks.enable();
    tileTypes = t.tiledata.fgt;
    Add(new TileOccluder(t.tiledata.tileOcc));
    OnDashCollide = (Player p, Vector2 dir)=>((ITemplateChild) this).propagateDashhit(p,dir);
    if(PartialTiles.usingPartialtiles){
      Collider = new MiptileCollider(t.tiledata.FgMipgrid, Vector2.One);
    } else Collider = new DumbGridWrapper(Collider as Grid, t.tiledata.lowresGrid);
    RemoveTag(Tags.Global);
  }
  public void setOffset(Vector2 ppos){
    ChildMarker.Get(this,parent);
  }
  public override void Added(Scene scene){
    base.Added(scene);
    bounds = new FloatRect(SceneAs<Level>().Bounds);
  }
  bool ITemplateChild.hasPlayerRider(){
    if(Scene==null)DebugConsole.Write("I hate hate hate hate hate hate hate you");
    return UpdateHook.cachedPlayer?.IsRiding(this)??false;
  }
  public bool hasInside(Actor a){
    return Collider.Collide(a.Collider);
  }
  public void relposTo(Vector2 loc, Vector2 liftspeed){
    MoveTo(loc+toffset, liftspeed);
  }
  public void destroy(bool particles){
    Vector2 ls = parent.gatheredLiftspeed;
    if(particles) makeDebris();
    foreach(var m in staticMovers) m.Destroy();
    staticMovers.Clear();
    RemoveSelf();
  }
  public void makeDebris(){
    Vector2 ls = parent.gatheredLiftspeed;
    Rectangle bounds = (this as IBoundsHaver).GetTilebounds(Position,AnimatedTiles.GetClippedRenderTiles(32));
    var o = OverrideVisualComponent.TryGet(this);
    for(int i=bounds.X; i<bounds.X+bounds.Width; i++){
      for(int j=bounds.Y; j<bounds.Height; j++){
        char tile = tileTypes[i,j];
        if(tile == '0' || tile == '\0') continue;
        Vector2 offset = new Vector2(i*8+4,j*8+4);
        var d = Engine.Pooler.Create<TileDebris>().Init(Position+offset,tile).RandFrom(ls);
        Scene.Add(d);
        OverrideVisualComponent.Get(d).CopyOther(o);
      }
    }
  }

  public override void MoveHExact(int move){
    GetRiders();
    Player player = Scene.Tracker.GetEntity<Player>();
    if (Collidable && player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && 
    !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY)){
      player.MoveV(1f);
    }

    base.X += move;
    MoveStaticMovers(Vector2.UnitX * move);
    if(!Collidable) return;
    int dir=Math.Sign(move);
    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()){
      if (!entity.AllowPushing) continue;
      bool collidable = entity.Collidable;
      entity.Collidable = true;
      if (!entity.TreatNaive && CollideCheck(entity, Position)){
        int i=0;
        for(; i<Math.Abs(move) && entity.CollideCheck(this, entity.Position+new Vector2(dir*i,0)); i++){}
        entity.LiftSpeed = LiftSpeed;
        Collidable = false;
        entity.MoveHExact(i*dir, entity.SquishCallback, this);
        Collidable = true;
      } else if (riders.Contains(entity)) {
        Collidable = false;
        if (entity.TreatNaive) entity.NaiveMove(Vector2.UnitX * move);
        else entity.MoveHExact(move);
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      }
      entity.Collidable = collidable;
    }  
    riders.Clear();
  }
  public override void MoveVExact(int move){
    GetRiders();
    base.Y += move;
    MoveStaticMovers(Vector2.UnitY * move);
    if(!Collidable) return;
    int dir = Math.Sign(move);
    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()){
      if (!entity.AllowPushing) continue;
      bool collidable = entity.Collidable;
      entity.Collidable = true;
      if (!entity.TreatNaive && CollideCheck(entity, Position)){
          int i=0;
          for(; i<Math.Abs(move) && entity.CollideCheck(this,entity.Position+new Vector2(0,i*dir)); i++){}
          entity.LiftSpeed = LiftSpeed;
          Collidable = false;
          entity.MoveVExact(i*dir,entity.SquishCallback,this);
          Collidable = true;
      } else if (riders.Contains(entity)){
          Collidable = false;
          if (entity.TreatNaive) entity.NaiveMove(Vector2.UnitY * move);
          else entity.MoveVExact(move);
          entity.LiftSpeed = LiftSpeed;
          Collidable = true;
      }
      entity.Collidable = collidable;
    }
    riders.Clear();
  }
  public override void Awake(Scene scene){
    base.Awake(scene);
    foreach (StaticMover smover in scene.Tracker.GetComponents<StaticMover>()){
      if (smover.Platform == null && smover.IsRiding(this)){
        staticMovers.Add(smover);
        smover.Platform = this;
        if (smover.OnAttach != null){
          smover.OnAttach(this);
        }
      }
    }
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0)Collidable = col>0;
    if(act!=0)Active = act>0;
    if(col>0) EnableStaticMovers();
    else if(col<0) DisableStaticMovers();
  }
  public Vector2 lastShake {get;set;} = Vector2.Zero;
}



[Tracked]
public class TileOccluder:OnAnyRemoveComp{
  public class RowValues{
    int[] starts;
    int[] e1;
    int[] e2;
    public RowValues(int rows, int width, Func<int,int,bool> oracle){
      starts = new int[rows+1];
      List<int> e1 = new();
      List<int> e2 = new();
      for(int i=0; i<rows; i++){
        starts[i] = e1.Count;
        for(int j=0; j<width; j++){
          if(oracle(i,j)){
            e1.Add(j);
            do j++; while(j<width && oracle(i,j));
            e2.Add(j);
          } 
        }
      }
      starts[^1] = e1.Count;
      this.e1 = e1.ToArray();
      this.e2 = e2.ToArray();
    }
    public void IntoBounds(int rs, int re, int el, int eh, Action<int,int,int> edgeHandler){
      int endrow = Math.Min(re,starts.Length-1);
      for(int i = Math.Max(rs,0); i<endrow; i++){
        int lidx = starts[i];
        int end = starts[i+1];
        int hidx = end;
        while(lidx<hidx){
          int mid = (lidx+hidx)/2;
          if(e2[mid]<=el) lidx = mid+1;
          else hidx = mid;
        }
        while(lidx<end && e1[lidx]<eh){
          edgeHandler(i, e1[lidx], e2[lidx]);
          lidx++;
        }
      }
    }
  }
  RowValues topEdges;
  RowValues bottomEdges;
  RowValues leftEdges;
  RowValues rightEdges;
  Vector2 size;
  public TileOccluder Build(VirtualMap<char> dat){
    topEdges = new(dat.Rows,dat.Columns,(int i, int j)=> dat[j,i]!='0' && (i==0 || dat[j,i-1]=='0'));
    bottomEdges = new(dat.Rows,dat.Columns,(int i, int j)=> dat[j,i]!='0' && (i==dat.Rows-1 || dat[j,i+1]=='0'));
    leftEdges = new(dat.Columns,dat.Rows,(int i, int j)=> dat[i,j]!='0' && (i==0 || dat[i-1,j]=='0'));
    rightEdges = new(dat.Columns,dat.Rows,(int i, int j)=> dat[i,j]!='0' && (i==dat.Columns-1 || dat[i+1,j]=='0'));
    size = new(dat.Columns*cellW,dat.Rows*cellH);
    return this;
  }
  public TileOccluder Build(MipGrid.Layer l){
    int w=l.width*8;
    int h=l.height*8;
    topEdges = new(h,w,(int i, int j)=>l.collidePoint(new(j,i))&&!l.collidePoint(new(j,i-1)));
    bottomEdges = new(h,w,(int i, int j)=>l.collidePoint(new(j,i))&&!l.collidePoint(new(j,i+1)));
    leftEdges = new(w,h,(int i, int j)=>l.collidePoint(new(i,j))&&!l.collidePoint(new(i-1,j)));
    rightEdges = new(w,h,(int i, int j)=>l.collidePoint(new(i,j))&&!l.collidePoint(new(i+1,j)));
    size = new(w*cellW,h*cellH);
    return this;
  }
  public TileOccluder():base(true,true){
    hooks.enable();
  }
  public TileOccluder(Vector2 cellsize, Vector2 offset):this(){
    cellW=cellsize.X;
    cellH=cellsize.Y;
    this.offset=offset;
  }
  public TileOccluder(TileOccluder copy):this(){
    topEdges = copy.topEdges;
    bottomEdges = copy.bottomEdges;
    leftEdges = copy.leftEdges;
    rightEdges = copy.rightEdges;
    size = copy.size;
    offset = copy.offset;
    cellW = copy.cellW;
    cellH = copy.cellH;
  }
  float cellW = 8;
  float cellH = 8;
  public void Occlude(LightingRenderer r, Vector3 atlasCenter, Color mask, Vector2 center, float rad){
    Vector2 pos = lpos;
    Vector2 del = center-pos;

    bottomEdges.IntoBounds( /// Edges that face _
      (int) ((del.Y-rad)/cellH), (int) Math.Ceiling(del.Y/cellH-1),
      (int) ((del.X-rad)/cellW), (int) Math.Ceiling((del.X+rad)/cellW), (y,x1,x2)=>{
        Span<Vector2> items = stackalloc Vector2[6];
        float ly = y*cellH-del.Y+cellH;
        float lx1 = Math.Max(-rsize, x1*cellW-del.X);
        float lx2 = Math.Min(rsize, x2*cellW-del.X);
        int n = ClipPosy(new(lx1, -ly), new(lx2, -ly), ref items);
        for(int i=0; i<n; i++) items[i].Y=items[i].Y*-1;
        PushVerts(r, n, ref items, atlasCenter, mask, true);
      }
    );
    topEdges.IntoBounds( /// Edges on the top
      (int) Math.Floor(del.Y/cellH+1), (int) Math.Ceiling((del.Y+rad)/cellH),
      (int) ((del.X-rad)/cellW), (int) Math.Ceiling((del.X+rad)/cellW), (y,x1,x2)=>{
        Span<Vector2> items = stackalloc Vector2[6];
        float ly = y*cellH-del.Y;
        float lx1 = Math.Max(-rsize, x1*cellW-del.X);
        float lx2 = Math.Min(rsize, x2*cellW-del.X);
        int n = ClipPosy(new(lx1, ly), new(lx2, ly), ref items);
        PushVerts(r, n, ref items, atlasCenter, mask, false);
      }
    );

    rightEdges.IntoBounds(  ///Edges that have out normal this way ->
      (int)((del.X-rad)/cellW), (int) Math.Ceiling(del.X/cellW-1),
      (int)((del.Y-rad)/cellH), (int) Math.Ceiling((del.Y+rad)/cellH), (x,y1,y2)=>{
        Span<Vector2> items = stackalloc Vector2[6];
        float lx = x*cellW-del.X+cellW;
        float ly1 = Math.Max(-rsize, y1*cellH-del.Y);
        float ly2 = Math.Min(rsize, y2*cellH-del.Y);
        int n = ClipPosy(new(ly1, -lx), new(ly2, -lx), ref items);
        for(int i=0; i<n; i++) items[i]=new Vector2(-items[i].Y, items[i].X);
        PushVerts(r, n, ref items, atlasCenter, mask, false);
      }
    );
    leftEdges.IntoBounds(  ///Edges that have out normal this way <-
      (int) Math.Floor(del.X/cellW+1), (int) Math.Ceiling((del.X+rad)/cellW),
      (int) ((del.Y-rad)/cellH), (int) Math.Ceiling((del.Y+rad)/cellH), (x,y1,y2)=>{
        Span<Vector2> items = stackalloc Vector2[6];
        float lx = x*cellW-del.X;
        float ly1 = Math.Max(-rsize, y1*cellH-del.Y);
        float ly2 = Math.Min(rsize, y2*cellH-del.Y);
        int n = ClipPosy(new(ly1, lx), new(ly2, lx), ref items);
        for(int i=0; i<n; i++) items[i]=new Vector2(items[i].Y, items[i].X);
        PushVerts(r, n, ref items, atlasCenter, mask, true);
      }
    );
  }
  public static void OccludeAll(LightingRenderer r, Vector3 atlasCenter, Color mask, Vector2 center, float rad){
    if(rects.Count!=occs.Count){
      DebugConsole.Write("Whatttt");
      return;
    }
    for(int i=0; i<rects.Count; i++){
      if(rects[i].CollideCircle(center,rad)){
        occs[i].Occlude(r,atlasCenter,mask,center,rad);
      }
    }
  }

  const int rsize = 128;
  //Y must be Strictly Positive, p1 must be on the negative side of p2 when rotating via origin
  static int ClipPosy(Vector2 p1, Vector2 p2, ref Span<Vector2> o){
    float x1 = Math.Abs(p1.X);
    float x2 = Math.Abs(p2.X);
    int type1=Math.Sign(p1.X)*(x1>p1.Y?1:0);
    int type2=Math.Sign(p2.X)*(x2>p2.Y?1:0);
    int n=0;
    o[n++] = p1;
    if(x1!=rsize){
      if(type1 !=0){
        o[n++] = p1*(rsize/x1);
      } else o[n++] = p1*(rsize/p1.Y);
    }
    if(type1==-1 && type2>-1) o[n++] = new Vector2(-rsize,rsize);
    if(type1<=0 && type2==1) o[n++] = new Vector2(rsize, rsize);
    if(x2!=rsize){
      if(type2 !=0){
        o[n++] = p2*(rsize/x2);
      } else o[n++] = p2*(rsize/p2.Y);
    }
    o[n++] = p2;
    return n;
  }
  static void PushVerts(LightingRenderer r, int n, ref Span<Vector2> o, Vector3 center, Color mask, bool flipWinding = false){
    if(r.indexCount+32>r.indices.Length) flushLightMatrices(r);
    int s = r.vertexCount;
    int flip = flipWinding?1:0;
    r.verts[s].Position = center + new Vector3(o[0],0);
    r.verts[s].Color = mask;
    r.verts[s+1].Position = center + new Vector3(o[1],0);
    r.verts[s+1].Color = mask;
    for(int i=2; i<n; i++){
      r.verts[s+i].Position = center + new Vector3(o[i],0);
      r.verts[s+i].Color = mask;
      r.indices[r.indexCount++] = s;
      r.indices[r.indexCount++] = s+i-1+flip;
      r.indices[r.indexCount++] = s+i-flip; 
    }
    r.vertexCount += n;
  }
  static Matrix lightMat = Matrix.CreateScale(0.0009765625f) * Matrix.CreateScale(2f, -2f, 1f) * Matrix.CreateTranslation(-1f, 1f, 0f);
  static void flushLightMatrices(LightingRenderer r){
    Engine.Instance.GraphicsDevice.BlendState = LightingRenderer.OccludeBlendState;
    GFX.FxPrimitive.Parameters["World"].SetValue(lightMat);
    foreach (EffectPass pass in GFX.FxPrimitive.CurrentTechnique.Passes){
      pass.Apply();
      Engine.Instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, r.verts, 0, r.vertexCount, r.indices, 0, r.indexCount / 3);
    }
    r.StartDrawingPrimitives();
  }

  public bool selfVis = true;
  Vector2 offset=Vector2.Zero;
  Vector2 lpos;
  bool lvis=false;
  static List<FloatRect> rects = new();
  static List<TileOccluder> occs = new();
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnReset)]
  static void Clear(){
    rects.Clear();
    occs.Clear();
  }

  public static void HandleThing(On.Celeste.LightingRenderer.orig_BeforeRender orig, LightingRenderer self, Scene l){
    rects.Clear();
    occs.Clear();
    foreach(TileOccluder comp in l.Tracker.GetComponents<TileOccluder>()){
      Vector2 epos = comp.Entity.Position+comp.offset;
      bool nvis = comp.Entity.Visible && comp.selfVis;
      FloatRect rNew = new(epos.X,epos.Y, comp.size.X,comp.size.Y);
      if(nvis){
        rects.Add(rNew);
        occs.Add(comp);
      }
      if(epos == comp.lpos && nvis == comp.lvis) continue;
      FloatRect rOld = new(comp.lpos.X,comp.lpos.Y, comp.size.X,comp.size.Y);
      bool flag = false;
      if(!comp.lvis) rOld = rNew;
      else if((comp.lpos != epos) && nvis) flag = true;
      comp.lpos = epos;
      comp.lvis = nvis;
      foreach(VertexLight v in l.Tracker.GetComponents<VertexLight>()){
        if(v.Dirty) continue;
        if(rOld.CollideCircle(v.Center,v.EndRadius) || (flag && rNew.CollideCircle(v.Center,v.endRadius))){
          v.Dirty = true;
        }
      }
    }
    orig(self,l);
  }
  public override void OnRemove() {
    Scene s = Entity?.Scene;
    if(s==null || !lvis) return;
    FloatRect rOld = new(lpos.X,lpos.Y, size.X,size.Y);
    foreach(VertexLight v in s.Tracker.GetComponents<VertexLight>()){
      if(v.Dirty) continue;
      if(rOld.CollideCircle(v.Center,v.EndRadius)){
        v.Dirty = true;
      }
    }
  }
  static void OccluderHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchCallvirt<LightingRenderer>(nameof(LightingRenderer.GetCenter)),
      itr=>itr.MatchStloc(9), itr=>itr.MatchLdarg0(),itr=>itr.MatchLdloc(5),
      itr=>itr.MatchLdcR4(0), itr=>itr.MatchLdcR4(1),
      itr=>itr.MatchCallvirt<LightingRenderer>(nameof(LightingRenderer.GetMask)),
      itr=>itr.MatchStloc(10)
    )){
      c.EmitLdarg0();
      c.EmitLdloc(9);
      c.EmitLdloc(10);
      c.EmitLdloc(7);
      c.EmitLdloc(6);
      c.EmitLdfld(typeof(VertexLight).GetField(nameof(VertexLight.endRadius),Util.GoodBindingFlags));
      c.EmitDelegate(OccludeAll);
    } else DebugConsole.WriteFailure("Failed to add light hooks", true);
  }
  static HookManager hooks = new(()=>{
    IL.Celeste.LightingRenderer.DrawLightOccluders += OccluderHook;
    On.Celeste.LightingRenderer.BeforeRender += HandleThing;
  }, ()=>{
    IL.Celeste.LightingRenderer.DrawLightOccluders -= OccluderHook;
    On.Celeste.LightingRenderer.BeforeRender -= HandleThing;
  }, auspicioushelperModule.OnEnterMap);
}
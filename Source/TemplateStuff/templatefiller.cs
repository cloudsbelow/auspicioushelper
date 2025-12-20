


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/templateFiller")]
[CustomloadEntity]
public class templateFiller{
  
  internal string name;
  internal MarkedRoomParser.TemplateRoom room;
  internal TemplateData data = new();
  internal TileData tiledata;


  internal class TemplateData{
    public LevelData roomdat;
    internal List<DecalData> decals = new();
    internal List<EntityData> ChildEntities = new(); 
    internal Vector2 offset;
    internal Vector2 position;
    internal Vector2 size;
    internal Vector2 origin=>-offset+position;
    internal FloatRect roomRect=>new(position.X,position.Y,size.X,size.Y);
  }

  internal templateFiller(EntityData d, Vector2 leveloffset){
    data.position = d.Position;
    data.size = new(d.Width, d.Height);
    name = d.Attr("template_name","");
    tiledata = new(leveloffset+data.position, new Rectangle((int)d.Position.X/8, (int)d.Position.Y/8, d.Width/8,d.Height/8));
    data.offset = d.Nodes?.Length>0?d.Position-d.Nodes[0]:Vector2.Zero;
  }
  internal templateFiller(Int2 pos, Int2 size){
    data.position = pos;
    tiledata = new(data.position, new Rectangle(pos.x/8,pos.y/8,size.x/8,size.y/8));
  }
  internal templateFiller(){}
  public class TileView{
    VirtualMap<MTexture> tiles;
    VirtualMap<List<AnimatedTiles.Tile>> anims;
    AnimatedTilesBank bank;
    public bool hasAnimatedtiles=false; 
    int w;
    int h;
    public void Fill(TileGrid t, AnimatedTiles a, int xstart, int ystart, int w, int h){
      this.w=w; this.h=h;
      tiles = new(w,h);
      anims = new(w,h);
      bank = a.Bank;
      for(int i=0; i<w; i++){
        for(int j=0; j<h; j++){
          int x = i+xstart;
          int y = j+ystart;
          tiles[i,j] = t.Tiles[x,y];
          anims[i,j] = a.tiles[x,y];
          if(a.tiles[x,y]!=null) hasAnimatedtiles=true;
        }
      }
    }
    static TileView intercept;
    public static bool intercepting => intercept!=null;
    static Autotiler.Generated Hook(On.Celeste.Autotiler.orig_Generate orig, Autotiler self, 
      VirtualMap<char> mapData, int startX, int startY, int tilesX, int tilesY, bool forceSolid, char forceID, Autotiler.Behaviour behaviour
    ){
      if(intercept == null) return orig(self, mapData, startX,startY,tilesX,tilesY,forceSolid,forceID,behaviour);
      Autotiler.Generated ret;
      ret.TileGrid = new TileGrid(8,8,0,0);
      ret.TileGrid.Tiles = intercept.tiles;
      if(intercept.hasAnimatedtiles){
        ret.SpriteOverlay = new AnimatedTiles(intercept.w,intercept.h, intercept.bank);
        var from = intercept.anims;
        for(int x=0; x<intercept.w; x++){
          for(int y=0; y<intercept.h; y++){
            if(from[x,y]!=null){
              List<AnimatedTiles.Tile> arr = new();
              foreach(AnimatedTiles.Tile t in from[x,y]){
                arr.Add(new AnimatedTiles.Tile{
                  AnimationID = t.AnimationID,
                  Frame = t.Frame,
                  Scale = t.Scale,
                });
              }
              ret.SpriteOverlay.tiles[x,y] = arr;
            }
          }
        }
      } else {
        ret.SpriteOverlay = new AnimatedTiles(intercept.w,intercept.h,intercept.bank){Visible=false,Active=false};
      } 
      intercept = null;
      return ret;
    }
    static HookManager hooks = new HookManager(()=>{
      On.Celeste.Autotiler.Generate+=Hook;
    },()=>{
      On.Celeste.Autotiler.Generate-=Hook;
    });
    public void InterceptNext(){
      hooks.enable();
      intercept = this;
    }
  } 
  internal class TileData {
    public Rectangle tr;
    public VirtualMap<char> fgt;
    public VirtualMap<char> bgt;
    public bool created = false;
    public TileView Fgt = null;
    public MipGrid FgMipgrid;
    public TileView Bgt = null;
    public Vector2 tiletlc;
    public TileOccluder tileOcc  = null;
    public TileData(Vector2 tileoffset, Rectangle tilerect){
      this.tr = tilerect;
      tiletlc = tileoffset;
    }
    public void initDynamic(Level l){
      if(created) return;
      created = true;
      if(fgt!=null){
        SolidTiles st = l.SolidTiles;
        Int2 sto = Int2.Round((tiletlc-st.Position)/8);
        if(PartialTiles.usingPartialtiles) SetMipgrid(st, sto, new(tr.Width,tr.Height));
        Fgt = new(); 
        Fgt.Fill(st.Tiles, st.AnimatedTiles,sto.x,sto.y,tr.Width,tr.Height);
      }
      if(bgt!=null){
        BackgroundTiles st = l.BgTiles;
        Int2 sto = Int2.Round((tiletlc-st.Position)/8);
        Bgt = new();
        Bgt.Fill(st.Tiles, st.AnimatedTiles,sto.x,sto.y,tr.Width,tr.Height);
      }
    }
    public void initStatic(SolidTiles solid, BackgroundTiles back){
      if(created) return;
      created = true;
      if(fgt!=null){
        Int2 sto = Int2.Round((tiletlc-solid.Position)/8);
        if(PartialTiles.usingPartialtiles) SetMipgrid(solid, sto, new(tr.Width,tr.Height));
        Fgt = new(); 
        Fgt.Fill(solid.Tiles, solid.AnimatedTiles,sto.x,sto.y,tr.Width,tr.Height);
      }
      if(bgt!=null){
        Int2 sto = Int2.Round((tiletlc-back.Position)/8);
        Bgt = new(); 
        Bgt.Fill(back.Tiles, back.AnimatedTiles,sto.x,sto.y,tr.Width,tr.Height);
      }
    }
    void SetMipgrid(SolidTiles solid, Int2 tiletlc, Int2 size){
      if(solid.Collider is not MiptileCollider col){
        throw new Exception($"Using partialtiles but solidtile collider {solid.Collider.GetType()}, not MiptileCollider");
      }
      FgMipgrid = new(col.mg.layers[0].GetSublayer(tiletlc.x*8, tiletlc.y*8, size.x*8,size.y*8));
    }
    internal void setTiles(string fg, string bg){
      Regex regex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
      char[,] fgtiles = new char[tr.Width,tr.Height];
      char[,] bgtiles = new char[tr.Width,tr.Height];
      bool keepfg = false;
      bool keepbg = false;
      string[] fglines= regex.Split(fg);
      string[] bglines= regex.Split(bg);
      for(int i=0; i<tr.Height; i++){
        for(int j=0; j<tr.Width; j++){
          int r = i+tr.Top;
          int c = j+tr.Left;
          if(r>=0 && c>=0 && r<fglines.Length && c<fglines[r].Length){
            fgtiles[j,i]=fglines[r][c];
            keepfg |= fglines[r][c]!='0';
          } else {
            fgtiles[j,i]='0';
          }
          if(r>=0 && c>=0 && r<bglines.Length && c<bglines[r].Length){
            bgtiles[j,i]=bglines[r][c];
            keepbg |= bglines[r][c]!='0';
          } else {
            bgtiles[j,i]='0';
          }
        }
      }
      fgt = keepfg? new VirtualMap<char>(fgtiles):null;
      if(fgt != null) tileOcc = new TileOccluder().Build(fgt);
      bgt = keepbg? new VirtualMap<char>(bgtiles):null;
    }
    internal void setTiles(VirtualMap<char> tiles, bool foreground = true){
      char[,] fill = new char[tr.Width,tr.Height];
      bool has = false;
      for(int i=0; i<tr.Width; i++) for(int j=0; j<tr.Height; j++){
        char c = tiles[i+tr.Left,j+tr.Top];
        fill[i,j]=c;
        has |= c!='0';
      }  
      if(foreground) {
        fgt = has?new VirtualMap<char>(fill):null;
        if(fgt != null) tileOcc = new TileOccluder().Build(fgt);
      } else bgt = has?new VirtualMap<char>(fill):null;
    }
  }
  public void AddTilesTo(Template tem, Scene s){
    if(tiledata == null) return;
    tiledata.initDynamic(s as Level);
    if(tiledata.Fgt != null){
      tiledata.Fgt.InterceptNext();
      tem.addEnt(tem.fgt = new FgTiles(this, tem.roundLoc, tem.depthoffset));
    }
    if(tiledata.Bgt != null){
      tiledata.Bgt.InterceptNext();
      tem.addEnt(new BgTiles(this, tem.roundLoc, tem.depthoffset));
    }
  }
  public virtual bool Use(Template user)=>true;
  public virtual templateFiller GetInstance()=>this;
  

  internal TemplateBehaviorChain.Chain chain=null;
  public static templateFiller MkNestingFiller(EntityData internalTemplate,TemplateBehaviorChain.Chain chain = null){
    var f=new templateFiller();
    f.data.ChildEntities.Add(internalTemplate);
    f.data.position = internalTemplate.Position;
    f.chain = chain;
    return f;
  }
  public templateFiller setRoomdat(LevelData ld){
    data.roomdat = ld;
    return this;
  }

  [CustomEntity("auspicioushelper/TemplateFillerSwitcher")]
  [CustomloadEntity]
  public class FillerSwitcher:templateFiller{
    static HashSet<FillerSwitcher> used = new();
    [ResetEvents.RunOn(ResetEvents.RunTimes.OnReset)]
    static void Reset(){
      foreach(var use in used) use.idx=-1;
      used.Clear();
    }
    List<string> list = new();
    int idx=-1;
    enum ChooseMode{
      Loop, PseudoRandom, TrueRandom, Channel
    }
    enum ResetMode{
      Individual, Room, Never
    }
    ChooseMode mode;
    ResetMode reset;
    bool inUsing=false;
    string channel;
    public FillerSwitcher(EntityData d){
      name = d.Attr("template_name","");
      channel = d.Attr("channel");
      mode = d.Enum("SelectionMode", ChooseMode.Loop);
      reset = d.Enum("LoopResetMode", ResetMode.Individual);
      list = Util.listparseflat(d.Attr("templates"));
    }
    public override bool Use(Template user){
      if(inUsing){
        DebugConsole.MakePostcard("Infinite loop in template switchers detected. Bad");
        return false;
      }
      using(new Util.AutoRestore<bool>(ref inUsing, true)){
        switch(mode){
          case ChooseMode.Loop: idx=(idx+1)%list.Count; break;
          case ChooseMode.PseudoRandom: idx = Calc.Random.Next(list.Count); break;
          case ChooseMode.TrueRandom: idx = RandomNumberGenerator.GetInt32(list.Count); break;
          case ChooseMode.Channel: idx = (int)Math.Floor(ChannelState.readChannel(channel)); break;
        }
        if(idx<0 || idx>=list.Count){
          DebugConsole.MakePostcard($"Tried to get item {idx+1} of {room.Name}/{name}, but only {list.Count} were given");
          return false;
        }
        if(!MarkedRoomParser.getTemplate(list[idx], room, user.Scene??user.addingScene, out var t)){
          DebugConsole.MakePostcard($"Could not find the template \"{list[idx]}\" in filler switcher {room.Name}/{name}");
        }
        t.Use(user);
        data = t.data;
        tiledata = t.tiledata;
      }
      return true;
    }
    public override templateFiller GetInstance() {
      if(reset == ResetMode.Individual) return Util.shallowCopy(this);
      if(reset == ResetMode.Room && mode == ChooseMode.Loop) used.Add(this);
      return this;
    }
    public FillerSwitcher(EntityData d, Vector2 o):base(){
      list = Util.listparseflat(d.Attr("templates"),stripout:true);
    }
  } 
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
  public TileOccluder():base(true,true){
    hooks.enable();
  }
  public TileOccluder(TileOccluder copy):this(){
    topEdges = copy.topEdges;
    bottomEdges = copy.bottomEdges;
    leftEdges = copy.leftEdges;
    rightEdges = copy.rightEdges;
    size = copy.size;
  }
  const float cellW = 8;
  const float cellH = 8;
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

  
  Vector2 lpos;
  bool lvis=false;
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReset)]
  static List<FloatRect> rects = new();
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReset)]
  static List<TileOccluder> occs = new();
  public static void HandleThing(Level l){
    rects.Clear();
    occs.Clear();
    foreach(TileOccluder comp in l.Tracker.GetComponents<TileOccluder>()){
      Vector2 epos = comp.Entity.Position;
      bool nvis = comp.Entity.Visible;
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
  }, ()=>{
    IL.Celeste.LightingRenderer.DrawLightOccluders -= OccluderHook;
  }, auspicioushelperModule.OnEnterMap);
}
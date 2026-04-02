


using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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
  //there's history for this class being as dumb as it is. you don't want to know it.
  internal class PaddedMap{
    internal VirtualMap<char> b;
    VirtualMap<char> safe=null;
    Int2 pad=Int2.Zero;
    Int2 size=Int2.Zero;
    public int xPad=>pad.x;
    public int yPad=>pad.y;
    static Int2 ndim=>new(8,8);
    public PaddedMap(VirtualMap<char> orig){
      b=orig;
      size = b==null?Int2.Zero:new(orig.Columns,orig.Rows);
    }
    public OverlayStack MakeSafeFor(Int2 loc, Int2 span){
      var npad = ndim-loc;
      var nsize = loc+span+1;
      if(Int2.Max(pad,npad)!=pad || Int2.Max(size,nsize)!=size || safe==null){
        pad = Int2.Max(pad,npad);
        size = Int2.Max(size, nsize);
        safe = new(pad.x+size.x,pad.y+size.y);
        if(b!=null) for(int xx=0; xx<b.Columns; xx++) for(int yy=0; yy<b.Rows; yy++){
          safe[pad.x+xx,pad.y+yy]=b[xx,yy];
        }
      }
      return new(){
        x=loc.x+pad.x, y=loc.y+pad.y, w=span.x, h=span.y, ttypes=safe
      };
    }
    public class OverlayStack{
      public int x,y,w,h;
      public VirtualMap<char> ttypes;
    }
    static OverlayStack lastOver=null;
    public static void SetupOverlay(Entity e){
      if(e.Get<ChildMarker>()?.parent is not {} p) return;
      var pm = p.t.tiledata.getPaddedmap();
      int w = (int)e.Width/8;
      int h = (int)e.Height/8;
      if(p.fgt==null) lastOver = pm.MakeSafeFor(new(0,0),new(w,h));
      else {
        var offset = Int2.Round((e.Position-p.Position-p.t.data.offset)/8);
        lastOver = pm.MakeSafeFor(offset,new(w,h));
      }
    }
    [OnLoad.OnHook(typeof(Autotiler),nameof(Autotiler.GenerateOverlay))]
    static Autotiler.Generated Hook(
      On.Celeste.Autotiler.orig_GenerateOverlay orig, Autotiler self, 
      char id, int x, int y, int tilesX, int tilesY, VirtualMap<char> mapData
    ){
      if(lastOver == null) return orig(self,id,x,y,tilesX,tilesY,mapData);
      else {
        var old = self.LevelBounds;
        self.LevelBounds = new(){new Rectangle(0,0,lastOver.x+lastOver.w+8,lastOver.y+lastOver.h+8)};
        var res = orig(self,id,lastOver.x,lastOver.y,lastOver.w,lastOver.h,lastOver.ttypes);
        self.LevelBounds = old;
        lastOver = null;
        return res;
      }
    }
  }
  static PaddedMap defaultMap=>new PaddedMap(null);
  internal class TileData {
    public Rectangle tr;
    public VirtualMap<char> fgt;
    public VirtualMap<char> bgt;
    public bool created = false;
    public bool createStatically = false;
    public TileView Fgt = null;
    public MipGrid FgMipgrid;
    public MipGrid lowresGrid;
    public TileView Bgt = null;
    public Vector2 tiletlc;
    public TileOccluder tileOcc  = null;
    PaddedMap _pmap;
    internal PaddedMap getPaddedmap(){
      if(fgt==null) return defaultMap;
      if(_pmap==null || _pmap.b!=fgt) _pmap = new(fgt);
      return _pmap; 
    }
    public TileData(Vector2 tileoffset, Rectangle tilerect){
      this.tr = tilerect;
      tiletlc = tileoffset;
    }
    public void initDynamic(Level l){
      if(created) return;
      if(createStatically) using(new ConnectedBlocks.PaddingLock()){
        int pd=MarkedRoomParser.tilepadding;
        Vector2 tileLoc = new Vector2(tr.X-pd,tr.Y-pd)*8;
        SolidTiles st = fgt==null?null: new(tileLoc,Util.addPadding(fgt,new(pd,pd)));
        BackgroundTiles bt = bgt==null?null: new(tileLoc,Util.addPadding(bgt,new(pd,pd)));
        initStatic(st,bt);
        return;
      } else created=true;

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
      if(fgt != null){
        tileOcc = new TileOccluder().Build(fgt);
        lowresGrid = new MipGrid(fgt.Map(x=>x!='0'));
      }
      bgt = keepbg? new VirtualMap<char>(bgtiles):null;
    }
    internal void setTiles(VirtualMap<char> tiles, bool foreground = true, Int2? customOffset=null){
      char[,] fill = new char[tr.Width,tr.Height];
      bool has = false;
      Int2 offset = customOffset??new Int2(tr.Left,tr.Top);
      for(int i=0; i<tr.Width; i++) for(int j=0; j<tr.Height; j++){
        char c = tiles[i+offset.x,j+offset.y];
        if(c=='\n') c='0';
        fill[i,j]=c;
        has |= c!='0';
      }  
      if(foreground) {
        fgt = has?new VirtualMap<char>(fill):null;
        if(fgt != null){
          tileOcc = new TileOccluder().Build(fgt);
          lowresGrid = new MipGrid(fgt.Map(x=>x!='0'));
        }
      } else bgt = has?new VirtualMap<char>(fill):null;
    }
  }
  public void AddTilesTo(Template tem, Scene s){
    if(tiledata == null) return;
    if(!tiledata.created){
      tiledata.initDynamic(s as Level);  
    }
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
  static Dictionary<string, Action<templateFiller>> preprocess = new();
  static HashSet<string> processing = new();
  public void Preprocess(){
    foreach(EntityData d in data.ChildEntities) processing.Add(d.Name);
    foreach(var e in processing) if(preprocess.TryGetValue(e, out var a)) a(this);
    processing.Clear();
  }
}
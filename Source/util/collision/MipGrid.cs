


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using IL.Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;
public class MipGrid{
  //ok but we hardcode these everywhere so like don't
  //well if these are ever not a power of two it will be even worse. just leave them at 8.
  const int blockw = 8;
  const int blockh = 8;
  public class Layer{
    ulong[] d;
    public int width;
    public int height;
    const int ChunkSize = 32;
    public int gridx;
    public int gridy;
    ulong[][] chunks;
    public Layer(List<ulong> data, int width){
      d=data.ToArray();
      this.width=width;
      height = data.Count/width;
      if(data.Count!=width*height) throw new Exception("mystery!");
    }
    public Layer(int width, int height){
      gridx=Util.UDiv(width,ChunkSize);
      gridy=Util.UDiv(height,ChunkSize);
      chunks = new ulong[gridx*gridy][];
    }


    public void FillChunk(List<ulong> data, int x, int y){
      if(data.Count!=ChunkSize*ChunkSize || (uint)x>(uint)gridx || (uint)y>(uint)gridy) throw new Exception("bad");
      chunks[x+y*gridx] = data.ToArray();
    }
    public ulong getBlockChunked(int x, int y){
      int gx=(x+ChunkSize)/ChunkSize-1;
      int gy=(y+ChunkSize)/ChunkSize-1;
      if((uint)x<(uint)gridx && (uint)y<(uint)gridy && chunks[gx+gy*gridx] is {} c){
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(c), x+ChunkSize*(y-gx-gy*ChunkSize));
      }
      return 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void getBlocktile(int x, int y, out ulong tl, out ulong tr, out ulong bl, out ulong br){
      if(x<=-blockw||y<=-blockh){
        tl=tr=bl=br=0;
        return;
      }
      int bx = (x+blockw)/blockw-1;
      int by = (y+blockh)/blockh-1;
      if(chunks!=null){
        int gx = (bx+ChunkSize)/ChunkSize-1;
        int gy = (by+ChunkSize)/ChunkSize-1;
        int fx = bx-gx*ChunkSize;
        int fy = by-gy*ChunkSize;
        if(fx<ChunkSize-1 && fy<ChunkSize-1){
          //everything lies in same chunk
          if((uint)gx<(uint)gridx && (uint)gy<(uint)gridy && chunks[gx+gy*gridx] is {} chunk){
            int idx = fy*ChunkSize+fx;
            ref ulong mloc = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chunk), idx);
            tl=mloc; tr=Unsafe.Add(ref mloc,1); 
            bl=Unsafe.Add(ref mloc, ChunkSize); br=Unsafe.Add(ref mloc,1+ChunkSize);
            return;
          } else {
            tl=tr=bl=br=0;
            return;
          }
        } else {
          bool negX = bx<0; bool negY = by<0;
          tl=negX||negY?0:getBlockChunked(bx,by); tr=negY?0:getBlockChunked(bx+1,by);
          bl=negX?0:getBlockChunked(bx,by+1); br=getBlockChunked(bx+1,by+1);
          return;
        }
      } else {
        if((uint)bx<(uint)width-1 && (uint)by<(uint)height-1){
          int idx = bx+by*width;
          ref ulong mloc = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(d), idx);
          tl = mloc;
          tr = Unsafe.Add(ref mloc,1);
          bl = Unsafe.Add(ref mloc,width);
          br = Unsafe.Add(ref mloc,1+width);
        } else {
          tl = getBlock(bx,by);
          tr = getBlock(bx+1,by);
          bl = getBlock(bx,by+1);
          br = getBlock(bx+1,by+1);
        }
      }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong getBlock(int x, int y){
      if(x<0 || y<0 || x>=width || y>=height) return 0;
      return d[x+y*width];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong getBlockFast(int x, int y){
      return d[x+y*width];
    }
    public ulong getArea(int x, int y){
      // int xf = (x%blockw+blockw)%blockw;
      // int yf = (y%blockh+blockh)%blockh;
      // int xb = (x-xf)/blockw;
      // int yb = (y-yf)/blockh;
      if(x<=-blockw||y<=-blockh) return 0;
      int xb = (x+blockw)/blockw-1;
      int yb = (y+blockh)/blockh-1;
      int xf = x-xb*blockw;
      int yf = y-yb*blockh;
      //DebugConsole.Write($"{x},{y}, {xb},{yb}  {xf},{yf}");
      ulong tl, tr, bl, br;
      if(xb>=0 && yb>=0 && xb<width-1 && yb<height-1){
        tl = getBlockFast(xb,yb);
        tr = getBlockFast(xb+1,yb);
        bl = getBlockFast(xb,yb+1);
        br = getBlockFast(xb+1,yb+1);
      } else {
        tl = getBlock(xb,yb);
        tr = getBlock(xb+1,yb);
        bl = getBlock(xb,yb+1);
        br = getBlock(xb+1,yb+1);
      }
      //ulong topmask = BYTEMARKER<<(yf*8);
      //ulong botmask = BYTEMARKER^topmask;
      ulong leftmask = BYTEMARKER*(byte)((0xff<<xf)&0xff);
      ulong rightmask = FULL^leftmask;
      ulong res=0;
      int yfi = blockh-yf;
      int xfi = blockw-xf;
      res|=(tl&leftmask)>>(yf*8+xf);
      res|=(tr&rightmask)>>(yf*8)<<xfi;
      if(yf!=0){
        res|=(bl&leftmask)<<(yfi*8)>>xf;
        res|=(br&rightmask)<<(yfi*8+xfi);
      }
      //DebugConsole.Write(getBlockstr(res));
      return res;
    }
    public ulong getAreaSmeared(int x, int y, bool smearH, bool smearV){
      ulong a = smearH?getArea(x,y)|getArea(x+1,y):getArea(x,y);
      if(!smearV) return a;
      ulong b = smearH?getArea(x,y+1)|getArea(x+1,y+1):getArea(x,y+1);
      return a|b;
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public ulong getAreaSmearedFast(int x, int y, bool smearH, bool smearV){
      if(x<=-blockw||y<=-blockh) return 0;
      int xb = (x+blockw)/blockw-1;
      int yb = (y+blockh)/blockh-1;
      int xf = x-xb*blockw;
      int yf = y-yb*blockh;
      //DebugConsole.Write($"{x},{y}, {xb},{yb}  {xf},{yf}");
      ulong tl, tr, bl, br;
      if(xb>=0 && yb>=0 && xb<width-1 && yb<height-1){
        tl = getBlockFast(xb,yb);
        tr = getBlockFast(xb+1,yb);
        bl = getBlockFast(xb,yb+1);
        br = getBlockFast(xb+1,yb+1);
      } else {
        tl = getBlock(xb,yb);
        tr = getBlock(xb+1,yb);
        bl = getBlock(xb,yb+1);
        br = getBlock(xb+1,yb+1);
      }
      byte lmb = (byte)((0xff<<xf)&0xff);
      ulong leftmask = BYTEMARKER*lmb;
      byte em=0;
      ulong rightmask = FULL^leftmask;
      ulong res=0;
      int yfi = blockh-yf;
      int xfi = blockw-xf;
      if(smearV){
        //this is fine because yf,xf are less than 8
        em=(byte)((((bl&leftmask)>>(yf*8+xf))|((br&rightmask)>>(yf*8)<<xfi)) & 0xff);
      }
      res|=(tl&leftmask)>>(yf*8+xf);
      res|=(tr&rightmask)>>(yf*8)<<xfi;
      //the branch is needed because shifting loops
      if(yf!=0){
        res|=(bl&leftmask)<<(yfi*8)>>xf;
        //the branch isn't needed because rightmask will be 0
        res|=(br&rightmask)<<(yfi*8+xfi);
      }
      if(smearH) {
        ulong vmask = BYTEMARKER*(byte)(1<<xf);
        ulong barres = (tr&vmask)<<(blockh-xf-1)>>yf*8;
        if(yf!=0)barres |= (br&vmask)<<(blockh-xf-1+yfi*8);
        if(smearV){
          em = (byte)(em|(em>>1)+(byte)((br>>xf+8*yf)&1)*0x80);
        }
        res = res|((res&~BYTEMARKER)>>1)|barres;
      }
      if(smearV){
        res = res | (res<<8) | ((ulong)em)>>56;
      }
      return res;
    }
    public ulong getAreaAligned(int x, int y){
      return getBlock(x/blockw,y/blockh);
    }
  }
  
  internal List<Layer> layers;
  internal int width;
  internal int height;
  internal int highestlevel;
  public Vector2 cellshape;
  internal Vector2 tlc => g.AbsolutePosition.Round()+cellshape*cellRectCorner;
  FloatRect bounds=>new FloatRect(tlc.X,tlc.Y,cellshape.X*width,cellshape.Y*height);
  Grid g;
  Vector2 cellRectCorner = Vector2.Zero;
  const ulong FULL = 0xffff_ffff_ffff_ffffUL;
  const ulong BYTEMARKER = 0x0101_0101_0101_0101UL;
  public MipGrid(Grid grid){
    g=grid;
    cellshape = new Vector2(g.CellWidth,g.CellHeight);
    VirtualMap<bool> map = grid.Data;
    List<ulong> r0 = new();
    for(int yb=0; yb<map.Rows; yb+=blockh){
      for(int xb=0; xb<map.Columns; xb+=blockw){
        ulong block = 0;
        int xstop = Math.Min(blockw,map.Columns-xb);
        int ystop = Math.Min(blockh,map.Rows-yb);
        for(int y=0; y<ystop; y++){
          for(int x=0; x<xstop; x++){
            if(map[x+xb,y+yb])block |= 1UL<<(x+y*8);
          }
        }
        r0.Add(block);
      }
    }
    layers = [new Layer(r0,(map.Columns+blockw-1)/blockw)];
    width = map.Columns;
    height = map.Rows;
    buildMips();
  }
  public MipGrid(){
    
  }
  public MipGrid(Grid g, int x1, int y1, int x2, int y2){
    if(x1>x2 || y1>y2) throw new Exception("lol um?");
    this.g=g;
    cellshape = new Vector2(g.CellWidth,g.CellHeight);
    VirtualMap<bool> map = g.Data;
    List<ulong> r0 = new();
    for(int yb = y1; yb<y2; yb+=blockh){
      for(int xb = x1; xb<x2; xb+=blockw){
        ulong block = 0;
        int xstop = Math.Min(blockw,x2-xb);
        int ystop = Math.Min(blockh,y2-yb);
        for(int y=0; y<ystop; y++){
          if(yb+y<0 || yb+y>=map.Rows) continue;
          for(int x=0; x<xstop; x++){
            if(xb+x<0 || xb+x>=map.Columns) continue;
            if(map[x+xb,y+yb])block |= 1UL<<(x+y*8);
          }
        }
        r0.Add(block);
      }
    }
    cellRectCorner = new Vector2(x1,y1);
    width = x2-x1;
    height = y2-y1;
    layers = [new Layer(r0,(x2-x1+blockw-1)/blockw)];
    buildMips();
  }
  void buildMips(){
    Layer b = layers[0];
    if(layers.Count != 1) throw new Exception("only build mips on new grid");
    while(Math.Max(b.width,b.height)>=4){
      List<ulong> r = new();
      for(int yb=0; yb<b.height; yb+=blockh){
        for(int xb=0; xb<b.width; xb+=blockw){
          ulong block = 0;
          int xstop = Math.Min(blockw,b.width-xb);
          int ystop = Math.Min(blockh,b.height-yb);
          for(int y=0; y<ystop; y++){
            for(int x=0; x<xstop; x++){
              if(b.getBlockFast(x+xb, y+yb)!=0)block |= 1UL<<(x+y*8);
            }
          }
          r.Add(block);
        }
      }
      layers.Add(b = new Layer(r, (b.width+blockw-1)/blockw));
    }
    highestlevel = layers.Count-1;
  }
  //takes in corners of rect in the frame of the grid's bottom layer
  internal static ulong makeRectMask(int blockx, int blocky, Vector2 otlc, Vector2 obrc, int level){
    int levelDiv = 1<<(level*3);
    Vector2 coordoffset = new Vector2(blockx*blockw,blocky*blockh);
    Vector2 rtlc = (otlc/levelDiv-coordoffset).Floor();
    Vector2 rbrc = (obrc/levelDiv-coordoffset).Ceiling();
    if(rbrc.X<=0 || rbrc.Y<=0 || rtlc.X>=blockw || rtlc.Y>=blockh) return 0;
    int x1 = Math.Clamp((int) rtlc.X,0,blockw-1);
    int y1 = Math.Clamp((int) rtlc.Y,0,blockh-1);
    int x2 = Math.Clamp((int) rbrc.X,1,blockw);
    int y2 = Math.Clamp((int) rbrc.Y,1,blockh);
    if(x2<=x1 || y2<=y1) return 0;
    byte row = (byte)(((1<<(x2-x1))-1)<<x1);
    int rows = y2-y1;
    var mask = rows==8? FULL: ((1UL << (rows*8))-1) << (y1*8);
    return mask & (0x0101_0101_0101_0101UL*row);
  }
  //x and y are in block space for the level
  bool collideFrLevel(int x, int y, Vector2 otlc, Vector2 obrc, int level){
    ulong dat = layers[level].getBlock(x,y);
    ulong mask = makeRectMask(x,y,otlc,obrc,level);
    if(dat == 0 || mask == 0) return false;
    //if(mask == FULL) return true;
    //if(dat == FULL && level == 0) return true;
    ulong hit = dat&mask;
    if(level == 0) return hit!=0;
    while(hit != 0){
      int index = System.Numerics.BitOperations.TrailingZeroCount(hit);
      if(collideFrLevel(x*blockw+index%8,y*blockh+index/8,otlc,obrc,level-1)) return true;
      hit &= hit-1;
    }
    return false;
  }
  public bool collideFr(FloatRect f){
    int mld = (int)Math.Ceiling(Math.Max(f.w/cellshape.X,f.h/cellshape.Y));
    int level = 0;
    while(level<highestlevel && (1<<(3*(level+1)))<mld)level++;
    int levelDiv = 1<<(3*level);
    Vector2 rtlc = Vector2.Max(((f.tlc.Round()-tlc)/cellshape).Floor(), new Vector2(0,0));
    Vector2 rbrc = Vector2.Min(((f.brc.Round()-tlc)/cellshape).Ceiling(), new Vector2(width,height));
    if(rbrc.X<0 || rbrc.Y<0 || rtlc.X>=width || rtlc.Y>=height) return false;

    int xstop = Math.Min((int)Math.Ceiling(rbrc.X/levelDiv/blockw),layers[level].width);
    int ystop = Math.Min((int)Math.Ceiling(rbrc.Y/levelDiv/blockh),layers[level].height);
    for(int x=Math.Max(0,(int)Math.Floor(rtlc.X/levelDiv/blockw)); x<xstop; x++){
      for(int y=Math.Max(0,(int)Math.Floor(rtlc.Y/levelDiv/blockh)); y<ystop; y++){
        if(collideFrLevel(x,y,rtlc,rbrc,level)) return true;
      }
    }
    return false;
  }
  //oloc is offset of other grid in level 0 area space. x,y are level's blockspace
  bool collideMipGridLevel(MipGrid o, Vector2 oloc, int x, int y, int level){
    int levelDiv = 1<<(level*3);
    //offset in level's blockspace
    Vector2 soffset = new Vector2(x*blockw, y*blockh) - oloc/levelDiv;
    Vector2 owhole = soffset.Floor();
    Vector2 ofrac = soffset-owhole;
    ulong self = layers[level].getBlock(x,y);
    ulong other = o.layers[level].getAreaSmeared((int)owhole.X, (int)owhole.Y, ofrac.X!=0, ofrac.Y!=0);
    ulong hit = self&other;
    if(level == 0)return hit!=0;
    while(hit!=0){
      int index = System.Numerics.BitOperations.TrailingZeroCount(hit);
      if(collideMipGridLevel(o,oloc,x*blockw+index%8,y*blockh+index/8,level-1)) return true;
      hit &= hit-1;
    }
    return false;
  }
  public bool collideMipGrid(MipGrid o, Vector2? optionalAtparam=null){
    Vector2 owpos = optionalAtparam??o.tlc;
    if(o.cellshape!=cellshape) throw new Exception("cannot collide grids with different cell shapes yet");
    //into level 0 area space
    Vector2 oloc = (owpos.Round()-tlc.Round())/cellshape;
    //DebugConsole.Write($"===============starting {oloc}");
    int level = Math.Min(o.highestlevel, highestlevel);
    int levelDiv = 1<<(3*level);
    Vector2 low = (oloc/levelDiv).Floor();
    Vector2 high = ((oloc+new Vector2(o.width+1,o.height+1))/levelDiv).Ceiling();

    int xstop = Math.Min((int)Math.Ceiling(high.X/blockw),layers[level].width);
    int ystop = Math.Min((int)Math.Ceiling(high.Y/blockh),layers[level].height);
    for(int x=Math.Max(0,(int)Math.Floor(low.X/blockw)); x<xstop; x++){
      for(int y=Math.Max(0,(int)Math.Floor(low.Y/blockh)); y<ystop; y++){
        if(collideMipGridLevel(o,oloc,x,y,level)) return true;
      }
    }
    return false;
  }
  public bool collideMipGridOffset(MipGrid o, Vector2 offset){
    return collideMipGrid(o, o.tlc+offset);
  }
  public bool collideFrOffset(FloatRect f, Vector2 offset){
    return collideFr(new FloatRect(f.x+offset.X,f.y+offset.Y,f.w,f.h));
  }
  public static string getBlockstr(ulong data){
    string s = "";
    for(int y=0; y<blockh; y++){
      if(y!=0) s+="\n";
      for(int x=0; x<blockw; x++){
        s+=(data&(1UL<<(x+8*y)))!=0?"X":"_";
      }
    }
    return s;
  }
  public static MipGrid fromGrid(Grid g){
    DynamicData d = new DynamicData(g);
    if(d.TryGet("__mipgrid", out var obj) && obj is MipGrid m){
    } else {
      m = new MipGrid(g);
      d.Set("__mipgrid",m);
    }
    return m;
  }
}

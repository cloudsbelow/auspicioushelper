


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using IL.Celeste.Mod.UI;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using FloatVec = System.Numerics.Vector<float>;
using Vec = System.Numerics.Vector;

namespace Celeste.Mod.auspicioushelper;
public class MipGrid{
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
  public static string getRowstr(int data){
    string s = "";
    for(int x=0; x<blockw; x++){
      s+=(data&(1<<(x)))!=0?"X":"_";
    }
    return s;
  }
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
    public ulong[][] chunks;
    public Layer(int width, int height){
      this.width = width; this.height = height;
      if(width*height>ChunkSize*ChunkSize*16){
        gridx=Util.UDiv(width,ChunkSize);
        gridy=Util.UDiv(height,ChunkSize);
        chunks = new ulong[gridx*gridy][];
      }else{
        d=new ulong[width*height];
      }
    }
    public static Layer fromAreasize(int width, int height){
      return new Layer((width+blockw-1)/blockw, (height+blockh-1)/2);
    }
    public void SetBlock(ulong val, int x, int y){
      // DebugConsole.Write($"Set {x},{y}");
      // DebugConsole.Write(getBlockstr(val));
      if((uint)x<(uint)width && (uint)y<(uint)height){
        if(d!=null) d[x+y*width] = val;
        if(chunks!=null){
          int gx=x/ChunkSize;
          int gy=y/ChunkSize;
          int idx = x+ChunkSize*(y-gx-gy*ChunkSize);
          if(chunks[gx+gy*gridx] is {} l) l[idx]=val;
          else if(val!=0){
            ulong[] arr = (chunks[gx+gy*gridx] = new ulong[ChunkSize*ChunkSize]);
            arr[idx]=val;
          }
        }
      } else throw new Exception($"setting invalid area of block {x},{y} on grid of dim {width},{height}");
    }
    public void SetRect(bool val, Int2 tlc, Int2 brc){
      var bdim = new Int2(blockw, blockh);
      var low = Int2.Max(tlc/bdim,Int2.Zero);
      var high = Int2.Min((brc+bdim-1)/bdim,new Int2(width,height));
      for(int x=low.x; x<high.x; x++) for(int y=low.y; y<high.y; y++){
        ulong orig = getBlock(x,y);
        ulong mask = makeRectMask(x,y,tlc,brc,0);
        if(val)SetBlock(orig|mask,x,y);
        else SetBlock(orig&~mask,x,y);
      }
    }
    public void SetPoint(bool val, Int2 loc){
      if(loc.x<0 || loc.y<0 || loc.x>=width*blockw || loc.y>=height*blockh) return;
      Int2 bdim = new Int2(blockw, blockh);
      Int2 blk = loc/bdim;
      Int2 tpos = loc-blk*bdim;
      ulong mask = 1ul<<(tpos.x+8*tpos.y);
      SetBlock((getBlock(blk.x,blk.y) & ~mask)|(val?mask:0ul),blk.x,blk.y);
    }
    public Layer BuildParent(){
      Layer o = new Layer((width+blockw-1)/blockw, (height+blockh-1)/blockh);
      if(d!=null){
        ref ulong mloc = ref MemoryMarshal.GetArrayDataReference(d);
        for(int ty=0; ty<height; ty+=blockh){
          for(int tx=0; tx<width; tx+=blockw){
            ulong blk = 0;
            ulong p = 1;
            for(int fy=0; fy<blockh; fy++){
              for(int fx=0; fx<blockw; fx++){
                int x=tx+fx; int y=ty+fy;
                if(x<width && y<height && Unsafe.Add(ref mloc,x+y*width)!=0) blk |= p;
                p<<=1;
              }
            }
            if(blk!=0) o.SetBlock(blk,tx/blockw,ty/blockh);
          }
        }
      } else {
        if(ChunkSize%blockw!=0 || ChunkSize%blockh!=0) throw new Exception("dont change the numbers");
        for(int gy=0; gy<gridy; gy++) for(int gx=0; gx<gridx; gx++){
          if(chunks[gx+gridx*gy] is {} c){
            ref ulong mloc = ref MemoryMarshal.GetArrayDataReference(c);
            for(int ty=0; ty<ChunkSize; ty+=blockh) for(int tx=0; tx<ChunkSize; tx+=blockw){
              ulong blk = 0;
              ulong p = 1;
              int ox = (ChunkSize*gx+tx)/blockw;
              int oy = (ChunkSize*gy+ty)/blockh;
              if(!((uint)oy<o.height && (uint)ox<o.width)) continue;

              for(int fy=0; fy<blockh; fy++) for(int fx=0; fx<blockw; fx++){
                int x=tx+fx; int y=ty+fy;
                if(Unsafe.Add(ref mloc,x+y*ChunkSize)!=0) blk |= p;
                p<<=1;
              }
              if(blk!=0) o.SetBlock(blk,ox,oy);
            }
          }
        }
      }
      return o;
    }
    public Layer GetSublayer(int sx, int sy, int w, int h){
      Layer o = new((w+blockw-1)/blockw,(h+blockh-1)/blockh);
      for(int iy=0; iy<h; iy+=blockh)for(int ix=0; ix<w; ix+=blockw){
        int x = sx+ix; int y=sy+iy;
        ulong blk = getArea(x,y);
        int hmm = w-ix;
        int vmm = h-iy;
        ulong mask = FULL;
        if(hmm<blockw || vmm<blockh){
          mask = (BYTEMARKER*(byte)(0xff>>(Math.Max(0,blockw-hmm))))>>(8*Math.Max(0,blockh-vmm));
        }
        o.SetBlock(blk & mask, ix/blockw, iy/blockh);
      }
      return o;
    }
    ulong getBlockChunked(int x, int y){
      int gx=(x+ChunkSize)/ChunkSize-1;
      int gy=(y+ChunkSize)/ChunkSize-1;
      if((uint)gx<(uint)gridx && (uint)gy<(uint)gridy && chunks[gx+gy*gridx] is {} c){
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(c), x+ChunkSize*(y-gx-gy*ChunkSize));
      }
      return 0;
    }
    /// <summary>
    /// WARNiNG: undefined behavior for bx,by<-1. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void getBlocktile(int bx, int by, out ulong tl, out ulong tr, out ulong bl, out ulong br){
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
          tl = getDenseBlock(bx,by);
          tr = getDenseBlock(bx+1,by);
          bl = getDenseBlock(bx,by+1);
          br = getDenseBlock(bx+1,by+1);
        }
      }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ulong getDenseBlock(int x, int y){
      if((uint) x<width && (uint) y<height){
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(d), x+y*width);
      }
      return 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ulong getDenseBlockFast(int x, int y){
      return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(d), x+y*width);
    }
    ulong getArea(int x, int y){
      if(x<=-blockw||y<=-blockh) return 0;
      int xb = (x+blockw)/blockw-1;
      int yb = (y+blockh)/blockh-1;
      int xf = x-xb*blockw;
      int yf = y-yb*blockh;
      getBlocktile(xb,yb, out var tl, out var tr, out var bl, out var br);
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
      if(x<-blockw||y<-blockh) return 0;
      int xb = (x+blockw)/blockw-1;
      int yb = (y+blockh)/blockh-1;
      int xf = x-xb*blockw;
      int yf = y-yb*blockh;
      getBlocktile(xb,yb, out var tl, out var tr, out var bl, out var br);

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
        res = res | (res>>8) | ((ulong)em)<<56;
      }
      return res;
    }
    public ulong getBlock(int x, int y){
      if((uint)x<width && (uint)y<height){
        if(d!=null) return getDenseBlockFast(x,y);
        return getBlockChunked(x,y);
      }
      return 0;
    }
    public ulong getAreaAligned(int x, int y)=>getBlock(x/blockw,y/blockh);
    public bool collidePoint(Int2 loc){
      int bx = (loc.x+blockw)/blockw-1;
      int by = (loc.y+blockh)/blockh-1;
      int fx = loc.x-bx*blockw;
      int fy = loc.y-by*blockh;
      return (getBlock(bx,by) & (1UL<<(fx+8*fy)))!=0;
    }
  }
  




  
  internal List<Layer> layers;
  internal int width;
  internal int height;
  internal int highestlevel;
  const ulong FULL = 0xffff_ffff_ffff_ffffUL;
  const ulong BYTEMARKER = 0x0101_0101_0101_0101UL;
  public MipGrid(VirtualMap<bool> map){
    //cellshape = new Vector2(g.CellWidth,g.CellHeight);
    Layer l = new Layer((map.Columns+blockw-1)/blockw,(map.Rows+blockh-1)/blockh);
    int ss = VirtualMap<bool>.SegmentSize;
    int mlx = map.segments.GetLength(0);
    int mly = map.segments.GetLength(1);
    for(int yb=0; yb<map.Rows; yb+=blockh){
      for(int xb=0; xb<map.Columns; xb+=blockw){
        bool flag=false;
        int lx = xb/ss; int hx = (xb+blockw)/ss;
        int ly = yb/ss; int hy = (yb+blockh)/ss;
        for(int i=lx; i<=hx && i<mlx; i++) for(int j=ly; j<=hy && j<mly; j++) flag|=map.AnyInSegment(i,j);
        if(!flag) continue;
        
        ulong block = 0;
        int xstop = Math.Min(blockw,map.Columns-xb);
        int ystop = Math.Min(blockh,map.Rows-yb);
        for(int y=0; y<ystop; y++){
          for(int x=0; x<xstop; x++){
            if(map[x+xb,y+yb])block |= 1UL<<(x+y*8);
          }
        }
        
        l.SetBlock(block,xb/blockw,yb/blockh);
      }
    }

    layers = [l];
    width = map.Columns;
    height = map.Rows;
    buildMips();
  }
  public MipGrid(Grid g):this(g.Data){}
  public MipGrid(Layer l, bool noMips=false){
    layers = [l];
    width = l.width*blockw;
    height = l.height*blockh;
    if(noMips){
      highestlevel = 0;
    } else buildMips();
  }
  void buildMips(){
    Layer b = layers[0];
    if(layers.Count != 1) throw new Exception("only build mips on new grid");
    while(Math.Max(b.width,b.height)>=4){
      layers.Add(b = b.BuildParent());
    }
    highestlevel = layers.Count-1;
    //DebugConsole.Write("Highest level:",highestlevel, width, height, layers[highestlevel].width);
  }
  void forceMip(){
    if(layers.Count !=1) return;
    layers.Add(layers[0].BuildParent());
    highestlevel=1;
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static ulong makeRectMask(int blockx, int blocky, Int2 otlc, Int2 obrc, int level){
    int leveldenom = level+level+level;
    Int2 offset = new Int2(blockw*blockx, blockh*blocky);
    Int2 rtlc = (otlc>>leveldenom)-offset;
    Int2 rbrc = ((obrc+(1<<leveldenom)-1)>>leveldenom)-offset;
    if(rbrc.x<=0 || rbrc.y<=0 || rtlc.x>=blockw || rtlc.y>=blockh) return 0;
    if(rtlc.x<=0 && rtlc.y<=0 && rbrc.x>=blockw && rbrc.y>=blockh) return FULL;
    int x1 = Math.Clamp((int) rtlc.x,0,blockw-1);
    int y1 = Math.Clamp((int) rtlc.y,0,blockh-1);
    int x2 = Math.Clamp((int) rbrc.x,1,blockw);
    int y2 = Math.Clamp((int) rbrc.y,1,blockh);
    if(x2<=x1 || y2<=y1) return 0;
    byte row = (byte)(((1<<(x2-x1))-1)<<x1);
    int rows = y2-y1;
    var mask = rows==8? FULL: ((1UL << (rows*8))-1) << (y1*8);
    return mask & (0x0101_0101_0101_0101UL*row);
  }
  //x and y are in block space for the level
  bool collideFrLevel(int x, int y, Int2 otlc, Int2 obrc, int level){
    //DebugConsole.Write($"CollideLevel{level} {x} {y}");
    ulong dat = layers[level].getBlock(x,y);
    ulong mask = makeRectMask(x,y,otlc,obrc,level);
    //DebugConsole.Write(Util.sideBySide([getBlockstr(dat),getBlockstr(mask)]));
    if(dat == 0 || mask == 0) return false;
    ulong hit = dat&mask;
    if(level == 0) return hit!=0;
    while(hit != 0){
      int index = System.Numerics.BitOperations.TrailingZeroCount(hit);
      if(collideFrLevel(x*blockw+index%8,y*blockh+index/8,otlc,obrc,level-1)) return true;
      hit &= hit-1;
    }
    return false;
  }
  public bool collideInFrame(IntRect f){
    int mld = Math.Max(f.w,f.h);
    int level = 0;
    while(level<highestlevel && (1<<(3*(level+1)))<mld)level++;
    int leveldenom = level+level+level+3;
    Int2 rtlc = Int2.Max(f.tlc,0);
    Int2 rbrc = Int2.Min(f.brc, new Int2(width,height));
    if(rbrc.x<0 || rbrc.y<0 || rtlc.x>=width || rtlc.y>=height) return false;

    int addAmt = 1<<leveldenom;
    int xstop = (rbrc.x+addAmt-1)>>leveldenom;
    int ystop = (rbrc.y+addAmt-1)>>leveldenom;
    for(int x=rtlc.x>>leveldenom; x<xstop; x++){
      for(int y=rtlc.y>>leveldenom; y<ystop; y++){
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
    ulong other = o.layers[level].getAreaSmearedFast((int)owhole.X, (int)owhole.Y, ofrac.X!=0, ofrac.Y!=0);
    ulong hit = self&other;
    if(level == 0)return hit!=0;
    while(hit!=0){
      int index = System.Numerics.BitOperations.TrailingZeroCount(hit);
      if(collideMipGridLevel(o,oloc,x*blockw+index%8,y*blockh+index/8,level-1)) return true;
      hit &= hit-1;
    }
    return false;
  }
  //assumes grids are same cell size; oloc is in local coordinates
  public bool collideGridSameCs(MipGrid o, Vector2 oloc){
    int level = Math.Min(o.highestlevel, highestlevel);
    int levelDiv = 1<<(3*level);
    Vector2 low = (oloc/levelDiv).Floor();
    Vector2 high = ((oloc+new Vector2(o.width+1,o.height+1))/levelDiv).Ceiling();

    int xstop = Math.Min((int)Math.Ceiling(high.X/blockw),layers[level].width);
    int ystop = Math.Min((int)Math.Ceiling(high.Y/blockh),layers[level].height);
    for(int x=Math.Max(0,(int)Math.Floor(low.X/blockw)); x<=xstop; x++){
      for(int y=Math.Max(0,(int)Math.Floor(low.Y/blockh)); y<=ystop; y++){
        if(collideMipGridLevel(o,oloc,x,y,level)) return true;
      }
    }
    return false;
  }
  bool collideMipGridLevelOffset(MipGrid o, Vector2 oloc, int x, int y, int level){
    //offset in level's blockspace
    float sofac = level==-1? 8f : 1f/(1<<(level*3));
    Vector2 soffset = new Vector2(x*blockw, y*blockh) - oloc*sofac;
    Vector2 owhole = soffset.Floor();
    Vector2 ofrac = soffset-owhole;
    ulong self = level==-1?FULL:layers[level].getBlock(x,y);
    ulong other = o.layers[level+1].getAreaSmearedFast((int)owhole.X, (int)owhole.Y, ofrac.X!=0, ofrac.Y!=0);
    ulong hit = self&other;
    if(level == -1)return hit!=0;
    while(hit!=0){
      int index = System.Numerics.BitOperations.TrailingZeroCount(hit);
      if(collideMipGridLevelOffset(o,oloc,x*blockw+index%8,y*blockh+index/8,level-1)) return true;
      hit &= hit-1;
    }
    return false;
  }
  //assumes MipGrid o is one level more detailed
  public bool collideGridLowCs(MipGrid o, Vector2 oloc){
    if(o.highestlevel == 0)o.forceMip();
    int level = Math.Min(o.highestlevel-1, highestlevel);
    int levelDiv = 1<<(3*level);
    Vector2 low = (oloc/levelDiv).Floor();
    //We don't worry about rounding here because all instances of partialtiles constructed via layer; will be multiples
    Vector2 high = ((oloc+new Vector2(o.width/blockw+1,o.height/blockh+1))/levelDiv).Ceiling();

    int xstop = Math.Min((int)Math.Ceiling(high.X/blockw),layers[level].width);
    int ystop = Math.Min((int)Math.Ceiling(high.Y/blockh),layers[level].height);
    for(int x=Math.Max(0,(int)Math.Floor(low.X/blockw)); x<=xstop; x++){
      for(int y=Math.Max(0,(int)Math.Floor(low.Y/blockh)); y<=ystop; y++){
        if(collideMipGridLevelOffset(o,oloc,x,y,level)) return true;
      }
    }
    return false;
  }
  public bool collidePoint(Int2 loc){
    int bx = (loc.x+blockw)/blockw-1;
    int by = (loc.y+blockh)/blockh-1;
    int fx = loc.x-bx*blockw;
    int fy = loc.y-by*blockh;
    return (layers[0].getBlock(bx,by) & (1UL<<(fx+8*fy)))!=0;
  }



    // p=p-center;
    // Vector2 d=Vector2.Max(Vector2.Zero,new Vector2(Math.Abs(p.X)-w,Math.Abs(p.Y)-h));
    // return d.X*d.X+d.Y*d.Y<r*r;
  static string AsStr(FloatVec v){
    string s = "";
    for(int i=0; i<FloatVec.Count; i++) s+=v[i]+" ";
    return s;
  }
  static ulong MakeCircleMask(int blockx, int blocky, Vector2 loc, float rad, int level){
    float ldenom = 1<<(level*3); 
    rad = rad/ldenom;
    loc = loc/ldenom - new Vector2(blockx*8, blocky*8);
    ulong ret = 0;
    FloatVec half = new(0.5f);
    float xloc = loc.X;
    FloatVec yc = new(loc.Y);
    FloatVec r2 = new(rad*rad);
    Span<float> items = stackalloc float[FloatVec.Count];
    for(int i=0; i<8; i++){
      int mask = 0;
      for(int j=0; j<8; j+=FloatVec.Count){
        for(int k=0; k<FloatVec.Count; k++) items[k] = xloc-j-k;
        var px = new FloatVec(items);
        // DebugConsole.Write(AsStr(px));
        var py = yc - half*(2*i+1);
        var dx = Vec.Max(Vec.Max(px-half,FloatVec.Zero),-px-half);
        var dy = Vec.Max(Vec.Max(py-half,FloatVec.Zero),-py-half);
        var del = dx*dx+dy*dy;
        var f = Vec.LessThan(del,r2);
        for(int k=0; k<FloatVec.Count; k++)mask|=(f[k]&1)<<(j+k);
      }
      ret |= ((ulong)mask)<<(i*8);
    }
    return ret;
  }
  bool collideCircleLevel(int x, int y, Vector2 center, float rad, int level){
    ulong dat = layers[level].getBlock(x,y);
    ulong mask = MakeCircleMask(x,y,center,rad,level);
    if(dat == 0 || mask == 0) return false;
    ulong hit = dat&mask;
    if(level == 0) return hit!=0;
    while(hit != 0){
      int index = System.Numerics.BitOperations.TrailingZeroCount(hit);
      if(collideCircleLevel(x*blockw+index%8,y*blockh+index/8,center,rad,level-1)) return true;
      hit &= hit-1;
    }
    return false;
  }
  public bool CollideCircle(Vector2 center, float rad){
    Vector2 rv = new(rad,rad);
    IntRect f = IntRect.fromCorners(Int2.Floor(center-rv), Int2.Ceil(center+rv));
    int mld = Math.Max(f.w,f.h);
    int level = 0;
    while(level<highestlevel && (1<<(3*(level+1)))<mld)level++;
    int leveldenom = level+level+level+3;
    Int2 rtlc = Int2.Max(f.tlc,0);
    Int2 rbrc = Int2.Min(f.brc, new Int2(width,height));
    if(rbrc.x<0 || rbrc.y<0 || rtlc.x>=width || rtlc.y>=height) return false;

    int addAmt = 1<<leveldenom;
    int xstop = (rbrc.x+addAmt-1)>>leveldenom;
    int ystop = (rbrc.y+addAmt-1)>>leveldenom;
    for(int x=rtlc.x>>leveldenom; x<xstop; x++){
      for(int y=rtlc.y>>leveldenom; y<ystop; y++){
        if(collideCircleLevel(x,y,center,rad,level)) return true;
      }
    }
    return false;
  }
}

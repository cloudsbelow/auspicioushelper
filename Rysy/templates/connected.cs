

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rysy;
using Rysy.Components;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Selections;

namespace auspicioushelper.Rysy;

public partial class RoomData{
  public List<ISprite> cachedConnectedSprites = null;
}
public class ConnectedTiles:LonnEntity{
  [CustomEntity("auspicioushelper/ConnectedBlocks",["auspicioushelper"])]
  class ConnectedFg:ConnectedTiles{}
  [CustomEntity("auspicioushelper/ConnectedBlocksBg",["auspicioushelper"])]
  class ConnectedBg:ConnectedTiles{}
  [CustomEntity("auspicioushelper/ConnectedContainer",["auspicioushelper"])]
  class ConnectedCont:ConnectedTiles{}
  public bool handled = false;
  public override IEnumerable<ISprite> GetSprites() {
    if(handled) return new List<ISprite>(){ISprite.Rect(Pos,Width,Height,Color.White*0.4f)};
    return base.GetSprites();
  }
  public override void OnChanged(EntityDataChangeCtx changed) {
    base.OnChanged(changed);
    this.GetRoomdata().cachedConnectedSprites = null;
  }
  IntRect Bounds()=>new((int)Math.Round(Pos.X),(int)Math.Round(Pos.Y),Width,Height);
  public static List<ISprite> ProcessScene(Room r, RoomData rd){
    if(rd.cachedConnectedSprites != null) return rd.cachedConnectedSprites;
    var sprites = rd.cachedConnectedSprites = new();
    
    var allThings_ = r.Entities.OfType<ConnectedTiles>();
    List<(IntRect,ConnectedTiles,int)> allThings = new();
    for(int i=0; i<allThings_.Count; i++) allThings.Add(new(allThings_[i].Bounds(),allThings_[i],i));

    List<(Util.QuickCollider<ConnectedTiles>, int)> cbs = new();
    while(allThings.Count>0){
      List<(IntRect,ConnectedTiles, int)> things = new();
      int idx=0;
      things.Add(allThings[^1]);
      allThings.RemoveAt(allThings.Count-1);
      while(idx<things.Count){
        int nidx=things.Count;
        allThings.RemoveAll(x=>{
          for(int i=idx; i<nidx; i++) if(things[i].Item1.CollideIr(x.Item1)){
            things.Add(x);
            return true;
          }
          return false;
        });
        idx=nidx;
      } //

      Int2 min = things.ReduceMapI(a=>a.Item1.tlc,Int2.Min);
      Int2 max = things.ReduceMapI(a=>a.Item1.brc,Int2.Max);
      Int2 size = (max-min)/8;
      things.Sort((a,b)=>a.Item3-b.Item3);
      //I know this is transposed of usual notation! I don't care.
      char[,] fgd = new char[size.x,size.y];
      char[,] bgd = new char[size.x,size.y];
      bool useFg=false;
      bool useBg=false;
      Util.QuickCollider<ConnectedTiles> qcl = new();
      foreach(var a in things){
        Int2 dloc = (a.Item1.tlc-min)/8;
        Int2 hloc = (a.Item1.brc-min)/8;
        char tid = a.Item2.Attr("tiletype","0").FirstOrDefault();
        switch(a.Item2){
          case ConnectedFg: fgd.FillRect(dloc,hloc,tid); useFg=true; a.Item2.handled=true; break;
          case ConnectedBg: bgd.FillRect(dloc,hloc,tid); useBg=true; break;
          case ConnectedCont: qcl.Add(a.Item2,a.Item1); break;
          default: throw new Exception("Not possible unless someone is doign something mean to me");
        } 
      }
      if(useFg){
        AutotiledSprite[,] fgctx = new AutotiledSprite[size.x,size.y];
        Autotiler tiler = r.Map.FgAutotiler;
        List<char> unused = new();
        for(int i=0; i<size.x; i++) for(int j=0; j<size.y; j++) if(fgd[i,j]!=default){
          fgctx[i,j] = tiler.GetSprite(new ConnectedTilechecker(i,j,fgd),i,j,ref unused);
        } 
        sprites.Add(new AutotileSpriterect(IntRect.fromCorners(min,max),new(0,0,size.x,size.y),fgctx) with {
          Depth = -15000
        });
      }
      if(useBg){}
      
    } 
    return sprites;
  }
  public static void AddTileSprites(List<ISprite> into, Autotiler tiler, char[,] data, IntRect bounds){

  }

  public struct ConnectedTilechecker(int cx, int cy, char[,] data):ISimpleTilechecker{
    public char GetTileAt(int x, int y, char def) {
      char c = (x>=0 && y>=0 && x<data.GetLength(0) && y<data.GetLength(1))? data[x,y]:def;
      if(c == default) return def;
      if(c=='\n') return x==cx && y==cy? def:data[cx,cy];
      return c;
    }
  }
  public interface ISimpleTilechecker:ITileChecker{
    bool ITileChecker.IsInBounds(int x, int y)=>true;
    bool ITileChecker.ExtendOutOfBounds()=>false;
    bool ITileChecker.IsConnectedTileAt(int x, int y, TilesetData data) {
      return data.IsTileConnected(GetTileAt(x,y,'0'));
    }
  }
  public record class AutotileSpriterect(IntRect bounds, IntRect tr, AutotiledSprite[,] tilectx):ISprite{
    public int? Depth {get;set;}
    public Color Color { get; set; } = Color.White;
    public Color Border { get; set; } = Color.Yellow*0.5f;
    public bool IsLoaded=>true;
    public bool wrong=false;
    ISprite ISprite.WithMultipliedAlpha(float alpha)=> this with{
      Color = Color*alpha, Border = Border*alpha
    };
    void ISprite.Render(SpriteRenderCtx ctx){
      Camera camera = ctx.Camera;
      //if(camera != null && !camera.IsRectVisible(bounds)) return;
      SpriteBatch b = Gfx.Batch;
      if(!wrong) for(int i=0; i<tr.w; i++) for(int j=0; j<tr.h; j++){
        tilectx[i+tr.x,j+tr.y]?.RenderAt(b, bounds.tlc+new Int2(i,j)*8, Color);
      } else b.Draw(Gfx.Pixel,bounds,Color);
      b.Draw(Gfx.Pixel, new IntRect(bounds.x,bounds.y,bounds.w,1), Border);
      b.Draw(Gfx.Pixel, new IntRect(bounds.x,bounds.y+1,1,bounds.h-2), Border);
      b.Draw(Gfx.Pixel, new IntRect(bounds.x+bounds.w-1,bounds.y+1,1,bounds.h-2), Border);
      b.Draw(Gfx.Pixel, new IntRect(bounds.x,bounds.y+bounds.h-1,bounds.w,1), Border);
    }
    ISelectionCollider ISprite.GetCollider()=>ISelectionCollider.FromRect(bounds);
  }
}
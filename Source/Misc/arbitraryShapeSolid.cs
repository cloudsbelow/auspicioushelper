


using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/ArbitrarySolid")]
public class ArbitraryShapeSolid:Solid{
  static Dictionary<string, (Vector2, MipGrid)> cache = new();
  static ArbitraryShapeSolid(){
    auspicioushelperModule.OnEnterMap.enroll(new PersistantAction(cache.Clear));
  }
  MTexture imag;
  Vector2 offset;
  public ArbitraryShapeSolid(EntityData d, Vector2 o):base(d.Position+o,0,0,d.Bool("safe")){
    string path = d.Attr("CustomColliderPath","");
    if(string.IsNullOrWhiteSpace(path)) path = d.Attr("image");
    MakeCollider(path);
    imag = GetTextureAtPath(d.Attr("image"));
  }
  MTexture GetTextureAtPath(string path){
    if(GFX.Game.textures.ContainsKey(path)) return GFX.Game[path];
    else return GFX.Game.GetAtlasSubtextureFromAtlasAt(path, 0);
  }
  void MakeCollider(string path){
    if(cache.TryGetValue(path,out var cmg)){
      Collider = new MiptileCollider(cmg.Item2,Vector2.One);
      Collider.Position = cmg.Item1;
    } else {
      MTexture tex = GetTextureAtPath(path);
      Color[] dat = Util.TexData(tex, out int w, out int h);
      int gw = (w+8-1)/8;
      int gh = (h+8-1)/8;
      MipGrid.Layer l = new(gw, gh);
      for(int bx=0; bx<gw; bx++) for(int by=0; by<gh; by++){
        ulong block = 0;
        ulong num = 1;
        for(int ty=0; ty<8; ty++) for(int tx=0; tx<8; tx++){
          int x = bx*8+tx; int y = by*8+ty;
          if(x<w && y<h && dat[x+w*y].A>50) block|=num;
          num<<=1;
        }
        l.SetBlock(block,bx,by);
      }
      MipGrid m = new(l);
      Collider = new MiptileCollider(m,Vector2.One);
      cache.Add(path, new(Collider.Position = (tex.DrawOffset-tex.Center)/tex.ScaleFix, m));
    }
  }
  public override void Render() {
    base.Render();
    imag.DrawCentered(Position);
  }
}

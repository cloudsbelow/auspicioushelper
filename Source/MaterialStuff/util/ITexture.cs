

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public abstract class ITexture{
  public abstract Texture2D tex{get;}
  public static implicit operator Texture2D(ITexture h)=>h.tex;

  public class UserLayerWrapper:ITexture{
    IMaterialLayer l = null;
    string i;
    public override Texture2D tex{get{
      if(l == null){
        if(!string.IsNullOrWhiteSpace(i)) l = MaterialController.getLayer(i);
        if(l == null){
          DebugConsole.WriteFailure($"Tried to access the layer {i} which does not exist");
          return null;
        }
      }
      if(!l.enabled){
        var nl = MaterialController.getLayer(i);
        if(l!=nl){
          l=nl;
          return tex;
        }
        DebugConsole.WriteFailure("Trying to use disabled texture as input");
        return null;
      }
      return l.outtex;
    }}
    public UserLayerWrapper(string ident){
      i=ident;
    }
    public UserLayerWrapper(IMaterialLayer l){
      this.l=l;
    }
  }
  public class LayerWrapper:ITexture{
    IMaterialLayer l = null;
    public override Texture2D tex{get{
      if(!l.enabled){
        DebugConsole.WriteFailure("Trying to use disabled texture as input");
        return null;
      }
      return l.outtex;
    }}
    public LayerWrapper(IMaterialLayer l){
      this.l=l;
    }
  }
  public class ImageWrapper:ITexture{
    static Dictionary<string, Texture2D> cached = new();
    static ImageWrapper(){
      auspicioushelperModule.OnEnterMap.enroll(new ScheduledAction(()=>{
        foreach(var c in cached) c.Value.Dispose();
        cached.Clear();
        return false;
      },"clear cached images"));
    }
    public override Texture2D tex {get;}
    public ImageWrapper(string gfxPath){
      if(!cached.TryGetValue(gfxPath,out var texture)){
        if(Everest.Content.Get(Util.concatPaths("Graphics",gfxPath)) is { } a){
          using(Stream stream = a.Stream){
            cached.Add(gfxPath, tex = Texture2D.FromStream(auspicioushelperGFX.gd, stream));
          }
        } else {
          string path = Util.removeWhitespace(gfxPath).RemovePrefix("/").RemovePrefix("Atlases/").RemoveSuffix(".png");
          string first = path.Split("/")[0];
          Atlas from = first switch {
            "ColorGrading"=>GFX.ColorGrades,
            "Gameplay"=>GFX.Game,
            "Gui"=>GFX.Gui,
            // ""=>GFX.Opening,
            // ""=>GFX.Misc,
            "Portraits"=>GFX.Portraits,
            _=>null
          };
          path=path.RemovePrefix(first+"/");
          if(from?[path] is {} mtex){
            var dat = Util.TexData(mtex, out var w, out var h);
            tex = new Texture2D(auspicioushelperGFX.gd, w,h);
            tex.SetData(dat);
            cached.Add(gfxPath, tex);
          }
        }
        if(tex!=null) DebugConsole.Write("Texture loaded:", gfxPath, tex?.Width, tex?.Height);
        else DebugConsole.WriteFailure($"Texture failed to load: {gfxPath}",true);
      } else tex=texture;
    }
    static ModAsset GetAsset(string asset){
      if(Everest.Content.Get(Util.concatPaths("Graphics",asset)) is { } a) return a;
      DebugConsole.WriteFailure($"Could not find image at {asset}",true);
      return null;
    }
  }
  public class BgWrapper:ITexture{
    public override Texture2D tex{get{
      if(!MaterialPipe.orderFlipped){
        DebugConsole.WriteFailure("Tried to use the background texture without flipping renderer");
        return null;
      }
      return GameplayBuffers.Level;
    }}
  }
  public class LevelWrapper:ITexture{
    public override Texture2D tex=>GameplayBuffers.Level;
  }
  public class GpWrapper:ITexture{
    public override Texture2D tex=>GameplayBuffers.Gameplay;
  }
  public static BgWrapper bgWrapper = new BgWrapper();
  public static LevelWrapper lvWrapper = new LevelWrapper();
  public static GpWrapper gpWrapper = new GpWrapper();
}


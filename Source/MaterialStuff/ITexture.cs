

using System.Collections.Generic;
using System.IO;
using IL.Monocle;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.auspicioushelper;

public abstract class ITexture{
  public abstract Texture2D tex{get;}
  public static implicit operator Texture2D(ITexture h)=>h.tex;

  public class UserLayerWrapper:ITexture{
    IMaterialLayer l = null;
    string i;
    public override Texture2D tex{get{
      if(l == null){
        l = MaterialController.getLayer(i);
        if(l == null) DebugConsole.WriteFailure($"Tried to access the layer {i} which does not exist");
      }
      if(!l.enabled){
        DebugConsole.WriteFailure("Trying to use disabled texture as input");
        return null;
      }
      return l.outtex;
    }}
    public UserLayerWrapper(string ident){
      i=ident;
    }
  }
  public class ImageWrapper:ITexture{
    static Dictionary<ModAsset, Texture2D> cached = new();
    static ImageWrapper(){
      auspicioushelperModule.OnEnterMap.enroll(new ScheduledAction(()=>{
        foreach(var c in cached) c.Value.Dispose();
        cached.Clear();
        return false;
      },"clear cached images"));
    }
    public override Texture2D tex {get;}
    public ImageWrapper(ModAsset asset){
      if(!cached.TryGetValue(asset,out var texture)){
        using (Stream stream = asset.Stream) {
          cached.Add(asset, tex = Texture2D.FromStream(auspicioushelperGFX.gd, stream));
        }
      } else tex=texture;
    }
    public ImageWrapper(string pathToAsset)
      :this(Everest.Content.Get(Util.concatPaths("Graphics",pathToAsset))){}
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
  public static BgWrapper bgWrapper = new BgWrapper();
  public class GpWrapper:ITexture{
    public override Texture2D tex=>GameplayBuffers.Gameplay;
  }
  public static GpWrapper gpWrapper = new GpWrapper();
}


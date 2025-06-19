using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
//I have become the c# abstraction-enraptured person lmao like actually what is this haha
//OK to be sort of fair I intended to do a custom rasterMats for this but decided not.
public class ChannelMaterialsA:BasicMaterialLayer{
  private List<IMaterialObject> bgItemsDraw = new List<IMaterialObject>();
  public RenderTargetPool.RenderTargetHandle bgtex = new(false);
  public bool enabled=>info.enabled;
  //public override RenderTarget2D outtex => bgtex;
  public ChannelMaterialsA():base([null,auspicioushelperGFX.LoadShader("emptynoise/channelmats")],new LayerFormat{
    depth = -13000
  }){DebugConsole.Write("constructing channel material layer");}
  public override void onEnable() {
    base.onEnable();
    bgtex.Claim();
  }
  public override void onRemove() {
    base.onRemove();
    bgtex.Free();
  }
  public void planDrawBG(IMaterialObject t){
    if(info.enabled) bgItemsDraw.Add(t);
  }
  public override void render(SpriteBatch sb, Camera c){
    MaterialPipe.gd.SetRenderTarget(bgtex);
    MaterialPipe.gd.Clear(Color.Transparent);
    
    StartSb(sb,null,c);
    foreach(IMaterialObject b in bgItemsDraw){
      b.renderMaterial(this, c);
    }
    sb.End();
    MaterialPipe.gd.Textures[1]=bgtex;
    bgItemsDraw.Clear();
    base.render(sb,c);
  }
}
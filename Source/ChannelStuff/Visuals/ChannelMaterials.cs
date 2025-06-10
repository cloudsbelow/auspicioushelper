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
  public RenderTarget2D bgtex;
  public bool enabled=>info.enabled;
  //public override RenderTarget2D outtex => bgtex;
  public ChannelMaterialsA():base([null,auspicioushelperGFX.LoadShader("emptynoise/channelmats")],new LayerFormat{
    clearWilldraw=true, depth = -13000
  }){
    bgtex = new RenderTarget2D(Engine.Instance.GraphicsDevice, 320, 180);
  }
  public void planDrawBG(IMaterialObject t){
    if(info.enabled) bgItemsDraw.Add(t);
  }
  public override void render(SpriteBatch sb, Camera c){
    MaterialPipe.gd.SetRenderTarget(bgtex);
    MaterialPipe.gd.Clear(Color.Transparent);
    
    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, GameplayRenderer.instance.Camera.Matrix);
    DebugConsole.Write(c.Matrix.ToString());
    foreach(IMaterialObject b in bgItemsDraw){
      b.renderMaterial(this, sb, c);
    }
    sb.End();
    MaterialPipe.gd.Textures[1]=bgtex;
    bgItemsDraw.Clear();
    base.render(sb,c);
  }
  public void planDraw(IMaterialObject o){
    if(info.enabled) willdraw.Add(o);
  }
}
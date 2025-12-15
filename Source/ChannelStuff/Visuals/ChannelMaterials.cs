using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
//I have become the c# abstraction-enraptured person lmao like actually what is this haha
//OK to be sort of fair I intended to do a custom rasterMats for this but decided not.
public class ChannelMaterialsA:BasicMaterialLayer, CachedUserMaterial{
  public string identifier {get;set;}
  public IOverrideVisualsEasy.Class bg = new();
  public RenderTargetPool.RenderTargetHandle bgtex = new(false);
  public override RenderTarget2D outtex => base.outtex;
  public bool enabled=>info.enabled;
  [Import.SpeedrunToolIop.Static]
  public static ChannelMaterialsA layerA;
  //public override RenderTarget2D outtex => bgtex;
  public ChannelMaterialsA():base([null,auspicioushelperGFX.LoadShader("emptynoise/channelmats")],new LayerFormat{
    depth = -13000
  }){DebugConsole.Write("constructing channel material layer");}
  public override void onEnable() {
    base.onEnable();
    bgtex.Claim();
    layerA=this;
  }
  public override void onRemove() {
    base.onRemove();
    bgtex.Free();
    layerA=null;
  }
  public void planDrawBG(OverrideVisualComponent t){
    t.AddToOverride(new(bg,-30000,false,true));
  }
  public override void render(SpriteBatch sb, Camera c){
    MaterialPipe.gd.SetRenderTarget(bgtex);
    MaterialPipe.gd.Clear(Color.Transparent);
    
    StartSb(sb,null,c);
    foreach(var b in bg.comps){
      b.renderMaterial(this, c);
    }
    sb.End();
    MaterialPipe.gd.Textures[1]=bgtex;
    base.render(sb,c);
  }
}
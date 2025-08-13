


using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

public class FgCassetteVisuals:BasicMaterialLayer{
  CassetteMaterialLayer.CassetteMaterialFormat f;
  public FgCassetteVisuals(CassetteMaterialLayer.CassetteMaterialFormat format):base([Shader],format.fgdepth){
    f=format;
  }
  public override bool autoManageRemoval=>false;
  public override void render(SpriteBatch sb, Camera c){
    shader.setparamvalex("highcol",f.fghigh.ToVector4());
    shader.setparamvalex("lowcol",f.fglow.ToVector4());
    shader.setparamvalex("fgsat",f.fgsat);
    base.render(sb,c);
  }
  //why am I so obtuse
  static VirtualShader shader;
  static VirtualShader Shader {get{
    resources.enable();
    return shader;
  }}
  static HookManager resources = new HookManager(()=>{
    shader = auspicioushelperGFX.LoadShader("cassette/fgtint");
  },()=>{});
}
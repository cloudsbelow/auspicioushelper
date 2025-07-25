


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
  public HashSet<Template> templates=new();
  public List<Entity> todraw=new();
  public bool dirty=true;
  public override bool autoManageRemoval=>false;
  public override void render(SpriteBatch sb, Camera c){
    if(dirty){
      todraw.Clear();
      foreach(Template t in templates) t.AddAllChildren(todraw);
      todraw.Sort(EntityList.CompareDepth);
      dirty = false;
    }
    shader.setparamvalex("highcol",f.fghigh.ToVector4());
    shader.setparamvalex("lowcol",f.fglow.ToVector4());
    shader.setparamvalex("fgsat",f.fgsat);
    MaterialPipe.gd.SetRenderTarget(outtex);
    MaterialPipe.gd.Clear(Color.Transparent);
    StartSb(sb, shader, c);
    foreach(Entity e in todraw) if(e.Scene!=null){
      if(CassetteMaterialLayer.stupididiotdumbpompusthings.TryGetValue(e.GetType(),out var fn))fn(e);
      else e.Render();
    }
    sb.End();
    info.diddraw=true;
  }
  public void Add(Template t){
    dirty = true;
    templates.Add(t);
  }
  public void Remove(Template t){
    dirty = true;
    templates.Remove(t);
    todraw.Clear();
  }
  public override bool checkdo(){
    return templates.Count>0;
  }
  public override void onRemove(){
    templates.Clear();
    todraw.Clear();
    base.onRemove();
  }
  public void Clean(){
    HashSet<Template> keep = new();
    foreach(Template e in templates){
      if(e.Scene!=null)keep.Add(e);
    }
    templates = keep;
    dirty = true;;;;;
    todraw.Clear();
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
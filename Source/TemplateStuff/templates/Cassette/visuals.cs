



using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class CassetteMaterialLayer:BasicMaterialLayer{
  public CassetteMaterialFormat format;
  string channel;
  public struct CassetteMaterialFormat{
    public Color border = Util.hexToColor("fff");
    public Color innerlow = Util.hexToColor("333");
    public Color innerhigh = Util.hexToColor("aaa");
    public Color fghigh = Util.hexToColor("88f");
    public Color fglow = Util.hexToColor("448");
    public float fgsat = 0.5f;
    public bool hasfg;
    public float fgdepth = 1;
    public Vector4 patternvec = new Vector4(0.5f,0.5f,0,0);
    public float alphacutoff = 0.1f;
    public float stripecutoff = 0f;
    public float depth = 8999;
    public int edepth = 9500;
    public VirtualShaderList passes;
    public CassetteMaterialFormat(){}

    public static CassetteMaterialFormat fromDict(Dictionary<string,string> dict){
      CassetteMaterialFormat c = new CassetteMaterialFormat();
      foreach(var pair in dict){
        switch(pair.Key){
          case "border": c.border=Util.hexToColor(pair.Value.Trim()); break;
          case "innerlow": c.innerlow=Util.hexToColor(pair.Value.Trim()); break;
          case "innerhigh": c.innerhigh=Util.hexToColor(pair.Value.Trim()); break;
          case "color":
            Color h =Util.hexToColor(pair.Value.Trim());
            c.border = h; 
            c.innerhigh = new Color(0.5f*h.ToVector4()); 
            c.innerlow = new Color(0.3f*h.ToVector4());
          break;
          case "innercolor":
            Color h2 = Util.hexToColor(pair.Value.Trim());
            c.innerhigh = h2;
            c.innerlow = h2;
            break;
          case "x": c.patternvec.X=float.Parse(pair.Value); break;
          case "y": c.patternvec.Y=float.Parse(pair.Value); break;
          case "w": c.patternvec.X=MathF.PI/float.Parse(pair.Value);break;
          case "h": c.patternvec.Y=MathF.PI/float.Parse(pair.Value);break;
          case "time": c.patternvec.Z=float.Parse(pair.Value); break;
          case "phase": c.patternvec.W=float.Parse(pair.Value); break;
          case "depth":c.depth = float.Parse(pair.Value); break;
          case "edepth":c.edepth = int.Parse(pair.Value); break;
          case "alphacutoff":c.alphacutoff = float.Parse(pair.Value); break;
          case "stripecutoff":c.stripecutoff = float.Parse(pair.Value); break;
          case "style":
            switch(pair.Value){
              case "vanilla": c.passes=vanillaPasses;break;
              case "simple": c.passes=simplePasses;break;
              default:
                var pass = Util.listparseflat(pair.Value,true,true);
                c.passes=new();
                foreach(string p in pass){
                  if(string.IsNullOrWhiteSpace(p)||p=="null") c.passes.Add(null);
                  else c.passes.Add(auspicioushelperGFX.LoadShader(p));
                }
                break;
            }
          break;
          case "fg": 
            c.hasfg = true;
            if(float.TryParse(pair.Value,out var fgd)) c.fgdepth = fgd;
            break;
          case "fghigh": c.fghigh = Util.hexToColor(pair.Value); break;
          case "fglow": c.fglow = Util.hexToColor(pair.Value); break;
          case "fgsat": c.fgsat = float.Parse(pair.Value); break;
        }
      }
      if(c.passes == null) c.passes=vanillaPasses;
      return c;
    }
    public int gethash(){
      return HashCode.Combine(border, innerlow, innerhigh, patternvec, alphacutoff, stripecutoff, depth, HashCode.Combine(
        hasfg, fghigh, fglow, fgdepth
      ));
    }
  }
  [Import.SpeedrunToolIop.Static]
  public static Dictionary<string, CassetteMaterialLayer> layers = new Dictionary<string,CassetteMaterialLayer>();
  public FgCassetteVisuals fg = null;
  public CassetteMaterialLayer(CassetteMaterialFormat format, string channel):base(format.passes,format.depth){
    this.channel = channel;
    this.format = format;
    if(format.hasfg) fg = new FgCassetteVisuals(format);
  }
  public override void onEnable(){
    base.onEnable();
    if(fg!=null) MaterialPipe.addLayer(fg);
  }
  public override void onRemove(){
    base.onRemove();
    if(layers.TryGetValue(channel, out var l) && l==this) layers.Remove(channel);
    if(fg!=null) MaterialPipe.removeLayer(fg);
  }

  //public override RenderTarget2D outtex=>handles[1];
  public override void render(SpriteBatch sb, Camera c){
    passes.setparamvalex("edgecol",format.border.ToVector4());
    passes.setparamvalex("lowcol",format.innerlow.ToVector4());
    passes.setparamvalex("highcol",format.innerhigh.ToVector4());
    passes.setparamvalex("pattern",format.patternvec);
    passes.setparamvalex("stripecutoff",format.stripecutoff);
    passes.setbaseparams();
    base.render(sb,c);
  }

  public override void rasterMats(SpriteBatch sb,Camera c){
    TrySortWilldraw();
    int edepth = format.edepth;
    foreach(OverrideVisualComponent o in willdraw){
      if(o.Entity.Depth<edepth) o.renderMaterial(this,c);
    }
  }
  public override bool drawMaterials => true;
  static VirtualShaderList simplePasses = new VirtualShaderList{
    null,auspicioushelperGFX.LoadShader("cassette/simple")
  };
  static VirtualShaderList vanillaPasses = new VirtualShaderList{
    null, auspicioushelperGFX.LoadShader("cassette/prevanilla"), auspicioushelperGFX.LoadShader("cassette/vanilla")
  };

  public static void LoadFormat(string key, CassetteMaterialFormat format){
    CassetteMaterialLayer other;
    if(layers.TryGetValue(key, out other)){
      if(other.format.gethash() != format.gethash()){
        MaterialPipe.removeLayer(other);
        other = null;
      } 
    }
    if(other == null) other = new CassetteMaterialLayer(format, key);
    layers[key]=other;
    MaterialPipe.addLayer(other);
  }

  [CustomEntity("auspicioushelper/TemplateCassetteColor")]
  public class CustomCassetteLayer:Entity{
    public CustomCassetteLayer(EntityData d, Vector2 o):base(d.Position+o){
      CassetteMaterialFormat f = new();
      f.hasfg = d.Bool("tintActive");
      f.fgdepth = d.Int("activeDepth",-10);
      var l = Util.listparseflat(d.Attr("color","ddd")).Map(Util.hexToColor);
      f.fghigh = l[0];
      f.fglow = l.Count>1? l[1]:l[0].colorMult(0.5f);
      f.border = l[0];
      f.innerhigh = l[0].colorMult(0.5f);
      f.innerlow = l[0].colorMult(0.5f*0.7f);
      f.fgsat = d.Float("fgSaturation",0.5f);

      f.depth = d.Int("inactiveDepth",8999);
      f.passes = d.Bool("simpleStyle",false)?simplePasses:vanillaPasses;
      float angle = MathF.PI*d.Float("patternAngle",0)/180f;
      float scale = MathF.PI/d.Float("patternScale",4);
      f.patternvec.W = d.Float("patternOffset",0)+0.01f;
      f.patternvec.X = scale*MathF.Cos(angle);
      f.patternvec.Y = scale*MathF.Sin(angle); 
      f.stripecutoff = MathF.Cos(MathF.PI*d.Float("patternWeight",0.5f));
      if(d.tryGetStr("borderColor",out var bc))f.border = Util.hexToColor(bc);
      if(d.tryGetStr("innerColor",out var ic)){
        var c = Util.listparseflat(ic).Map(Util.hexToColor);
        f.innerhigh = c[0];
        f.innerlow = c.Count>1? c[1]:c[0].colorMult(0.7f);
      }
      
      foreach(var ch in Util.listparseflat(d.Attr("channels"))){
        LoadFormat(ch,f);
      }
    }
    public override void Awake(Scene s){
      base.Awake(s);
    }
  }
}
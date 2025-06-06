



using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class CassetteMaterialLayer:IMaterialLayer{
  public bool independent{get;} = true;
  public bool diddraw {get;set;}
  public bool removeNext {get;set;}
  public bool enabled {get;set;}
  public RenderTarget2D outtex {get; private set;}
  RenderTarget2D mattex;
  RenderTarget2D pretex;
  public float depth {get;set;}
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
    public float depth = 9000;
    public string shader = "__default__";
    public string preshader = "";
    public bool usepreshader = false;
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
          case "time": c.patternvec.Z=float.Parse(pair.Value); break;
          case "phase": c.patternvec.W=float.Parse(pair.Value); break;
          case "depth":c.depth = float.Parse(pair.Value); break;
          case "alphacutoff":c.alphacutoff = float.Parse(pair.Value); break;
          case "stripecutoff":c.stripecutoff = float.Parse(pair.Value); break;
          case "shader": c.shader = pair.Value; break;
          case "preshader": c.preshader = pair.Value; break;
          case "style":
            switch(pair.Value){
              case "vanilla": 
                c.shader = "vanilla";
                c.preshader = "prevanilla";
              break;
              case "simple": default:
                c.shader = "simple";
                c.preshader = "";
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
      if(c.shader == "__default__"){
        c.shader = "vanilla";
        c.preshader = "prevanilla";
      }
      if(!string.IsNullOrWhiteSpace(c.preshader)) c.usepreshader = true;
      return c;
    }
    public int gethash(){
      return HashCode.Combine(border, innerlow, innerhigh, patternvec, alphacutoff, stripecutoff, depth, HashCode.Combine(
        hasfg, fghigh, fglow, fgdepth
      ));
    }
  }
  public static Dictionary<string, CassetteMaterialLayer> layers = new Dictionary<string,CassetteMaterialLayer>();
  Effect shader;
  Effect preshader;
  public FgCassetteVisuals fg = null;
  public CassetteMaterialLayer(CassetteMaterialFormat format, string channel){
    this.channel = channel;
    this.depth = format.depth;
    this.format = format;
    outtex = new RenderTarget2D(Engine.Instance.GraphicsDevice, 320, 180);
    mattex = new RenderTarget2D(Engine.Instance.GraphicsDevice, 320, 180);
    resources.enable();
    switch(format.shader){
      case"simple": shader = simpleShader; break;
      case "vanilla": shader = vanillaShader; break;
      default: shader = auspicioushelperGFX.LoadEffect("cassette/"+format.shader)??simpleShader; break;
    }
    if(format.usepreshader){
      pretex = new RenderTarget2D(Engine.Instance.GraphicsDevice, 320, 180);
      switch(format.preshader){
        case "prevanilla": preshader = vanillaPreshader; break;
        default: preshader = auspicioushelperGFX.LoadEffect("cassette/"+format.preshader);break;
      }
    }
    if(format.hasfg) fg = new FgCassetteVisuals(format);
  }
  List<Entity> items = new List<Entity>();
  bool dirty;
  public void onEnable(){
    if(fg!=null) MaterialPipe.addLayer(fg);
  }
  public void render(Camera c, SpriteBatch sb, RenderTarget2D back){
    if(dirty){
      items.Sort(EntityList.CompareDepth);
      dirty = false;
    }
    EffectParameterCollection prm = shader.Parameters;
    prm["edgecol"]?.SetValue(format.border.ToVector4());
    prm["lowcol"]?.SetValue(format.innerlow.ToVector4());
    prm["highcol"]?.SetValue(format.innerhigh.ToVector4());
    prm["pattern"]?.SetValue(format.patternvec);
    prm["cpos"]?.SetValue(c.Position);
    prm["time"]?.SetValue((Engine.Scene as Level)?.TimeActive??0);
    prm["stripecutoff"]?.SetValue(format.stripecutoff);
    MaterialPipe.gd.SetRenderTarget(mattex);
    MaterialPipe.gd.Clear(Color.Transparent);
    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, c.Matrix);
    if(ChannelState.readChannel(channel) == 0)foreach(Entity e in items){
      if(e.Scene != null && e.Depth<=depth) e.Render();
      //DebugConsole.Write($"{e.Position} {e}");
    }
    foreach(IMaterialObject e in trying){
      e.renderMaterial(this, sb, c);
    }
    sb.End();
    MaterialPipe.gd.Textures[1] = mattex;
    if(format.usepreshader && preshader!=null){
      MaterialPipe.gd.SetRenderTarget(pretex);
      MaterialPipe.gd.Clear(Color.Transparent);
      sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, preshader, Matrix.Identity);
      sb.Draw(mattex,Vector2.Zero,Color.White);
      sb.End();
      MaterialPipe.gd.Textures[2] = pretex;
    }
    MaterialPipe.gd.SetRenderTarget(outtex);
    MaterialPipe.gd.Clear(Color.Transparent);
    sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, shader, Matrix.Identity);
    sb.Draw(mattex,Vector2.Zero,Color.White);
    sb.End();
    diddraw = true;
  }
  bool lastdn = false;
  public bool checkdo(){
    bool drawnormal = ChannelState.readChannel(channel) == 0;
    if(drawnormal && !lastdn) Util.RemovePred(trying, a=>a is TemplateCassetteBlock);
    lastdn = drawnormal;
    return drawnormal || trying.Count>0;
  }
  public void onRemove(){
    trying.Clear();
    if(layers.TryGetValue(channel, out var l) && l==this) layers.Remove(channel);
    if(fg!=null)MaterialPipe.removeLayer(fg);
  }
  public void dump(List<Entity> l){
    foreach(Entity e in l){
      items.Add(e);
    }
    dirty=true;
  }
  public HashSet<IMaterialObject> trying = new HashSet<IMaterialObject>();
  public void addTrying(IMaterialObject o){
    trying.Add(o);
  }
  public void removeTrying(IMaterialObject o){
    trying.Remove(o);
  }
  
  static Effect simpleShader;
  static Effect vanillaShader;
  static Effect vanillaPreshader;
  static HookManager resources = new HookManager(()=>{
    //totally a hook (this is a good api)
    simpleShader = auspicioushelperGFX.LoadEffect("cassette/simple");
    vanillaShader = auspicioushelperGFX.LoadEffect("cassette/vanilla");
    vanillaPreshader = auspicioushelperGFX.LoadEffect("cassette/prevanilla");
  },bool ()=>{
    foreach(var pair in layers){
      List<Entity> keep = new List<Entity>();
      foreach(Entity e in pair.Value.items){
        if(e.Scene!=null)keep.Add(e);
      }
      pair.Value.items = keep;
      pair.Value.trying.Clear();
      pair.Value.fg?.Clean();
    }
    return false;
  },auspicioushelperModule.OnReset);
}
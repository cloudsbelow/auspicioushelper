



using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.auspicioushelper;
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
    public float depth = 9000;
    public VirtualShaderList passes;
    public CassetteMaterialFormat(){}

    public static CassetteMaterialFormat fromDict(Dictionary<string,string> dict){
      CassetteMaterialFormat c = new CassetteMaterialFormat();
      resources.enable();
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
  public static Dictionary<string, CassetteMaterialLayer> layers = new Dictionary<string,CassetteMaterialLayer>();
  public FgCassetteVisuals fg = null;
  public CassetteMaterialLayer(CassetteMaterialFormat format, string channel):base(format.passes,format.depth){
    this.channel = channel;
    this.format = format;
    resources.enable();
    if(format.hasfg) fg = new FgCassetteVisuals(format);
  }
  List<Entity> items = new List<Entity>();
  bool dirty;
  public override void onEnable(){
    base.onEnable();
    if(fg!=null) MaterialPipe.addLayer(fg);
  }
  public override void onRemove(){
    base.onRemove();
    if(layers.TryGetValue(channel, out var l) && l==this) layers.Remove(channel);
    if(fg!=null) MaterialPipe.removeLayer(fg);
    trying.Clear();
    items.Clear();
  }

  //public override RenderTarget2D outtex=>handles[1];
  public override void render(SpriteBatch sb, Camera c){
    if(dirty){
      items.Sort(EntityList.CompareDepth);
      dirty = false;
    }
    
    passes.setparamvalex("edgecol",format.border.ToVector4());
    passes.setparamvalex("lowcol",format.innerlow.ToVector4());
    passes.setparamvalex("highcol",format.innerhigh.ToVector4());
    passes.setparamvalex("pattern",format.patternvec);
    passes.setparamvalex("stripecutoff",format.stripecutoff);
    passes.setbaseparams();
    base.render(sb,c);
  }
  bool lastdn = false;
  public override bool drawMaterials => true;
  public static Dictionary<Type,Action<Entity>> stupididiotdumbpompusthings = new();
  public override void rasterMats(SpriteBatch sb, Camera c) {
    //DebugConsole.Write($"rendering {items.Count} {Engine.Instance.scene.Tracker.GetEntities<LayerMarkingEntity>().Count}",info.markingEnt.Scene);
    if(ChannelState.readChannel(channel) == 0)foreach(Entity e in items){
      if(e.Scene != null && e.Depth<=info.depth){
        if(stupididiotdumbpompusthings.TryGetValue(e.GetType(),out var fn))fn(e);
        else e.Render();
      }
    }
    foreach(IMaterialObject e in trying){
      e.renderMaterial(this, c);
    }
  }
  public override bool checkdo(){
    bool drawnormal = ChannelState.readChannel(channel) == 0;
    if(drawnormal && !lastdn) Util.RemovePred(trying, a=>a is TemplateCassetteBlock);
    lastdn = drawnormal;
    return drawnormal || trying.Count>0;
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
  
  static VirtualShaderList simplePasses;
  static VirtualShaderList vanillaPasses;
  static HookManager resources = new HookManager(()=>{
    //totally a hook (this is a good api)
    simplePasses = new VirtualShaderList{
      null,auspicioushelperGFX.LoadShader("cassette/simple")
    };
    vanillaPasses = new VirtualShaderList{
      null, auspicioushelperGFX.LoadShader("cassette/prevanilla"), auspicioushelperGFX.LoadShader("cassette/vanilla")
    };
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



using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class UserLayer:BasicMaterialLayer, IMaterialLayer, IFadingLayer, ISettableDepth, CachedUserMaterial{
  List<MaterialResource> resources = new();
  public string identifier {get;set;}
  public float depth {set{
    info.depth=value;
    if(info.markingEnt!=null)info.markingEnt.Depth = (int)info.depth;
  }}
  List<Action> chset = new();
  Action getparamsetter(string key, string channel, float mult){
    return ()=>{
      float val = mult*(float)ChannelState.readChannel(channel);
      passes.setparamvalex(key,val);
    };
  }
  static Regex chr = new Regex(@"@(\w*(?:\[[^]]+\])?)((?:[*\/][\d\.]+)?)", RegexOptions.Compiled);
  void setparamval(string key, string val){
    if(string.IsNullOrEmpty(key)) return;
    switch(val.ToLower()){
      case "true": passes.setparamvalex(key, true); break;
      case "false": passes.setparamvalex(key, false); break;
      default: switch(val[0]){
        case '#': passes.setparamvalex(key, Util.toArray(Util.hexToColor(val).ToVector4())); break;
        case '{': case '[': 
          if(!val.Contains("@")){
            passes.setparamvalex(key, Util.csparseflat(Util.stripEnclosure(val))); 
            break;
          }
          var arr = Util.listparseflat(val, true,false);
          float[] values = new float[arr.Count];
          for(int i=0; i<arr.Count; i++){
            if(arr[i].StartsWith("@")){
              string chstr = arr[i].Substring(1);
              int index = i;
              chset.Add(()=>values[index]=(float)ChannelState._readChannel(chstr));
            } else values[i] = float.Parse(arr[i]);
          }
          chset.Add(()=>passes.setparamvalex(key,values));
        break;
        case '@': 
          var match = chr.Match(val);
          if (!match.Success){
            DebugConsole.WriteFailure($"Error parsing channel string {val}"); break;
          }
          string ch = match.Groups[1].Value;
          float mult = 1;
          if(!string.IsNullOrWhiteSpace(match.Groups[2].Value)){
            float.TryParse(match.Groups[2].Value.Substring(1), out mult);
            if(match.Groups[2].Value[0] == '/') mult = 1/mult;
          }
          chset.Add(getparamsetter(key, ch, mult));
          break;
        case >='0' and <='9': case '.':
          if(val.Contains('.')){
            float.TryParse(val, out var f);
            passes.setparamvalex(key,f);
          } else {
            int.TryParse(val, out var i);
            passes.setparamvalex(key,i);
          }
          break;
        default:
          DebugConsole.WriteFailure($"Don't know how to parse {val}"); break;
      }break;
    }
  }
  List<Tuple<int,ITexture>> textures = new();
  SamplerList samplers = new();

  static VirtualShaderList getEffects(string s){
    VirtualShaderList list = new();
    foreach(string p in Util.listparseflat(s,true,true)){
      if(string.IsNullOrWhiteSpace(p)||p=="null") list.Add(null);
      else list.Add(auspicioushelperGFX.LoadExternShader(p));
    }
    return list;
  }
  internal static UserLayer make(EntityData d){
    LayerFormat l = new LayerFormat{
      depth = d.Float("depth",-2),
      quadfirst = d.Bool("quadFirst", false),
      alwaysRender = d.Bool("always", true),
      drawInScene = d.Bool("drawInScene",true),
    };
    return new UserLayer(d.Attr("textures"),d.Attr("params",""),getEffects(d.Attr("passes")),l);
  }  
  internal static UserLayer make(BinaryPacker.Element d){
    LayerFormat l = new LayerFormat{
      depth = d.AttrInt("renderOrder"),
      quadfirst = d.AttrBool("quadFirst"),
      alwaysRender = d.AttrBool("alwaysRender"),
      drawInScene = false,
    };
    return new UserLayer(d.Attr("textures"),d.Attr("params",""),getEffects(d.Attr("passes")),l){backdropLayer=true};
  }
  public IFadingLayer.FadeTypes fadeTypeIn {get;set;} = IFadingLayer.FadeTypes.Linear;
  public IFadingLayer.FadeTypes fadeTypeOut {get;set;} = IFadingLayer.FadeTypes.Linear;
  int swapch=0;
  bool backdropLayer=false;
  bool IMaterialLayer.autoManageRemoval => !backdropLayer;
  public UserLayer(string texstring, string paramstring, VirtualShaderList l, LayerFormat f):base(l,f){
    SetupTextures(texstring);
    for(int i=0; i<swapch; i++) handles.Add(new RenderTargetPool.RenderTargetHandle(false));
    try{
      foreach(var pair in Util.kvparseflat(paramstring)){
        setparamval(pair.Key,pair.Value);
      }
    } catch(Exception err){
      DebugConsole.WriteFailure($"error setting shader params: {err}");
    }
  }
  void SetupTextures(string texstring){
    foreach(var p in Util.kvparseflat(texstring,true,true)){
      if(p.Key=="0")DebugConsole.Write($"Warning: Binding to texture 0 only affects the first quad in 'drawquad'. Use 00 to hide this message");
      if(p.Key.Trim().StartsWith('s')){
        if(!int.TryParse(p.Key.Trim().Substring(1), out var slot)||slot>15||slot<0) DebugConsole.WriteFailure($"Invalid sampler slot {p.Key}");
        samplers.Add(slot, p.Value);
        continue;
      } 
      ITexture texture = null;
      if(!int.TryParse(p.Key.Trim(),out var idx)||idx>15||idx<0) DebugConsole.WriteFailure($"Invalid texture slot {p.Key}");
      else if(string.IsNullOrWhiteSpace(p.Value)) DebugConsole.WriteFailure($"Invalid texutre resource at {idx}");
      else switch(p.Value.ToLower()){
        case "lv": case "level":
          info.independent = layerformat.independent=false;
          textures.Add(new(idx,texture = ITexture.bgWrapper));
          break;
        case "bg": case "background":
          info.usesbg = layerformat.useBg=true;
          textures.Add(new(idx,texture = ITexture.bgWrapper));
          break;
        case "gp": case "gameplay":
          info.independent = layerformat.independent=false;
          textures.Add(new(idx,texture = ITexture.gpWrapper));
          break;
        case "prev": case "last":
          textures.Add(new(idx,texture = new ITexture.UserLayerWrapper(this)));
          swapch = 1;
          break;
        default:
          switch(p.Value[0]){
            case '/': //photo
              textures.Add(new(idx,texture = new ITexture.ImageWrapper(p.Value.Substring(1))));
              break;
            case '$': //child layer
              textures.Add(new(idx,texture = new ITexture.UserLayerWrapper(p.Value.Substring(1))));
              break;
            case '%': //backdrop
              var backdrop = BackdropCapturer.Get(p.Value.Substring(1).Trim());
              textures.Add(new(idx,texture = new ITexture.LayerWrapper(backdrop)));
              resources.Add(backdrop.resource);
              break;
            default:
              DebugConsole.WriteFailure($"Unknown resource {p.Value} on slot {idx}");
              break;
          }
          break;
      }
      if(idx == 0) overrideFirstResource = texture;
    }
  }
  public override void render(SpriteBatch sb, Camera c) {
    foreach(var a in chset)a(); 
    foreach(var t in textures) MaterialPipe.gd.Textures[t.Item1] = t.Item2;
    using(samplers.Apply(MaterialPipe.gd))base.render(sb, c);
    if(swapch>0){
      int i=passes.Count;
      var o = handles[passes.Count-1];
      for(; i<handles.Count; i++){
        handles[i-1]=handles[i];
      }
      handles[handles.Count-1]= o;
    }
  }
  HashSet<OverrideVisualComponent> waiting = null;
  //[Import.SpeedrunToolIop.Static]
  static HashSet<UserLayer> hasWaiting = new();
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnReset)]
  public static void ClearWaiting(){
    foreach(var l in hasWaiting) l.waiting = null;
    hasWaiting.Clear();
  }
  public override void AddC(OverrideVisualComponent c) {
    if(!info.enabled){
      if(waiting == null) {
        DebugConsole.Write($"Non-enabled layer {this} added to. Waiting.");
        waiting = new();
        hasWaiting.Add(this);
      }
      waiting.Add(c);
    }
    base.AddC(c);
  }
  public override void RemoveC(OverrideVisualComponent c) {
    waiting?.Remove(c);
    base.RemoveC(c);
  }
  public override void onEnable() {
    base.onEnable();
    foreach(var r in resources) r.Use();
    if(waiting!=null){
      if(!info.enabled) throw new Exception(" literally How");
      foreach(var o in waiting) AddC(o);
      waiting = null;
      hasWaiting.Remove(this);
      DebugConsole.Write($"Layer with waiting items {this} corrected successfully");
    }
  }
  public override void onRemove() {
    base.onRemove();
    foreach(var r in resources) r.Unuse();
  }
  public override string ToString()=>$"UserLayer.{(identifier.Length>40?identifier.Substring(0,40)+"...":identifier)} ({this.GetHashCode()})";
}

public class MaterialResource{
  int users;
  public bool inUse=>users>0;
  public Action<bool> a;
  public MaterialResource(Action<bool> OnChange){
    a=OnChange;
  }
  public void Use(){
    if(users++==0)a(true);
  }
  public void Unuse(){
    if(--users==0)a(false);
  }
}
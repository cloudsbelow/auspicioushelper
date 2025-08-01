


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class UserLayer:BasicMaterialLayer, IMaterialLayer, IFadingLayer, ISettableDepth, CachedUserMaterial{
  public string identifier {get;set;}
  public float depth {set{
    info.depth=value;
    if(info.markingEnt!=null)info.markingEnt.Depth = (int)info.depth;
  }}
  List<Action> chset = new();
  Action getparamsetter(string key, string channel, float mult){
    return ()=>{
      float val = mult*(float)ChannelState.readChannel(channel);
      //DebugConsole.Write($"{key} {val}");
      //if(Calc.NextFloat(Calc.Random)<0.1f)passes.setparamvalex(key,val+0.0001f);
      //else passes.setparamvalex(key,val);
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
        case '{': case '[': passes.setparamvalex(key, Util.csparseflat(Util.stripEnclosure(val))); break;
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
  List<Tuple<int,ITexture>> textures;
  internal static UserLayer make(EntityData d){
    VirtualShaderList list = new();
    foreach(string p in Util.listparseflat(d.Attr("passes"),true,true)){
      if(string.IsNullOrWhiteSpace(p)||p=="null") list.Add(null);
      else list.Add(auspicioushelperGFX.LoadExternShader(p));
    }
    List<Tuple<int,ITexture>> textures = new();
    LayerFormat l = new LayerFormat{
      //useBg = d.Bool("usebg",false),
      //independent = d.Bool("independent",true),
      depth = d.Float("depth",-2),
      quadfirst = d.Bool("quadFirst", false),
      alwaysRender = d.Bool("always", true),
      drawInScene = d.Bool("drawInScene",true),
    };
    foreach(var p in Util.kvparseflat(d.Attr("textures"),true,true)){
      if(p.Key=="0")DebugConsole.Write($"Warning: Binding to texture 0 is strange. Use 00 to hide this message");
      if(!int.TryParse(p.Key.Trim(),out var idx)||idx>15||idx<0) DebugConsole.WriteFailure($"Invalid texture slot {p.Key}");
      else if(string.IsNullOrWhiteSpace(p.Value)) DebugConsole.WriteFailure($"Invalid texutre resource at {idx}"); 
      else switch(p.Value.ToLower()){
        case "bg": case "background": 
          l.useBg=true;
          textures.Add(new(idx,ITexture.bgWrapper));
          break;
        case "gp": case "gameplay":
          l.independent=false;
          textures.Add(new(idx,ITexture.gpWrapper));
          break;
        default:
          switch(p.Value[0]){
            case '/': //photo
              break;
            case '$': //child layer
              textures.Add(new(idx, new ITexture.UserLayerWrapper(p.Value.Substring(1))));
              break;
          }
          break;
      }
    }
    return new UserLayer(d,list,l){textures=textures};
  }  
  public IFadingLayer.FadeTypes fadeTypeIn {get;set;} = IFadingLayer.FadeTypes.Linear;
  public IFadingLayer.FadeTypes fadeTypeOut {get;set;} = IFadingLayer.FadeTypes.Linear;
  public UserLayer(EntityData d, VirtualShaderList l, LayerFormat f):base(l,f){
    try{
      foreach(var pair in Util.kvparseflat(d.Attr("params",""))){
        setparamval(pair.Key,pair.Value);
      }
    } catch(Exception err){
      DebugConsole.WriteFailure($"error setting shader params: {err}");
    }
  }
  public override void render(SpriteBatch sb, Camera c) {
    foreach(var a in chset)a(); 
    foreach(var t in textures) MaterialPipe.gd.Textures[t.Item1] = t.Item2;
    base.render(sb, c);
  }
}
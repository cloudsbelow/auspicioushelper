

using Monocle;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.auspicioushelper;

public interface IFadingLayer{
  enum FadeTypes {
    Always, Never, Linear, Cos, Sqrt
  }
  FadeTypes fadeTypeIn {get;set;}
  FadeTypes fadeTypeOut {get;set;}
  void setFade(FadeTypes i, FadeTypes o){
    fadeTypeIn=i; fadeTypeOut=o;
  }
  static float getFade(FadeTypes type, float fac){
    return type switch{
      FadeTypes.Always=>1,
      FadeTypes.Never=>0,
      FadeTypes.Linear=>fac,
      FadeTypes.Cos=>(1-MathF.Cos(fac*3.141592f))/2,
      FadeTypes.Sqrt=>MathF.Sqrt(fac),
      _=>1
    };
  }
  float getTransAlpha(bool leaving, float camAt){
    return leaving?getFade(fadeTypeOut,1-camAt):getFade(fadeTypeIn,camAt);
  }
  static FadeTypes fromString(string s){
    return s switch{
      "Never"=>IFadingLayer.FadeTypes.Never,
      "Linear"=>IFadingLayer.FadeTypes.Linear,
      "Cosine"=>IFadingLayer.FadeTypes.Cos,
      "Always"=>IFadingLayer.FadeTypes.Always,
      "Sqrt"=>IFadingLayer.FadeTypes.Sqrt,
      _=>IFadingLayer.FadeTypes.Always,
    };
  }
}
public interface ISettableDepth{
  float depth {set;}
}
public interface IDeclareLayers{
  void declareLayers();
}

[CustomEntity("auspicioushelper/MaterialController")]
[Tracked]
internal class MaterialController:Entity, IDeclareLayers{
  static Dictionary<string, IMaterialLayer> loadedMats = new Dictionary<string, IMaterialLayer>();
  public static IMaterialLayer getLayer(string ident)=>loadedMats.TryGetValue(ident, out var layer)?layer:null;
  static MaterialController(){
    auspicioushelperModule.OnEnterMap.enroll(new ScheduledAction(()=>{
      loadedMats.Clear(); return false;
    }, "clear material cache"));
  }
  EntityData e;
  internal string identifier;
  public IMaterialLayer load(EntityData e){
    string path=e.Attr("path","");
    identifier=e.Attr("identifier");
    if(string.IsNullOrWhiteSpace(identifier)) identifier = path+"###"+e.Attr("params","");
    bool reload = e.Bool("reload",false);
    if(path.Length == 0)return null;
    IMaterialLayer l = null;
    if(reload && loadedMats.TryGetValue(identifier, out l)){
      if(l.enabled) MaterialPipe.removeLayer(l);
      loadedMats.Remove(identifier);
    }
    DebugConsole.Write($"Loading material shader from {path} as {identifier}");
    if(!loadedMats.ContainsKey(identifier)){
      if(identifier == "auspicioushelper/ChannelMatsEN###"){
        l = loadedMats[identifier] = (ChannelBaseEntity.layerA = new ChannelMaterialsA());
      } else {
        l = UserLayer.make(e);
        if(l!=null){
          loadedMats[identifier]=l;
        }
      }
    }
    if(loadedMats.TryGetValue(identifier, out var layer)){
      MaterialPipe.addLayer(layer);
      return layer;
    }
    return null;
  }
  public MaterialController(EntityData e,Vector2 v):base(new Vector2(0,0)){
    this.e=e;
    declareLayers();
  }
  public void declareLayers(){
    if(!string.IsNullOrEmpty(identifier) && loadedMats.TryGetValue(identifier, out var l)){
      MaterialPipe.addLayer(l);
    }else l = load(e);

    if(l!=null){
      if(l is IFadingLayer u){
        u.setFade(IFadingLayer.fromString(e.Attr("Fade_in","")), IFadingLayer.fromString(e.Attr("fadeOut","")));
      }
      if(l is ISettableDepth d){
        d.depth = e.Int("depth",0);
        MaterialPipe.dirty = true;
      }
    }
  }
  public override void Added(Scene scene){
    base.Added(scene);
  }
}
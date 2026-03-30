

using Monocle;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using System;
using Celeste.Mod.Backdrops;
using MonoMod.Cil;
using Microsoft.Xna.Framework.Graphics;

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
public interface CachedUserMaterial:IMaterialLayer{
  string identifier {get;set;}
}

[CustomEntity("auspicioushelper/MaterialController")]
[Tracked]
internal class MaterialController:Entity{
  static Dictionary<string, CachedUserMaterial> loadedMats = new();
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnReload)]
  static void Reload(){
    loadedMats.Clear();
    if((Engine.Instance.scene as LevelLoader)?.Level is {} l){
      foreach(Backdrop b in l.Background.Backdrops) if(b is MaterialBackdrop c) c.Readd();
      foreach(Backdrop b in l.Foreground.Backdrops) if(b is MaterialBackdrop c) c.Readd();
    }
  }
  public static IMaterialLayer getLayer(string ident)=>loadedMats.TryGetValue(ident, out var layer)?layer:null;
  EntityData e;
  internal string identifier;
  public CachedUserMaterial load(EntityData e){
    string path=e.Attr("path","")+e.Attr("passes","");
    identifier=e.Attr("identifier");
    if(string.IsNullOrWhiteSpace(identifier)){
      identifier = path+"###"+e.Attr("params","");
      if(!string.IsNullOrWhiteSpace(e.Attr("textures",""))) identifier+="###"+e.Attr("textures");
    }
    bool reload = e.Bool("reload",false);
    if(path.Length == 0) return null;
    if(reload && loadedMats.TryGetValue(identifier, out var l)){
      if(l.enabled) MaterialPipe.removeLayer(l);
      loadedMats.Remove(identifier);
    }
    DebugConsole.Write($"Loading material shader from {path} as {identifier}");
    if(!loadedMats.ContainsKey(identifier)){
      if(identifier == "auspicioushelper/ChannelMatsEN###"){
        l = loadedMats[identifier] = new ChannelMaterialsA();
      } else {
        l = UserLayer.make(e);
        if(l!=null){
          loadedMats[identifier]=l;
        }
      }
      l.identifier = identifier;
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
  public static void fixmat(CachedUserMaterial c){
    loadedMats[c.identifier] = c;
  }
  public override void Added(Scene scene){
    base.Added(scene);
  }
  [CustomBackdrop("auspicioushelper/MaterialEffect")]
  public class MaterialBackdrop:Backdrop{
    string identifier;
    CachedUserMaterial u;
    bool deferred=false; //automatically set
    BlendState blend = BlendState.AlphaBlend;
    bool dontPaste = false;
    public void Readd(){
      loadedMats[identifier]=u;
      DebugConsole.Write("Reenabling",identifier);
    }
    static int autocountcount = 0;
    float extraTimer=0;
    float extraTime;
    float alpha=1;
    public MaterialBackdrop(BinaryPacker.Element e){
      identifier=e.Attr("identifier");
      bool noIdent=false;
      if(string.IsNullOrWhiteSpace(identifier)){
        identifier = e.Attr("passes","")+"###"+(++autocountcount);
        noIdent=true;
      }
      if(e.Attr("passes","").Length == 0)return;
      if(!noIdent && loadedMats.TryGetValue(identifier, out var l)){
        if(l.enabled) MaterialPipe.removeLayer(l);
        loadedMats.Remove(identifier);
      }
      DebugConsole.Write($"Loading material backdrop from {e.Attr("passes","")} as {identifier}");

      UserLayer layer = UserLayer.make(e,identifier);
      if(!noIdent) loadedMats[identifier]=layer;
      u=layer;
      deferred = u is UserLayer{DeferLayerDraw:true};
      UseSpritebatch=false;
      blend = e.Attr("blendMode","") switch {
        "Auto"=>null,
        "AlphaBlend"=>BlendState.AlphaBlend,
        "Addative"=>BlendState.Additive,
        "Multiply"=>CustomBlendstates.multiply,
        "Darken"=>CustomBlendstates.darken,
        "Lighten"=>CustomBlendstates.lighten,
        "Subtract"=>CustomBlendstates.subtract,
        "Max"=>CustomBlendstates.max,
        "Min"=>CustomBlendstates.min,
        _=>BlendState.AlphaBlend
      };
      dontPaste = e.Attr("blendMode","").Trim()=="Discard";
      layer.clearColor = Util.hexToColor(e.Attr("clearColor","#0000"));

      layer.fadeTypeIn = IFadingLayer.fromString(e.Attr("fadeIn","Always"));
      layer.fadeTypeOut = IFadingLayer.fromString(e.Attr("fadeOut","Always"));
      extraTime = e.AttrFloat("extraFadeOutTime",0);

      DebugConsole.Write("Params",layer.fadeTypeIn,layer.fadeTypeOut,extraTime);
    }
    public override void Update(Scene scene) {
      Level l = scene as Level;
      bool nextVisible = IsVisible(l);
      if(l.Transitioning || (Visible && extraTimer>0)){
        if(Visible || nextVisible){
          if(u is not IFadingLayer f){}
          else if(nextVisible){ //fade in
            extraTimer=0;
            if(!Visible) alpha=0;
            alpha = Math.Max(alpha,f.getTransAlpha(false,MaterialPipe.camAt));
          } else { //fade out
            if(l.Transitioning) extraTimer = extraTime;
            float time = MaterialPipe.camAt*MaterialPipe.NextTransitionDuration+(extraTime-extraTimer);
            float denom = MaterialPipe.NextTransitionDuration+extraTime;
            alpha = f.getTransAlpha(true,time/denom);
            extraTimer-=Engine.DeltaTime;
          }
          Visible=true;
        }
      } else {
        Visible = nextVisible;
        alpha=1;
      }
      if(u==null) return;
      if(Visible && !u.enabled){
        MaterialPipe.addLayer(u);
      }
      if(!Visible && u.enabled){
        MaterialPipe.removeLayer(u);
      }
    }
    public override void Render(Scene scene) {
      if(u==null) return;
      if(Visible && !u.enabled){
        DebugConsole.Write($"Backdrop {u.identifier} not enabled! Enabling next frame.", MaterialPipe.renderingLevel);
        if(MaterialPipe.renderingLevel == null)Update(scene);
        else MaterialPipe.addAfterAction(()=>Update(scene));
        return;
      }
      if(deferred){
        u.render();
        MaterialPipe.gd.SetRenderTarget(GameplayBuffers.Level);
      }
      if(!dontPaste){
        BackdropCapturer.currentRenderer.StartSpritebatch(blend??BackdropCapturer.currentBlendstate);
        Draw.SpriteBatch.Draw(u.outtex,Vector2.Zero,Color.White*alpha);
        BackdropCapturer.currentRenderer.EndSpritebatch();
      }
    }

    static class CustomBlendstates{
      public static readonly BlendState multiply = new BlendState {
        ColorBlendFunction = BlendFunction.Add,
        AlphaBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.Zero,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
      };
      public static readonly BlendState darken = new BlendState {
        ColorBlendFunction = BlendFunction.Add,
        AlphaBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
      };
      public static readonly BlendState lighten = new BlendState{
        ColorBlendFunction = BlendFunction.Add,
        AlphaBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
      };
      public static readonly BlendState subtract = new BlendState{
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        AlphaBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
      };
      public static readonly BlendState max = new BlendState{
        ColorBlendFunction = BlendFunction.Max,
        AlphaBlendFunction = BlendFunction.Max,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
      };
      public static readonly BlendState min = new BlendState{
        ColorBlendFunction = BlendFunction.Min,
        AlphaBlendFunction = BlendFunction.Min,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
      };
    }
  }
}
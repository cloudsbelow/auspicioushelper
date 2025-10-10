


using Monocle;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Celeste.Mod;
using System;

namespace Celeste.Mod.auspicioushelper;


public interface IMaterialLayer{
  Entity markingEntity {get;set;}
  bool enabled {get;set;}
  float depth {get;}
  RenderTarget2D outtex {get;}
  bool independent {get;}
  bool diddraw {get;set;}
  float alpha {get=>1;}
  bool drawInScene=>true;
  bool autoManageRemoval=>true;
  float transalpha(bool leaving, float camAt){
    //DebugConsole.Write($"Roomchange: {leaving} {camAt}");
    return (this is IFadingLayer f)?f.getTransAlpha(leaving,camAt):1;
  }
  bool usesbg {get;}
  void render(SpriteBatch sb, Camera c);
  void render()=>render(Draw.SpriteBatch,MaterialPipe.camera);
  void paste(){
    Color c = Color.White*alpha*MaterialPipe.GetTransitionAlpha(this);
    if(independent){
      if(!diddraw||!drawInScene) return;
      Draw.SpriteBatch.Draw(outtex, Vector2.Zero+MaterialPipe.camera.Position,c);
    } else {
      if(!checkdo()) return;
      Draw.SpriteBatch.End();
      render();
      MaterialPipe.continueDefault();
      if(drawInScene)Draw.SpriteBatch.Draw(outtex, Vector2.Zero+MaterialPipe.camera.Position,c);
    }
  }
  bool checkdo();
  void onRemove(){}
  void onEnable(){}
  void addEnt(OverrideVisualComponent o){}
  void removeEnt(OverrideVisualComponent o){}
}

[Tracked]
internal class LayerMarkingEntity:Entity{
  IMaterialLayer layer;
  public LayerMarkingEntity(IMaterialLayer layer):base(Vector2.Zero){
    AddTag(Tags.Global);
    this.layer=layer;
    layer.markingEntity=this;
    Depth = (int)layer.depth;
  }
  public override void Added(Scene scene) {
    base.Added(scene);
  }
  public override void Removed(Scene scene) {
    if(layer.enabled && layer.markingEntity == this){
      DebugConsole.Write($"Layer is still active! is it ok {layer}!");
    }
    base.Removed(scene);
  }
  public override void SceneEnd(Scene scene) {
    if(layer.enabled && layer.markingEntity == this){
      DebugConsole.Write($"Layer is still active! is it ok {layer}!");
    }
    base.SceneEnd(scene);
  }
  
  public override void Render() {
    base.Render();
    if(layer.enabled) layer.paste();
  }
}



public class MaterialLayerInfo{
  public bool enabled;
  public bool independent=true;
  public bool usesbg=false;
  public bool diddraw;
  public float depth;
  public Entity markingEnt;
  public MaterialLayerInfo(bool independent, float depth, bool usebg = false){
    this.independent=independent; this.depth=depth; usesbg=usebg;
  }
}
public interface IMaterialLayerSimple:IMaterialLayer{
  MaterialLayerInfo info {get;}
  bool IMaterialLayer.enabled {get=>info.enabled; set=>info.enabled=value;}
  bool IMaterialLayer.independent{get=>info.independent;}
  bool IMaterialLayer.diddraw {get=>info.diddraw; set=>info.diddraw=value;}
  float IMaterialLayer.depth {get=>info.depth;}
  bool IMaterialLayer.usesbg {get=>info.usesbg;}
  Entity IMaterialLayer.markingEntity {get=>info.markingEnt; set{
    if(info.markingEnt!=null && value!=null){
      DebugConsole.Write("Setting marking ent while one already exists. Weird.");
      info.markingEnt.RemoveSelf();
    }
    info.markingEnt=value;
  }}
}

public class BasicMaterialLayer:IMaterialLayerSimple, IOverrideVisuals{
  public MaterialLayerInfo info {get;}
  public VirtualShaderList passes;
  public List<RenderTargetPool.RenderTargetHandle> handles=new();
  public LayerFormat layerformat;
  public virtual RenderTarget2D outtex=>handles[handles.Count-1];
  public virtual bool autoManageRemoval=>true;
  public class LayerFormat{
    public float depth;
    public bool independent=true;
    public bool alwaysRender=false;
    public bool quadfirst=false;
    public bool useBg=false;
    public bool drawInScene=true;
  }
  bool IMaterialLayer.drawInScene=>layerformat.drawInScene;
  public BasicMaterialLayer(VirtualShaderList passes, LayerFormat l){
    info = new(l.independent,l.depth,l.useBg);
    layerformat = l;
    this.passes=passes;
    foreach(var pass in passes) handles.Add(new RenderTargetPool.RenderTargetHandle(false));
  }
  public BasicMaterialLayer(VirtualShaderList passes, float depth):this(passes,new LayerFormat{depth=depth}){}
  public virtual void onEnable(){
    foreach(var h in handles)h.Claim();
    DebugConsole.Write($"enabled layer {this}. there are {RenderTargetPool.InUse} rendertargets active.");
  }
  public virtual void onRemove(){
    foreach(var h in handles)h.Free();
  }
  bool dirtyWilldraw=false;
  public List<OverrideVisualComponent> willdraw=new();
  public virtual bool checkdo(){
    return layerformat.quadfirst || layerformat.alwaysRender || willdraw.Count>0;
  }
  public static void StartSb(SpriteBatch sb, Effect e=null, Camera c=null){
    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, e, c?.Matrix??Matrix.Identity);
  }
  public void TrySortWilldraw(){
    if(dirtyWilldraw){
      willdraw.Sort((a,b)=>b._depth.CompareTo(a._depth));
      dirtyWilldraw = false;
    }
  }
  public virtual void rasterMats(SpriteBatch sb,Camera c){
    TrySortWilldraw();
    foreach(var o in willdraw){
      if(o.shouldRemove) removeEnt(o);
      else {
        o.renderMaterial(this,c);
      }
    }
  }
  public virtual bool drawMaterials => layerformat.quadfirst || willdraw.Count!=0;
  public ITexture overrideFirstResource = null;
  public virtual void render(SpriteBatch sb, Camera c){
    if(toRemove.Count>0){
      List<OverrideVisualComponent> nlist = new();
      foreach(var o in willdraw) if(!toRemove.Contains(o))nlist.Add(o);
      toRemove.Clear();
      willdraw=nlist;
    }
    passes.setbaseparams();
    GraphicsDevice gd = MaterialPipe.gd;
    for(int i=0; i<passes.Count; i++){
      gd.SetRenderTarget(handles[i]);
      gd.Clear(Color.Transparent);
      if(i==0){
        if(!drawMaterials) continue;
        StartSb(sb,passes[i],c);
        if(layerformat.quadfirst){
          Vector2 tlc = c.ScreenToCamera(Vector2.Zero);
          if(overrideFirstResource!=null){
            Rectangle dst = Int2.Round(tlc).withWidthHeight(RenderTargetPool.size);
            sb.Draw(overrideFirstResource,dst,Color.White);
          }else sb.Draw(RenderTargetPool.zero, tlc, Color.White);
        }
        rasterMats(sb,c);
        sb.End();
      } else {
        StartSb(sb,passes[i]);
        sb.Draw(handles[i-1],Vector2.Zero,Color.White);
        sb.End();
      }
    }
  }

  HashSet<OverrideVisualComponent> toRemove = new();
  public void addEnt(OverrideVisualComponent o){
    if(toRemove.Remove(o))return;
    if(info.enabled){
      willdraw.Add(o); 
      dirtyWilldraw = true;
    }
  }
  public void removeEnt(OverrideVisualComponent o){
    toRemove.Add(o);
  }
  public void AddC(OverrideVisualComponent c)=>addEnt(c);
  public void RemoveC(OverrideVisualComponent c)=>removeEnt(c);
}

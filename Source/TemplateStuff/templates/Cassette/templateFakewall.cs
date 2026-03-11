


using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;



namespace Celeste.Mod.auspicioushelper;

class FadeMaterialLayer:BasicMaterialLayer,IMaterialLayer{
  public float _alpha = 1;
  public float alpha => _alpha;
  public FadeMaterialLayer(int depth):base([null],depth){}
}

[CustomEntity("auspicioushelper/TemplateFakewall")]
public class TemplateFakewall:TemplateDisappearer, Template.IRegisterEnts{
  bool freeze;
  bool dontOnTransitionInto;
  int ddepth;
  float fadespeed;
  bool persistent = true;
  bool caveMode;
  public TemplateFakewall(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateFakewall(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    freeze = d.Bool("freeze",false);
    dontOnTransitionInto = d.Bool("dontOnTransitionInto");
    ddepth = d.Int("disappear_depth",-13000);
    fadespeed = d.Float("fade_speed",1);
    persistent = d.Bool("persistent",true);
    caveMode = d.Bool("caveMode",false);
  }
  public override void addTo(Scene scene){
    if(auspicioushelperModule.Session.brokenTempaltes.Contains(fullpath) && persistent){
      RemoveSelf();
    } else {
      base.addTo(scene);
      setVisColAct(true,false,!freeze);
    }
  }
  public override void Awake(Scene scene){
    base.Awake(scene);
    if(dontOnTransitionInto){
      Player p = Scene.Tracker.GetEntity<Player>();
      if(p!=null && !p.Dead && hasInside(p)){
        setVisColAct(false,false,false);
        auspicioushelperModule.Session.brokenTempaltes.Add(fullpath);
      }
      destroy(false);
    }
  }
  bool disappearing = false;
  FadeMaterialLayer caveLayer;
  public override void Update(){
    base.Update();
    Player p = Scene.Tracker.GetEntity<Player>();
    if(caveMode){
      if(p!=null && !p.Dead && hasInside(p)){
        if(caveLayer == null){
          caveLayer = new(ddepth);
          foreach(var e in GetChildren<Entity>()){
            OverrideVisualComponent.Get(e).AddToOverride(new(caveLayer,-20000,true,true));
          }
          MaterialPipe.addLayer(caveLayer);
        }
        caveLayer._alpha = Calc.Approach(caveLayer._alpha,0,fadespeed*Engine.DeltaTime);
      } else if(caveLayer!=null){
        caveLayer._alpha = Calc.Approach(caveLayer._alpha,1,fadespeed*Engine.DeltaTime);
        if(caveLayer._alpha == 1){
          foreach(var e in GetChildren<Entity>()){
            OverrideVisualComponent.Get(e).RemoveFromOverride(caveLayer);
          }
          MaterialPipe.removeLayer(caveLayer);
          caveLayer=null;
        }
      }
    } else {
      if(disappearing) return;
      if(p!=null && !p.Dead && hasInside(p)){
        Add(new Coroutine(disappearSequence()));
      }
    }
  }
  public override void RegisterEnts(List<Entity> l) {
    base.RegisterEnts(l);
    if(caveLayer!=null) foreach(Entity e in l){
      OverrideVisualComponent.Get(e).AddToOverride(new(caveLayer, -20000, true,true));
    }
  }
  IEnumerator disappearSequence(){
    disappearing = true;
    Audio.Play("event:/game/general/secret_revealed", Position);
    auspicioushelperModule.Session.brokenTempaltes.Add(fullpath);
    FadeMaterialLayer f = caveLayer = new FadeMaterialLayer(ddepth);
    foreach(var e in GetChildren<Entity>()){
      OverrideVisualComponent.Get(e).AddToOverride(new(f,-20000,true,true));
    }
    MaterialPipe.addLayer(f);
    yield return null;
    while((f._alpha = f._alpha-Engine.DeltaTime*fadespeed*1)>0){
      yield return null;
    }
    MaterialPipe.removeLayer(f);
    destroy(false);
  }
}
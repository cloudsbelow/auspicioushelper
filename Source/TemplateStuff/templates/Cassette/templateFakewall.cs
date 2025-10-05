


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
  List<Entity> todraw;
  public override bool drawMaterials=>true; 
  public FadeMaterialLayer(List<Entity> things, int depth):base([null],depth){
    todraw = things;
    todraw.Sort(EntityList.CompareDepth);
  }
  public override void rasterMats(SpriteBatch sb, Camera c){
    SpriteBatch origsb = Draw.SpriteBatch;
    Draw.SpriteBatch = sb;
    foreach(Entity e in todraw)e.Render();
    Draw.SpriteBatch = origsb;
  }
  public override bool checkdo(){
    return todraw.Count>0;
  }
}



[CustomEntity("auspicioushelper/TemplateFakewall")]
public class TemplateFakewall:TemplateDisappearer{
  bool freeze;
  bool dontOnTransitionInto;
  int ddepth;
  float fadespeed;
  bool persistent = true;
  public TemplateFakewall(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateFakewall(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    freeze = d.Bool("freeze",false);
    dontOnTransitionInto = d.Bool("dontOnTransitionInto");
    ddepth = d.Int("disappear_depth",-13000);
    fadespeed = d.Float("fade_speed",1);
    persistent = d.Bool("persistent",true);
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
  public override void Update(){
    base.Update();
    if(disappearing) return;
    Player p = Scene.Tracker.GetEntity<Player>();
    if(p!=null && !p.Dead && hasInside(p)){
      Add(new Coroutine(disappearSequence()));
    }
  }
  IEnumerator disappearSequence(){
    disappearing = true;
    Audio.Play("event:/game/general/secret_revealed", Position);
    auspicioushelperModule.Session.brokenTempaltes.Add(fullpath);
    List<Entity> c = new();
    AddAllChildren(c);
    setVisCol(false,false);
    FadeMaterialLayer f = new FadeMaterialLayer(c,ddepth);
    MaterialPipe.addLayer(f);
    yield return null;
    while((f._alpha = f._alpha-Engine.DeltaTime*fadespeed*1)>0){
      yield return null;
    }
    MaterialPipe.removeLayer(f);
    destroy(false);
  }
}
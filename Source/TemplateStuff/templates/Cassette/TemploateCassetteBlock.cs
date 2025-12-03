


using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Celeste.Editor;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Microsoft.Build.Utilities;
using System.Diagnostics;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateCassetteBlock")]
[Tracked(true)]
public class TemplateCassetteBlock:TemplateDisappearer, IOverrideVisuals, ITemplateChild{
  
  public string channel{get;set;}
  enum State {
    gone, trying, there
  }
  State there = State.there;
  public List<Entity> todraw=new List<Entity>();
  public bool doBoost;
  public bool doRaise;
  protected override Vector2 virtLoc =>Position+Vector2.UnitY*hoffset;
  float hoffset; 
  bool freeze;
  CassetteMaterialLayer layer = null;
  public TemplateCassetteBlock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateCassetteBlock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    freeze = d.Bool("freeze",false);
    channel = d.Attr("channel","");
    prop = prop&~Propagation.Inside;
    doBoost = d.Bool("do_boost",false);
  }
  public override void addTo(Scene scene) {
    int num = new ChannelTracker(channel, setChVal).AddTo(this).value;
    if(num == 0 && doBoost) hoffset=2;
    base.addTo(scene);
    CassetteMaterialLayer.layers.TryGetValue(channel,out layer);
    if(num==0)setChVal(0);
    setupEnts(GetChildren<Entity>());
  }
  const float fakeshake=0.2f;
  public override Vector2? getShakeVector(float n) {
    float time = fakeshake-n;
    return time switch{
      <0.04f=>Vector2.UnitY*-2,
      <0.07f=>Vector2.UnitY*-1,
      <0.12f=>Vector2.Zero,
      <0.2f=>Vector2.UnitY,
      _=>Vector2.Zero,
    };
  }
  public void tryManifest(){
    Player p = Scene?.Tracker.GetEntity<Player>();
    if(getParentCol() && p!=null && !p.Dead && hasInside(p)){
      p.Position.Y-=4;
      bool inside = hasInside(p);
      p.Position.Y+=4;
      bool flag = p.Dead;
      if(!inside){
        setCollidability(true);
        for(int i=0; i<4; i++){
          if (!p.CollideCheck<Solid>(p.Position - Vector2.UnitY * i)){
            p.Position -= Vector2.UnitY * i;
            flag = true;
            break;
          }
        }
        if(!flag)setCollidability(false);
      }
      if(!flag) return; 
    }
    if(layer!=null) foreach(var c in comps)c.SetStealUse(layer,false,false);
    there = State.there;
    prop|=Propagation.Inside;
    setVisColAct(true,true,true);
  }
  public void setChVal(int val){
    if(val==0){
      if(there == State.there){
        setVisColAct(layer!=null,false,!freeze);
        if(doBoost && hoffset!=2){
          hoffset = 2;
          childRelposSafe();
        }
        if(layer!=null) foreach(var c in comps)c.SetStealUse(layer,true,true);
      }
      there = State.gone;
      prop&=~Propagation.Inside;
    } else {
      if(there == State.gone){
        there = State.trying;
        tryManifest();
      }
      if(doBoost && there == State.there){
        shake(fakeshake);
        ownLiftspeed = -Vector2.UnitY*60;
        hoffset = 0;
        childRelposSafe();
        ownLiftspeed = Vector2.Zero;
      }
    }
  }
  public override void Update(){
    base.Update();
    if(there == State.trying) tryManifest();
  }
  void setupEnts(List<Entity> l){
    int tdepth = TemplateDepth();
    bool ghost = there!=State.there;
    if(layer!=null) foreach(Entity e in l){
      //DebugConsole.Write("Adding entity to cassette", e);
      var c = OverrideVisualComponent.Get(e);
      c.AddToOverride(new(this, -30000, false, true));
      c.AddToOverride(new(layer, -10000+tdepth, ghost,ghost));
      if(layer.fg!=null) c.AddToOverride(new(layer.fg,1000-tdepth, true,true));
    }
  }
  public override void OnNewEnts(List<Entity> l) {
    setupEnts(l);
    base.OnNewEnts(l);
  }
  public HashSet<OverrideVisualComponent> comps = new();
  public void AddC(OverrideVisualComponent c)=>comps.Add(c);
  public void RemoveC(OverrideVisualComponent c)=>comps.Remove(c);
}
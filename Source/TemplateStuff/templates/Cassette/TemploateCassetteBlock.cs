


using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Celeste.Editor;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateCassetteBlock")]
[Tracked(true)]
public class TemplateCassetteBlock:TemplateDisappearer, IMaterialObject, IChannelUser, ITemplateChild{
  
  public string channel{get;set;}
  enum State {
    gone, trying, there
  }
  State there = State.there;
  public List<Entity> todraw=new List<Entity>();
  public bool doBoost;
  public bool doRaise;
  public override Vector2 virtLoc =>Position+Vector2.UnitY*hoffset;
  float hoffset; 
  bool freeze;
  public TemplateCassetteBlock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateCassetteBlock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    freeze = d.Bool("freeze",false);
    channel = d.Attr("channel","");
    prop = prop&~Propagation.Inside;
    doBoost = d.Bool("do_boost",false);
    doRaise = d.Bool("do_raise",false);
  }
  public override void Added(Scene scene){
    base.Added(scene);
    int num = ChannelState.watch(this);
    if(CassetteMaterialLayer.layers.TryGetValue(channel,out var layer) || freeze){
      AddAllChildren(todraw);
      if(layer != null)layer.dump(todraw);
      if(layer?.fg!=null){
        parentChangeStatBypass(-1,0,0);
        if(num!=0) layer.fg.Add(this);
      }
    }
    if(num==0)setChVal(0);
  }
  public override int VisChangeHandler(int n, bool parentVis, bool selfVis) {
    if(!CassetteMaterialLayer.layers.TryGetValue(channel,out var layer) || layer.fg==null){
      return base.VisChangeHandler(n, parentVis,selfVis);
    }
    if(n==1){
      layer.fg.Add(this);
    } else {
      layer.fg.Remove(this);
    }
    return 0;
  }
  public override bool enforceAsVis {get{
    bool inlayer = CassetteMaterialLayer.layers.TryGetValue(channel,out var layer);
    return base.enforceAsVis && (!inlayer || layer.fg==null);
  }}
  public void tryManifest(){
    Player p = Scene?.Tracker.GetEntity<Player>();
    if(there!=State.trying) return;
    bool inLayer = CassetteMaterialLayer.layers.TryGetValue(channel,out var layer);
    if(getParentCol() && p!=null && !p.Dead && hasInside(p)){
      p.Position.Y-=4;
      bool inside = !p.Dead && hasInside(p);
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
        //else p.LiftSpeed = parentLiftspeed+new Vector2(0,-50);
      }
      if(inLayer){
        layer.addTrying(this);
      }
      if(!flag) return;
    }
    if(inLayer)layer.removeTrying(this);
    if(freeze) {
      foreach(Entity e in todraw) e.Active = true;
    }
    there = State.there;
    prop|=Propagation.Inside;
    setVisCol(true,true);
  }
  float bumpTarget = 1;
  IEnumerator bumpUp(){
    float at = 0;
    while(at>-bumpTarget){
      at=Calc.Approach(at,-bumpTarget,Engine.DeltaTime*120);
      hoffset = at;
      ownLiftspeed = Vector2.UnitY*-60;
      childRelposSafe();
      yield return null;
    }
    ownLiftspeed=Vector2.Zero;
    yield return 0.2f;
    while(at<0){
      at=Calc.Approach(at,0,Engine.DeltaTime*30);
      hoffset = at;
      ownLiftspeed = Vector2.UnitY*30;
      childRelposSafe();
      yield return null;
    }
    ownLiftspeed=Vector2.Zero;
  }
  public void setChVal(int val){
    if(val==0){
      if(there == State.there){
        setVisCol(false,false);
      }
      there = State.gone;
      prop&=~Propagation.Inside;
      if(freeze)foreach(Entity e in todraw)e.Active = false;
    } else {
      there = State.trying;
      tryManifest();
      if(doBoost)Add(new Coroutine(bumpUp()));
    }
  }
  public void renderMaterial(IMaterialLayer l, Camera c){
    if(there == State.there) return;
    foreach(Entity e in todraw) if(e.Scene != null && e.Depth<=l.depth)e.Render();
  }
  public override void Update(){
    base.Update();
    if(there == State.trying) tryManifest();
  }
}
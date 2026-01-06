


using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class IntroCarW:IntroCar, ISimpleEnt{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public Vector2 ppos;
  float yoffset = 0;
  public Template.Propagation prop=>Template.Propagation.Riding|Template.Propagation.Shake;
  public IntroCarW(EntityData d, Vector2 o):base(o+d.Position){}
  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    if(vis!=0) wheels.Visible = Visible = vis>0;
    if(col!=0) wheels.Collidable = Collidable = col>0;
    if(act!=0) wheels.Active = Active = act>0;
  }
  void ITemplateChild.setOffset(Vector2 ppos){
    toffset = Position-ppos; 
    this.ppos=ppos;
  } 
  void FixPos(){
    Vector2 npos = (ppos+toffset).Round();
    MoveTo(npos+yoffset*Vector2.UnitY, parent?.gatheredLiftspeed??Vector2.Zero);
    wheels.Position = npos;
  }
  void ITemplateChild.relposTo(Vector2 pos, Vector2 liftspeed){
    this.ppos = pos;
    FixPos();
  }
  public override void Update() {
    bool flag = HasRider();
    if (yoffset>0 && (!flag || yoffset>1)){
      yoffset = Calc.Approach(yoffset, 0, 10*Engine.DeltaTime);
      FixPos();
    }
    if (!didHaveRider && flag){
      yoffset = 2;
      FixPos();
    }
    if (didHaveRider && !flag)Audio.Play("event:/game/00_prologue/car_up", Position);
    didHaveRider = flag;
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    parent.AddNewEnts([wheels]);
  }
  bool ITemplateChild.hasPlayerRider(){
    return UpdateHook.cachedPlayer?.IsRiding(this)??false;
  }
}
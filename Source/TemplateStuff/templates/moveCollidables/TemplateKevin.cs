using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateKevin")]
public class TemplateKevin:TemplateMoveCollidable{
  public TemplateKevin(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  enum Axis{vertical, horizontal}
  Stack<Tuple<Axis, float>> moves = new(); 
  Vector2 godir;
  float maxspeed;
  float acceleration;
  float returnSpeed;
  float locktime=0.25f;
  int leniency;
  bool locked;
  bool[] dirflags;
  bool returning;
  string StartSfx = "event:/new_content/game/10_farewell/fusebox_hit_1";
  string ImpactSfx = "event:/game/general/fallblock_impact";
  public TemplateKevin(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    OnDashCollide = (p,dir)=>{
      if(!detatched) disconnect();
      if(locked || godir==dir) return ((ITemplateChild)this).propagateDashhit(p,dir);
      bool flag=false;
      if(dir.X!=0) flag = dir.X<0?dirflags[1]:dirflags[3];
      if(dir.Y!=0) flag = dir.Y<0?dirflags[2]:dirflags[0];
      if(!flag) return ((ITemplateChild)this).propagateDashhit(p,dir);
      if(returning) AddDirectionToStack(dir.X!=0? Axis.horizontal:Axis.vertical);
      if(current!=null)Remove(current);
      Add(current = new Coroutine(GoSequence(-dir)));
      return DashCollisionResults.Rebound;
    };
    dirflags = [
      d.Bool("top",false),d.Bool("right",false),d.Bool("bottom",false),d.Bool("left",false)
    ];
    leniency = d.Int("leniency",4);
    maxspeed = d.Float("maxspeed",240);
    acceleration = d.Float("acceleration",500);
    returnSpeed = d.Float("returnSpeed",60);
    returning = d.Bool("returning",true);
    ImpactSfx = d.Attr("ImpactSfx","event:/game/general/fallblock_impact");
    StartSfx = d.Attr("StartSfx","event:/new_content/game/10_farewell/fusebox_hit_1");
  }
  Coroutine current=null;
  void AddDirectionToStack(Axis ax){
    if(moves.Count ==0 || moves.Peek().Item1!=ax){
      moves.Push(new(ax,getPosAxis(ax)));
    } 
  }
  EventInstance sfx=null;
  IEnumerator GoSequence(Vector2 dir){
    if(sfx!=null)Audio.Stop(sfx);
    sfx = Audio.Play(StartSfx,Position);
    float speed = 0;
    locked = true;
    godir = dir;
    shake(locktime);
    yield return locktime;
    if(sfx!=null)Audio.Stop(sfx);
    sfx=null;
    locked = false;
    while(true){
      speed = Calc.Approach(speed, maxspeed, Engine.DeltaTime*acceleration);
      var q = getq(godir.Abs()*speed*Engine.DeltaTime+leniency*Vector2.One);
      ownLiftspeed = godir*speed;
      bool hit = false;
      if(godir.Y!=0) hit = MoveVCollide(q,speed*godir.Y*Engine.DeltaTime,leniency);
      else hit = MoveHCollide(q,speed*godir.X*Engine.DeltaTime,leniency);
      if(hit){
        Audio.Play(ImpactSfx,Position);
        shake(0.3f);
        godir = Vector2.Zero;
        ownLiftspeed = Vector2.Zero;
        yield return 0.4f;
        if(returning)Add(current = new Coroutine(ReturnSequence()));
        yield break;
      }
      yield return null;
    }
  }
  float getPosAxis(Axis targ)=>targ==Axis.horizontal?Position.X:Position.Y;
  void setPosAxis(Axis targ, float n){
    if(targ == Axis.horizontal) Position.X = n;
    else Position.Y = n;
  }
  bool moveTowardsPosAxis(Axis ax, float targ, float amount){
    float n = Calc.Approach(getPosAxis(ax),targ,amount);
    setPosAxis(ax,n);
    childRelposSafe();
    return targ == getPosAxis(ax);
  }
  IEnumerator ReturnSequence(){
    float speed = 0;
    while(true){  
      if(moves.Count == 0)yield break;
      var th = moves.Peek();
      speed = Calc.Approach(speed, returnSpeed, acceleration*Engine.DeltaTime/2);
      ownLiftspeed = th.Item1 ==Axis.horizontal?Vector2.UnitX:Vector2.UnitY;
      ownLiftspeed*=Math.Sign(th.Item2-getPosAxis(th.Item1));
      if(!moveTowardsPosAxis(th.Item1,th.Item2,speed*Engine.DeltaTime)){
        yield return null;
        continue;
      }
      shake(0.2f);
      moves.Pop();
      speed = 0;
      ownLiftspeed=Vector2.Zero;
      yield return 0.2f;
    }
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    if(sfx!=null)Audio.Stop(sfx);
  }
}
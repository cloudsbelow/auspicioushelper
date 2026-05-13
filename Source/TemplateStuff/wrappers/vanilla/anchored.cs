


using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public interface IBlockChild: ITemplateChild{
  Template.Propagation ITemplateChild.prop => Template.Propagation.All;
  public Vector2 toffset {get;set;}
  public Vector2 ppos {get;set;}
  Solid self=>(Solid) this;

  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    Platform p = self;
    if(vis!=0)p.Visible = vis>0;
    if(col!=0)p.Collidable = col>0;
    if(act!=0)p.Active = act>0;
    if(p.Scene==null) return;
    if(col>0) p.EnableStaticMovers();
    else if(col<0) p.DisableStaticMovers();
  }
  bool ITemplateChild.hasInside(Actor a)=>self.Collider.Collide(a.Collider);
  void ITemplateChild.destroy(bool particles)=>self.RemoveSelf();
  bool ITemplateChild.hasPlayerRider()=>self.HasPlayerRider();
  void ITemplateChild.setOffset(Vector2 parentpos){
    ppos=parentpos;
    toffset=self.Position-ppos;
  }
  void ITemplateChild.addTo(Monocle.Scene s)=>s.Add(self);
  void FixPosition(Vector2? ls);
  void ITemplateChild.relposTo(Vector2 loc, Vector2 parentLiftspeed){
    ppos=loc;
    FixPosition(parentLiftspeed);
  }
}
public class ZipMoverW:ZipMover, IBlockChild{  
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Vector2 dir;
  Vector2 ownLiftspeed;
  public Vector2 ppos {get;set;}
  public ZipMoverW(EntityData d, Vector2 o):base(d,o){
    Remove(Get<Coroutine>());
    Add(new Coroutine(NSequence()));
    dir = target-Position;
    OnDashCollide = (p, dir)=>((ITemplateChild) this).propagateDashhit(p,dir);
  }
  public void FixPosition(Vector2? ls=null){
    Vector2 np = ppos+toffset+percent*dir;
    if(ls is {} lsd){
      target = ppos+toffset+dir;
      if(Scene==null){
        Position=np;
        return;
      }
      Vector2 halfSize = new Vector2(Width,Height)/2;
      pathRenderer.from=ppos+toffset+halfSize;
      pathRenderer.to=target+halfSize;
    } else lsd=parent.gatheredLiftspeed;
    MoveTo(np,lsd+ownLiftspeed);
  }
  void ITemplateChild.AddAllChildren(List<Entity> list){
    list.Add(this);
    if(pathRenderer!=null) list.Add(pathRenderer);
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    parent.AddNewEnts([pathRenderer]);
  }
  public IEnumerator NSequence(){
    Vector2 start = Position;
    while (true){
      while(!HasPlayerRider()) yield return null;

      sfx.Play((theme == Themes.Normal) ? "event:/game/01_forsaken_city/zip_mover" : "event:/new_content/game/10_farewell/zip_mover");
      Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
      StartShaking(0.1f);
      yield return 0.1f;
      streetlight.SetAnimationFrame(3);
      StopPlayerRunIntoAnimation = false;
      float at2 = 0f;
      while (at2 < 1f){
        yield return null;
        at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
        percent = Util.SineIn(at2, out var d);
        ownLiftspeed = 2*d*dir;
        Vector2 vector = Vector2.Lerp(start, target, percent);
        ScrapeParticlesCheck(vector);
        if (Scene.OnInterval(0.1f)){
          pathRenderer.CreateSparks();
        }
        FixPosition();
      }

      StartShaking(0.2f);
      Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
      SceneAs<Level>().Shake();
      StopPlayerRunIntoAnimation = true;
      yield return null;
      ownLiftspeed=Vector2.Zero;
      yield return 0.5f-Engine.DeltaTime;

      StopPlayerRunIntoAnimation = false;
      streetlight.SetAnimationFrame(2);
      at2 = 0f;
      while (at2 < 1f){
        yield return null;
        at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
        percent = 1f - Util.SineIn(at2,out float d);
        ownLiftspeed = -0.5f*d*dir;
        FixPosition();
      }

      StopPlayerRunIntoAnimation = true;
      StartShaking(0.2f);
      streetlight.SetAnimationFrame(1);
      yield return null;
      ownLiftspeed=Vector2.Zero;
      yield return 0.5f-Engine.DeltaTime;
    }
  }
}

public class SwapBlockW:SwapBlock, IBlockChild{
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Vector2 dir;
  Vector2 ownLiftspeed;
  public Vector2 ppos {get;set;}
  public SwapBlockW(EntityData d, Vector2 o):base(d,o){
    dir = end-start;
    OnDashCollide = (p, dir)=>((ITemplateChild) this).propagateDashhit(p,dir);
  }
  public void FixPosition(Vector2? ls=null){
    if(ls is {} lsd){
      start = ppos+toffset;
      end = ppos+toffset+dir;
      moveRect.X=(int)Math.Min(start.X,end.X);
      moveRect.Y=(int)Math.Min(start.Y,end.Y);
      if(Scene==null){
        Position=start;
        return;
      }
      path.Position = start;
    } else lsd = parent.gatheredLiftspeed;
    Vector2 np = Vector2.Lerp(start, end, lerp);
    MoveTo(np,lsd+ownLiftspeed);
  }
  void ITemplateChild.AddAllChildren(List<Entity> list){
    list.Add(this);
    if(path!=null) list.Add(path);
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    parent.AddNewEnts([path]);
  }
  static Vector2 fixLs(Vector2 ls, SwapBlock self){
    if(self is SwapBlockW e){
      e.ownLiftspeed=ls;
      return e.ownLiftspeed+e.parent.gatheredLiftspeed;
    }
    else return ls;
  }
  [OnLoad.ILHook(typeof(SwapBlock),nameof(Update))]
  static void MOveHook(ILContext ctx){
    ILCursor c = new(ctx);
    while(c.TryGotoNext(MoveType.Before,
      itr=>itr.MatchCall<Platform>(nameof(Platform.MoveTo))
    )){
      c.EmitLdarg0();
      c.EmitDelegate(fixLs);
      c.Index++;
    }
  }
  public override void Update(){
    ownLiftspeed=Vector2.Zero;
    base.Update();
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    path?.RemoveSelf();
  }
}
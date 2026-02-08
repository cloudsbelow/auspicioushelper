


using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

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

class CrumbleBlockW:CrumblePlatform, ISimpleEnt{
  TemplateDisappearer.vcaTracker vca = new();
  bool ownCollidable=true;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public CrumbleBlockW(EntityData d, Vector2 o):base(d,o){}
  static IEnumerator AddDelegate(IEnumerator orig, CrumblePlatform s){
    if(s is not CrumbleBlockW c) return orig;
    else return c.NewSequence();
  }
  [OnLoad.ILHook(typeof(CrumblePlatform),nameof(CrumblePlatform.orig_Added))]
  static void AddILHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNext(MoveType.After,itr=>itr.MatchCallvirt<CrumblePlatform>(nameof(Sequence)))){
      c.EmitLdarg0();
      c.EmitDelegate(AddDelegate);
    } else DebugConsole.WriteFailure("Failed to add hook",true);
  }
  void ITemplateChild.relposTo(Vector2 ppos, Vector2 ls)=>MoveTo(ppos+toffset,ls);
  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    vca.Align(vis,col,act);
    vca.Apply(this, ocol:ownCollidable);
  }
  public IEnumerator NewSequence(){
    while (true)
    {
      bool onTop;
      if (GetPlayerOnTop() != null)
      {
        onTop = true;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
      }
      else
      {
        if (GetPlayerClimbing() == null)
        {
          yield return null;
          continue;
        }

        onTop = false;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
      }

      Audio.Play("event:/game/general/platform_disintegrate", Center);
      shaker.ShakeFor(onTop ? 0.6f : 1f, removeOnFinish: false);
      foreach (Image image in images)
      {
        SceneAs<Level>().Particles.Emit(P_Crumble, 2, Position + image.Position + new Vector2(0f, 2f), Vector2.One * 3f);
      }

      for (int i = 0; i < (onTop ? 1 : 3); i++)
      {
        yield return 0.2f;
        foreach (Image image2 in images)
        {
          SceneAs<Level>().Particles.Emit(P_Crumble, 2, Position + image2.Position + new Vector2(0f, 2f), Vector2.One * 3f);
        }
      }

      float timer = 0.4f;
      if (onTop)
      {
        while (timer > 0f && GetPlayerOnTop() != null)
        {
          yield return null;
          timer -= Engine.DeltaTime;
        }
      }
      else
      {
        while (timer > 0f)
        {
          yield return null;
          timer -= Engine.DeltaTime;
        }
      }

      outlineFader.Replace(OutlineFade(1f));
      occluder.Visible = Collidable = ownCollidable = false;
      float num = 0.05f;
      for (int j = 0; j < 4; j++)
      {
        for (int k = 0; k < images.Count; k++)
        {
          if (k % 4 - j == 0)
          {
            falls[k].Replace(TileOut(images[fallOrder[k]], num * (float)j));
          }
        }
      }

      yield return 2f;
      while (CollideCheck<Player>())
      {
        yield return null;
      }

      outlineFader.Replace(OutlineFade(0f));
      occluder.Visible = ownCollidable = true;
      Collidable = ownCollidable && vca.Collidable;
      for (int l = 0; l < 4; l++)
      {
        for (int m = 0; m < images.Count; m++)
        {
          if (m % 4 - l == 0)
          {
            falls[m].Replace(TileIn(m, images[fallOrder[m]], 0.05f * (float)l));
          }
        }
      }
    }
  }
}



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Import;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/SillySpikes")]
[Tracked]
public class CustomSpikes : Entity{
  public enum Directions {
    Up, Down, Left, Right
  }
  Vector2 unitDir=> Direction switch {
    Directions.Up=>-Vector2.UnitY,Directions.Down=>Vector2.UnitY,
    Directions.Left=>-Vector2.UnitX,Directions.Right=>Vector2.UnitX,
    _=>Vector2.Zero
  };


  public Directions Direction;
  public Vector2 imageOffset;
  bool dreamThru, dashThru, triggerSpike;
  bool fixPickup, fixDash, fixOnblock, fixOwnSpeed;
  int triggerStage=0;
  float triggerTiming, untriggerTiming;
  
  LiftspeedSm sm;
  Template parent;
  List<Image> images = new();
  public CustomSpikes(EntityData data, Vector2 offset): base(data.Position+offset){
    Direction = data.Enum("direction",Directions.Up);
    Depth = -1;
    int size = (Direction==Directions.Up || Direction==Directions.Down)? data.Width:data.Height;
    dreamThru = data.Bool("dreamThru", false);
    dashThru = data.Bool("dashThru", false);
    fixOwnSpeed = data.Bool("useOwnSpeed",false);
    fixPickup = data.Bool("fixPickup",true);
    fixDash = data.Bool("fixDash",true);
    fixOnblock = data.Bool("fixOnBlock",true);
    triggerSpike = data.Bool("TriggerSpike",false);
    var li = Util.csparseflat(data.Attr("triggerTimer","-1"),-1,-1);
    triggerTiming = li[0];
    untriggerTiming = li[1];
    switch (Direction) {
      case Directions.Up:
        base.Collider = new Hitbox(size, 3f, 0f, -3f);
        Add(new LedgeBlocker());
        break;
      case Directions.Down:
        base.Collider = new Hitbox(size, 3f);
        break;
      case Directions.Left:
        base.Collider = new Hitbox(3f, size, -3f);
        Add(new LedgeBlocker());
        break;
      case Directions.Right:
        base.Collider = new Hitbox(3f, size);
        Add(new LedgeBlocker());
        break;
    }

    Add(new PlayerCollider(OnCollide));
    if(EntityParser.currentParent is {} te){
      parent = te;
    } else if(data.Bool("canAttach")) Add(sm = new LiftspeedSm{
      OnShake = (Vector2 amount)=>imageOffset+=amount,
      SolidChecker = IsRiding,
      JumpThruChecker = IsRiding,
      OnEnable = ()=>Active=(Visible=(Collidable=true)),
      OnDisable = ()=>Active=(Visible=(Collidable=false)),
      OnMoveOther = amount=>Position+=amount
    });
    
    List<MTexture> subtex = GFX.Game.GetAtlasSubtextures($"danger/spikes/{data.Attr("type")}_{Direction.ToString().ToLower()}");
    if(data.tryGetStr("fancy",out var fancyRecolor)) subtex = subtex.Map(Util.ColorRemap.Get(fancyRecolor).RemapTex);
    Color c = Util.hexToColor(data.String("tint","#ffffff"));
    for (int j = 0; j < size / 8; j++) {
      var image = new Image(Calc.Random.Choose(subtex));
      switch (Direction) {
        case Directions.Up:
          image.JustifyOrigin(0.5f, 1f);
          image.Position = Vector2.UnitX * ((float)j + 0.5f) * 8f + Vector2.UnitY;
          break;
        case Directions.Down:
          image.JustifyOrigin(0.5f, 0f);
          image.Position = Vector2.UnitX * ((float)j + 0.5f) * 8f - Vector2.UnitY;
          break;
        case Directions.Right:
          image.JustifyOrigin(0f, 0.5f);
          image.Position = Vector2.UnitY * ((float)j + 0.5f) * 8f - Vector2.UnitX;
          break;
        case Directions.Left:
          image.JustifyOrigin(1f, 0.5f);
          image.Position = Vector2.UnitY * ((float)j + 0.5f) * 8f + Vector2.UnitX;
          break;
      }
      image.Color = c;
      if(triggerSpike) image.Position-=unitDir*4;
      Add(image);
      images.Add(image);
    }
    if(triggerSpike) DebugConsole.Write("timings", triggerTiming, untriggerTiming);
  }
  public override void Render(){
    if (MaterialPipe.clipBounds.CollideExRect(Position.X,Position.Y,Width,Height)){
      Vector2 position = Position;
      Position += imageOffset;
      base.Render();
      Position = position;
    }
  }
  bool shouldKill(Player p){
    Vector2 realSpeed = p.Speed;
    int s = p.StateMachine.State;
    if(dreamThru && (s==Player.StDreamDash || CommunalHelperIop.InTunnel(p))) return false;
    if(dashThru && s==Player.StDash) return false;

    if(((fixPickup && s==Player.StPickup) || (fixDash && s==Player.StDash && p.Speed==Vector2.Zero)) && oldSpeed is {} old) realSpeed+=old;
    if(fixOwnSpeed) realSpeed-=sm?.getLiftspeed()??parent?.gatheredLiftspeed??Vector2.Zero;
    
    float ms = Math.Max(Math.Abs(p.LiftSpeed.X),Math.Abs(p.LiftSpeed.Y));
    float tsm = Math.Max(p.LiftSpeedGraceTime-(ms==0?0:1/ms)-Engine.DeltaTime, float.Epsilon);
    if(fixOnblock && p.liftSpeedTimer>=tsm) realSpeed+=p.LiftSpeed;

    return Direction switch {
      Directions.Up=>realSpeed.Y >= 0f && p.Bottom <= base.Bottom,
      Directions.Down=>realSpeed.Y <= 0f,
      Directions.Left=>realSpeed.X >= 0f,
      Directions.Right=>realSpeed.X <= 0f,
      _=>false
    };
  }
  public void OnCollide(Player p){
    if(shouldKill(p)){
      if(triggerSpike && triggerStage<2){
        if(triggerStage==0){
          triggerStage++;
          Add(new Coroutine(emergeRoutine()));
        }
        return;
      }
      p.Die(unitDir*2);
      parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(null);
    }
  }
  public static void spikeCheck(Player p){
    var orig = p.Collider;
    p.Collider = p.hurtbox;
    foreach(CustomSpikes c in p.Scene.Tracker.GetEntities<CustomSpikes>()){
      if(p.CollideCheck(c)) c.OnCollide(p);
      if(p.Dead) break;
    }
    p.Collider = orig;
  }
  IEnumerator emergeRoutine(){
    float countdown = triggerTiming;
    if(triggerTiming<0){
      countdown = 0.4f;
      while(true){
        if(countdown>0){
          countdown = countdown-Engine.DeltaTime;
          yield return null;
          continue;
        }
        if(UpdateHook.cachedPlayer is {} p && !p.Dead){
          var orig = p.Collider;
          p.Collider = p.hurtbox;
          bool flag = p.CollideCheck(this) && shouldKill(p);
          p.Collider = orig;
          if(!flag) break;
          yield return null;
        }
      }
      countdown = 0.05f;
    }
    if(countdown >= 0.05f){
      yield return countdown-0.05f;
      countdown = 0.05f;
    }
    Add(new Coroutine(animate(1,3)));
    yield return countdown;
    if(triggerStage<=2) triggerStage = 2;
    while(triggerStage != 3) yield return null;

    if((countdown = untriggerTiming)<0) yield break;
    if(countdown >= 0.05f){
      yield return countdown-0.05f;
    }
    Add(new Coroutine(animate(-1,0)));
  }
  IEnumerator animate(int dir, int endstate){
    float prog = 0;
    while(prog<1){
      if(Math.Floor(prog*4)!=Math.Floor((prog+=Engine.DeltaTime*8)*4)){
        foreach(var image in images) image.Position+=unitDir*dir;
      }
      if(prog>=1) break;
      yield return null;
    }
    triggerStage = endstate;
  }

  public bool IsRiding(Solid solid) => Direction switch {
    Directions.Up => CollideCheckOutside(solid, Position + Vector2.UnitY),
    Directions.Down => CollideCheckOutside(solid, Position - Vector2.UnitY),
    Directions.Left => CollideCheckOutside(solid, Position + Vector2.UnitX),
    Directions.Right => CollideCheckOutside(solid, Position - Vector2.UnitX),
    _ => false,
  };
  public bool IsRiding(JumpThru jumpThru) {
    if (Direction != Directions.Up) return false;
    return CollideCheck(jumpThru, Position + Vector2.UnitY);
  }

  public static Vector2? oldSpeed;
  static void SetOldSpeed(Vector2 speed)=>oldSpeed = speed;
  static void RemoveOldSpeed()=>oldSpeed = null;
  [OnLoad.ILHook(typeof(Player),nameof(Player.PickupCoroutine),Util.HookTarget.Coroutine)]
  static void PickupHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchLdfld<Player>(nameof(Player.Speed)),
      itr=>itr.MatchStfld(out var field) && field.Name.Contains("oldSpeed")
    )){
      c.Index++;
      c.EmitDup();
      c.EmitDelegate(SetOldSpeed);
    } else DebugConsole.WriteFailure("Could not add pickup speed retain hook",true);
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchLdfld(out var field) && field.Name.Contains("oldSpeed"),
      itr=>itr.MatchStfld<Player>(nameof(Player.Speed))
    )){
      c.EmitDelegate(RemoveOldSpeed);
    } else DebugConsole.WriteFailure("Could not add pickup speed retain hook",true);
  }
  [OnLoad.OnHook(typeof(Player),nameof(Player.DashBegin))]
  static void Hook(On.Celeste.Player.orig_DashBegin orig, Player p){
    orig(p);
    oldSpeed = p.beforeDashSpeed;
  }
  [OnLoad.ILHook(typeof(Player),nameof(Player.DashCoroutine), Util.HookTarget.Coroutine)]
  static void DashHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.After, itr=>itr.MatchStfld<Player>(nameof(Player.Speed)))){
      c.EmitDelegate(RemoveOldSpeed);
    } else DebugConsole.WriteFailure("COuld not add old speed invalidation hook");
  }
}
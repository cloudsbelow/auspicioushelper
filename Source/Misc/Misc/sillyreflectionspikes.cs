


using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/SillySpikes")]
public class CustomSpikes : Entity{
  public enum Directions {
    Up, Down, Left, Right
  }


  public Directions Direction;
  public Vector2 imageOffset;
  bool dreamThru;
  bool dashThru;
  bool fixPickup;
  bool fixDash;
  bool fixOnblock;
  bool fixOwnSpeed;
  StaticMover sm;
  Template parent;
  Vector2 ls;
  float speedRetainTime;
  public CustomSpikes(EntityData data, Vector2 offset)
    : base(data.Position+offset)
  {
    Direction = data.Enum("direction",Directions.Up);
    Depth = -1;
    int size = (Direction==Directions.Up || Direction==Directions.Down)? data.Width:data.Height;
    dreamThru = data.Bool("dreamThru", false);
    dashThru = data.Bool("dashThru", false);
    fixOwnSpeed = data.Bool("useOwnSpeed",false);
    fixPickup = data.Bool("fixPickup",true);
    fixDash = data.Bool("fixDash",true);
    fixOnblock = data.Bool("fixOnBlock",true);
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
    } else if(data.Bool("canAttach")) Add(sm = new StaticMover{
      OnShake = (Vector2 amount)=>imageOffset+=amount,
      SolidChecker = IsRiding,
      JumpThruChecker = IsRiding,
      OnEnable = ()=>Active=(Visible=(Collidable=false)),
      OnDisable = ()=>Active=(Visible=(Collidable=false)),
      OnMove=(Vector2 amount)=>{
        Position+=amount;
        ls = sm.Platform.LiftSpeed;
        speedRetainTime = Engine.DeltaTime+0.001f;
      }
    });
    
    List<MTexture> subtex = GFX.Game.GetAtlasSubtextures($"danger/spikes/{data.Attr("type")}_{Direction.ToString().ToLower()}");
    if(data.tryGetStr("fancy",out var fancyRecolor)) subtex = subtex.Map(Util.ColorRemap.Get(fancyRecolor).RemapTex);
    Color c = Util.hexToColor(data.String("tint","#ffffff"));
    for (int j = 0; j < size / 8; j++) {
      Image image = new Image(Calc.Random.Choose(subtex));
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
      Add(image);
    }
  }
  public override void Render()
  {
    if (MaterialPipe.clipBounds.CollideExRect(Position.X,Position.Y,Width,Height)){
      Vector2 position = Position;
      Position += imageOffset;
      base.Render();
      Position = position;
    }
  }
  public override void Update() {
    base.Update();
    if(speedRetainTime>0){
      speedRetainTime-=Engine.DeltaTime;
      if(speedRetainTime<=0) ls=Vector2.Zero;
    }
  }
  public void OnCollide(Player p){
    Vector2 realSpeed = p.Speed;
    int s = p.StateMachine.State;
    if((dreamThru && s==Player.StDreamDash) || (dashThru && s==Player.StDash)) return;
    if(((fixPickup && s==Player.StPickup) || (fixDash && s==Player.StDash && p.Speed==Vector2.Zero)) && oldSpeed is {} old) realSpeed+=old;
    if(fixOnblock && s==Player.StNormal||s==Player.StDash && p.liftSpeedTimer>=p.LiftSpeedGraceTime-Engine.DeltaTime){
      realSpeed+=p.LiftSpeed;
    }
    if(fixOwnSpeed){
      if(parent is {} te) realSpeed-=te.gatheredLiftspeed;
      else realSpeed-=ls;
    }
    switch (Direction) {
      case Directions.Up:
        if (realSpeed.Y >= 0f && p.Bottom <= base.Bottom) p.Die(new Vector2(0f, -1f));
        break;
      case Directions.Down:
        if (realSpeed.Y <= 0f)p.Die(new Vector2(0f, 1f));
        break;
      case Directions.Left:
        if (realSpeed.X >= 0f)p.Die(new Vector2(-1f, 0f));
        break;
      case Directions.Right:
        if (realSpeed.X <= 0f) p.Die(new Vector2(1f, 0f));
        break;
    }
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
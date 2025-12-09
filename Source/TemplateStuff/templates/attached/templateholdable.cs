


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;


public interface ICustomHoldableRelease{
  bool replaceNormalRelease {get;}
  static Player releasing;
  static void ReleaseHook(On.Celeste.Holdable.orig_Release orig, Holdable s, Vector2 force){
    releasing = s.Holder;
    if(s.Entity is ICustomHoldableRelease c && c.replaceNormalRelease && s.OnRelease!=null){
      s.OnRelease(force);
    } else orig(s,force);
    releasing = null;
  }
  public static HookManager hooks = new(()=>{
    On.Celeste.Holdable.Release+=ReleaseHook;
  },()=>{
    On.Celeste.Holdable.Release-=ReleaseHook;
  },auspicioushelperModule.OnEnterMap);
}

[CustomEntity("auspicioushelper/templateholdable")]
[Tracked]
public class TemplateHoldable:Actor, ICustomHoldableRelease{
  public interface IPickupChild{
    void OnPickup(Player p);
    void OnRelease(Player p, Vector2 force);
  }
  TemplateDisappearer te;
  Vector2 hoffset;
  public Vector2 Offset=>hoffset;
  Vector2 lpos;
  Vector2 prevLiftspeed;
  Vector2 Speed;
  Holdable Hold;
  Vector2 origpos;
  float noGravityTimer=0;
  bool keepCollidableAlways = false;
  float playerfrac; float theofrac;
  string wallhitsound;
  float wallhitKeepspeed;
  float gravity;
  float friction;
  float terminalvel;
  bool dietobarrier;
  bool respawning;
  float respawndelay;
  public EntityData d;
  bool dontFlingOff=false;
  bool hasBeenTouched = false;
  bool showTutorial = false;
  bool startFloating = false;
  bool dangerous = false;
  float voidDieOffset = 100;
  float minHoldTimer = 0.35f;
  float[] customThrowspeeds;
  bool moveImmediately = true;
  public TemplateHoldable(EntityData d, Vector2 offset):base(d.Position+offset){
    Position+=new Vector2(d.Width/2, d.Height);
    hoffset = d.Nodes.Length>0?(d.Nodes[0]-new Vector2(d.Width/2, d.Height)):new Vector2(0,-d.Height/2);
    Collider = new Hitbox(d.Width,d.Height,-d.Width/2,-d.Height);
    if(d.Width>8 || d.Height>11) replaceNormalRelease = true;
    lpos = Position;
    Add(Hold = new Holdable(d.Float("cannot_hold_timer",0.1f)));
    var ex = Util.ciparseflat(d.Attr("Holdable_collider_expand","4"));
    int topEx = ex.Length switch{>=1=>ex[0],_=>4};
    int rightEx = ex.Length switch{>=2=>ex[1],>=1=>ex[0],_=>4};
    int bottomEx = ex.Length switch{>=3=>ex[2],>=1=>ex[0],_=>4};
    int leftEx = ex.Length switch{>=4=>ex[3],>=2=>ex[1],>=1=>ex[0],_=>4};
    Hold.PickupCollider = new Hitbox(d.Width+rightEx+leftEx, d.Height+topEx+bottomEx, -d.Width/2-leftEx, -d.Height-topEx);
    Hold.SlowFall = d.Bool("slowfall",false);
    Hold.SlowRun = d.Bool("slowrun",true); 
    Hold.SpeedGetter = ()=>Speed;
    Hold.SpeedSetter = (Vector2 v)=>Speed=v;
    Hold.OnPickup = OnPickup;
    Hold.OnRelease = OnRelease;
    Hold.OnHitSpring = HitSpring;
    Hold.DangerousCheck = (HoldableCollider c)=>{
      return dangerous && Hold.Holder == null && Speed!=Vector2.Zero;
    };
    LiftSpeedGraceTime = 0.1f;
    Tag = Tags.TransitionUpdate;
    keepCollidableAlways = d.Bool("always_collidable",false);
    origpos = Position;
    hooks.enable();
    this.d=d;

    playerfrac = d.Float("player_momentum_weight",1);
    theofrac = d.Float("holdable_momentum_weight",0);
    wallhitsound = d.Attr("wallhitsound","event:/game/05_mirror_temple/crystaltheo_hit_side");
    wallhitKeepspeed = d.Float("wallhit_speedretain",0.4f);
    gravity = d.Float("gravity",800f);
    terminalvel = d.Float("terminal_velocity",200f);
    friction = d.Float("friction",350);
    dietobarrier = d.Bool("die_to_barrier",false);
    respawning = d.Bool("respawning",false);
    respawndelay = d.Float("respawnDelay",2f);
    dontFlingOff = d.Bool("dontFlingOff",false);
    showTutorial = d.Bool("tutorial",false);
    startFloating = d.Bool("start_floating",false);
    dangerous = d.Bool("dangerous",false);
    voidDieOffset = d.Float("voidDieOffset",100);
    minHoldTimer = d.Float("minHoldTimer",0.35f);
    SquishCallback = OnSquish2;
    customThrowspeeds = Util.csparseflat(d.Attr("customThrowspeeds"));
    Depth = -1;
  }
  Util.HybridSet<Platform> Mysolids;
  void make(Scene s, templateFiller use = null){
    created = true;
    using(new Template.ChainLock())te = new TemplateDisappearer(d,Position+hoffset,d.Int("depthoffset",0));
    new DynamicData(te).Set("ausp_holdable",this);
    if(use!=null) te.setTemplate(use);
    te.addTo(s);
    Mysolids = new (te.GetChildren<Solid>());
  }
  BirdTutorialGui tutorialGui=null;
  IEnumerator tutorialRoutine(){
    yield return 0.25f;
    if(tutorialGui!=null) tutorialGui.Open=true;
  }
  bool created = false;
  public bool isCreated=>created;
  public void makeExternally(templateFiller f){
    if(created) return;
    make(Scene,f);
    Active = true;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(!string.IsNullOrEmpty(d.Attr("template",""))) make(scene);
    else Active = false;

    if(showTutorial){
      tutorialGui = new BirdTutorialGui(this, new Vector2(0f, -Height/2-8), Dialog.Clean("tutorial_carry"), Dialog.Clean("tutorial_hold"), BirdTutorialGui.ButtonPrompt.Grab);
      tutorialGui.Open = false;
      scene.Add(tutorialGui);
      Add(new Coroutine(tutorialRoutine()));
    }
  }
  void touched(){
    hasBeenTouched=true;
    if(tutorialGui==null) return;
    tutorialGui.RemoveSelf();
    tutorialGui=null;
  }
  public override void Removed(Scene scene){
    if(te!=null){
      te.destroy(true);
      te = null;
    }
    base.Removed(scene);
  }
  public override bool IsRiding(JumpThru jumpThru){
    return Speed.Y ==0 && !Hold.IsHeld && base.IsRiding(jumpThru);
  }
  public override bool IsRiding(Solid jumpThru){
    return Speed.Y ==0 && !Hold.IsHeld && base.IsRiding(jumpThru);
  }
  bool HitSpring(Spring s){
    if(Hold.IsHeld) return false;
    if(s.Get<ChildMarker>() is ChildMarker c && c.propagatesTo(te)) return false;
    if (s.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f){
      Speed.X *= 0.5f;
      Speed.Y = -160f;
      noGravityTimer = 0.15f;
      return true;
    }
    if (s.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f){
      MoveTowardsY(s.CenterY + 5f, 4f);
      Speed.X = 220f;
      Speed.Y = -80f;
      noGravityTimer = 0.1f;
      return true;
    }
    if (s.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f){
      MoveTowardsY(s.CenterY + 5f, 4f);
      Speed.X = -220f;
      Speed.Y = -80f;
      noGravityTimer = 0.1f;
      return true;
    }
    return false;
  }
  void OnPickup()
  {
    touched();
    AllowPushing=false;
    IgnoreJumpThrus=true;
    if (Hold.Holder is Player p){
      p.Speed = p.Speed * playerfrac + Speed * theofrac;
    }
    Speed = Vector2.Zero;
    AddTag(Tags.Persistent);
    foreach (Entity e in te.GetChildren<Entity>()){
      e.AddTag(Tags.Persistent);
      if(e is IPickupChild pc) pc.OnPickup(Hold.Holder);
      if(e is IBoundsHaver h) h.bounds = new FloatRect(-0x0fffffff,-0x0fffffff,0x1fffffff,0x1fffffff);
    }
    if (!keepCollidableAlways) te.setCollidability(false);
  }
  public bool replaceNormalRelease {get;}=false;
  void OnRelease(Vector2 force){
    Position = Position.Round();
    if(Depth!=-1) Depth=-1;
    AllowPushing=true;
    IgnoreJumpThrus=false;
    if(resetting) return;
    Player p = Hold.Holder??ICustomHoldableRelease.releasing;
    if(replaceNormalRelease){
      FloatRect bounds;
      int num=8;
      float boundsbottom = MathF.Min(p.Top,Bottom)+Height+p.Height;
      if(Width>p.Width) bounds = FloatRect.fromCorners(
        new Vector2(p.Right-Width,Top),
        new Vector2(p.Left+Width,boundsbottom)
      ); else bounds = FloatRect.fromCorners(
        new Vector2(p.Left,Top),
        new Vector2(p.Right+Width,boundsbottom)
      );
      bounds.expandLeft(force.X<0?num:1);
      bounds.expandRight(force.X>0?num:1);
      var q = TemplateMoveCollidable.getQinfo(bounds, new(te.GetChildren<Solid>()), Scene);
      FloatRect f = new FloatRect(this);
      if(!q.Collide(f,Vector2.Zero)) goto done;
      if(Width>p.Width) f = new(p.Left,p.Top,p.Width,1);
      else f = new(p.CenterX-Width/2,p.Top,Width,1);
      if(q.Collide(f,Vector2.Zero)) goto doneFail;
      while(Bottom<f.bottom && !q.Collide(f,-Vector2.UnitY)) f.y--;
      float dir = force.X!=0?Math.Sign(force.X):1;
      while(f.w<Width){
        if(!q.Collide(f,dir*Vector2.UnitX)){
          f.expandH(dir);
        } else if(!q.Collide(f,-dir*Vector2.UnitX)){
          f.expandH(-dir);
        } else if(f.top<p.Bottom){
          f.y++;
        } else goto doneFail;
        if(force.X==0) dir=-dir;
      }
      dir = force.X!=0?Math.Sign(force.X):1;
      int vdir = -1;
      while(f.h<Height){
        if(q.Collide(f,Vector2.UnitY*vdir)){
          float clearRight = bounds.right-f.right;
          float clearLeft = f.left-bounds.left;
          for(int i=1; i<(dir>0?clearRight:clearLeft); i++){
            if(!q.Collide(f,new Vector2(dir*i,vdir))){
              f.x+=dir*i;
              goto next;
            }
          }
          for(int i=1; i<(dir>0?clearLeft:clearRight); i++){
            if(!q.Collide(f,new Vector2(-dir*i,vdir))){
              f.x-=dir*i;
              goto next;
            }
          }
          if(vdir == 1)break;
          else{
            vdir = 1;
            noGravityTimer = 0.1f;
            continue;
          }
        }
        next:
          if(vdir == 1) f.expandDown(1);
          else f.expandUp(1);
      }
      if(f.h<Height){
        DebugConsole.Write("Failed to find good place for holdable");
        f.expandDown(Height-f.h);
      }
      Top = f.top;
      Left = f.left;
      //while()
      doneFail:
      done:
        Hold.cannotHoldTimer=Hold.cannotHoldDelay;
        Hold.Holder = null;
        Position=Position.Round();
    }
    RemoveTag(Tags.Persistent);
    if(!keepCollidableAlways) te.setCollidability(true);
    if(te==null) return;
    foreach (Entity e in te.GetChildren<Entity>()){
      e.RemoveTag(Tags.Persistent);
      if(e is IPickupChild pc) pc.OnRelease(p, force);
      if(e is IBoundsHaver h) h.bounds = new FloatRect(SceneAs<Level>().Bounds);
    }
    force = force*200f;
    if (force.X != 0f && force.Y == 0f){
      force.Y = -0.4f*200;
    }
    if(customThrowspeeds.Length>0) force.X = Math.Sign(force.X)*customThrowspeeds[0];
    if(customThrowspeeds.Length>1 && force.X!=0) force.Y = -customThrowspeeds[1];
    Speed = force;
    if (Speed != Vector2.Zero){
      noGravityTimer = 0.1f;
    }
    DebugConsole.Write("thrown speed: ",Speed);
  }
  void OnCollideH(CollisionData data){
    if (data.Hit is DashSwitch){
      (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * MathF.Sign(Speed.X));
    }
    Audio.Play(wallhitsound, Position);
    Speed.X *= -wallhitKeepspeed;
  }
  void OnCollideV(CollisionData data){
    if (data.Hit is DashSwitch){
      (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
    }
    Audio.Play(wallhitsound, Position);
    if (Speed.Y > 140f && !(data.Hit is DashSwitch)) Speed.Y*= 0.6f;
    else Speed.Y=0;
  }
  bool resetting;
  void Reset(bool particles = true){
    if(resetting){
      DebugConsole.Write("Called multiple times to reset routine (bad)");
    }
    resetting = true;
    Collidable = false;
    Position = lpos =origpos;
    Speed = Vector2.Zero;
    te.destroy(particles);
    Mysolids.Clear();
    te = null;
    if(!respawning) RemoveSelf();
    else Add(new Coroutine(respawnRoutine()));
  }
  IEnumerator respawnRoutine(){
    yield return respawndelay;
    hasBeenTouched=false;
    if(Scene!=null)make(Scene);
    Collidable = true;
    resetting = false;
  }
  public void OnSquish2(CollisionData data){
    if(inRelpos || Mysolids.Contains(data.Hit) || Mysolids.Contains(data.Pusher)) return;
    DebugConsole.Write($"squished: {data.Pusher} {data.Hit}");
    DebugConsole.Write("colliders", new FloatRect(this), new FloatRect(data.Pusher));
    // if(data.Pusher.Get<ChildMarker>()?.parent is Template t && new DynamicData(t).TryGet<TemplateHoldable>("ausp_holdable", out var th)){
    //   DebugConsole.Write($"Other",th, th.Position, new FloatRect(th));
    //   DebugConsole.Write("collide this st->other",te.fgt.CollideCheck(th));
    //   DebugConsole.Write("collide this->other st",t.fgt.CollideCheck(this));
    // }
    Reset();
  }
  CollisionExtensions.CachedCollision colLeft = new();
  CollisionExtensions.CachedCollision colRight = new();
  CollisionExtensions.CachedCollision colCenter = new();
  public override void Update(){
    base.Update();
    if(resetting) return;
    te.ownLiftspeed = Hold.IsHeld? Hold.Holder.Speed:Speed;
    if (Hold.IsHeld){
      prevLiftspeed = Vector2.Zero;
      if(actualDepth>=Hold.Holder.actualDepth)Depth = Hold.Holder.Depth-1;
    } else {
      //Position not being a whole number causes very annoying errors elsewhere
      Position = Position.Round();
      te.setCollidability(false);
      //DebugConsole.Write($"out update: {Position}");
      if(this.OnGroundCached(ref colCenter)){
        hasBeenTouched=true;
        float target = (!this.OnGroundCached(ref colRight, Position + Vector2.UnitX * 3f))? 20f: 
                       (!this.OnGroundCached(ref colLeft, Position - Vector2.UnitX * 3f) ? -20f:0);
        Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
        if (LiftSpeed == Vector2.Zero && prevLiftspeed != Vector2.Zero &&!dontFlingOff){
          Speed = prevLiftspeed;
          prevLiftspeed = Vector2.Zero;
          Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
          if (Speed.X != 0f && Speed.Y == 0f) Speed.Y=-60;
          if (Speed.Y < 0f) noGravityTimer = 0.15f;
        } else {
          prevLiftspeed = LiftSpeed;
          if (LiftSpeed.Y < 0f && Speed.Y < 0f &&!dontFlingOff) Speed.Y=0;
        }
      } else if(Hold.ShouldHaveGravity) {
        prevLiftspeed = (LiftSpeed = Vector2.Zero);
        float num = gravity;
        if(!hasBeenTouched && startFloating) num=0;
        if (Math.Abs(Speed.Y) <= 30f) num*=0.5f;
        float num2 = friction;
        if (Speed.Y < 0f) num2*=0.5f;

        Speed.X = Calc.Approach(Speed.X, 0f, num2 * Engine.DeltaTime);
        if (noGravityTimer > 0f)noGravityTimer-=Engine.DeltaTime;
        else Speed.Y = Calc.Approach(Speed.Y, terminalvel, num * Engine.DeltaTime);
      }
      inRelpos = true;
      MoveH(Speed.X * Engine.DeltaTime, OnCollideH);
      MoveV(Speed.Y * Engine.DeltaTime, OnCollideV);
      inRelpos = false;
      te.setCollidability(true);
    }
    if(dietobarrier) foreach (SeekerBarrier entity in base.Scene.Tracker.GetEntities<SeekerBarrier>()){
      entity.Collidable = true;
      bool res = CollideCheck(entity);
      entity.Collidable = false;
      if (res){
        if (Hold.IsHeld){
          Vector2 speed2 = Hold.Holder.Speed;
          Hold.Holder.Drop();
          Speed = speed2 * 0.333f;
          Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        }
        Audio.Play("event:/new_content/game/10_farewell/glider_emancipate", Position);
        Reset();
        break;
      }
    }
    if(Position.Y>SceneAs<Level>().Bounds.Bottom+voidDieOffset){
      Reset();
    }
    if(te!=null)fixPosition();
    Hold.CheckAgainstColliders();
  }


  void fixPosition(){
    if(lpos==Position) return;
    bool flag = keepCollidableAlways && !te.getSelfCol();
    //if(flag) te.setCollidability(true);
    touched();
    lpos = Position;
    if(inRelpos) throw new Exception("yell at me on discord please (this shouldn't ever happen but im paranoid)");

    bool origAllowPushing = AllowPushing; bool origIngorejt = IgnoreJumpThrus;
    inRelpos = true; AllowPushing = false; IgnoreJumpThrus = true;
    bool origHolderAllowPushing = false; bool orignaiiveinv = false; bool origHolderIgnorejtinv=false;
    if(keepCollidableAlways && Hold.IsHeld && Hold.Holder!=null){
      origHolderAllowPushing = Hold.Holder.AllowPushing;
      orignaiiveinv = !Hold.Holder.TreatNaive;
      origHolderIgnorejtinv = !Hold.Holder.IgnoreJumpThrus;
      Hold.Holder.TreatNaive = true;
      Hold.Holder.IgnoreJumpThrus = true;
      Hold.Holder.AllowPushing = false;
    }
    te.Position = Position+hoffset;
    te.ownLiftspeed = Hold.IsHeld? Hold.Holder.Speed:(Speed+prevLiftspeed);
    te.childRelposSafe();

    if(origHolderAllowPushing && Hold.Holder!=null) Hold.Holder.AllowPushing = true;
    if(orignaiiveinv && Hold.Holder!=null) Hold.Holder.TreatNaive = false;
    if(origHolderIgnorejtinv && Hold.Holder!=null) Hold.Holder.IgnoreJumpThrus = false;
    inRelpos = false; AllowPushing = origAllowPushing; IgnoreJumpThrus = origIngorejt;
    Position = lpos;
    if(flag) te.setCollidability(false);
  }
  bool inRelpos=false;
  public static bool PickupHook(On.Celeste.Player.orig_Pickup orig, Player self, Holdable hold){
    bool ret =  orig(self, hold);
    if(ret && hold.Entity is TemplateHoldable t){
      self.minHoldTimer = t.minHoldTimer;
    }
    return ret;
  }
  public static bool MoveHHook(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int amount, Collision cb, Solid pusher){
    //DebugConsole.Write($"HereH", self, amount, pusher, self.Scene.TimeActive, self.Position.X, jumpthruMoving);
    if(/*(pusher != null || jumpthruMoving>0) &&*/ self is TemplateHoldable s && s.te!=null){
      if(pusher?.Get<ChildMarker>()?.propagatesTo(s.te)??false) return false;
      bool flag = s.te.getSelfCol();
      if(flag)s.te.setCollidability(false);
      bool res = orig(self,amount,cb,pusher);
      if(s.resetting) return res;
      if(flag)s.te.setCollidability(true);
      if(s.moveImmediately && !s.inRelpos) s.fixPosition();
      return res;
    } else{
      return orig(self, amount, cb, pusher);
    }
  }
  public static bool MoveVHook(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int amount, Collision cb, Solid pusher){
    if(/*(pusher != null || jumpthruMoving>0) &&*/ self is TemplateHoldable s && s.te!=null){
      if(pusher?.Get<ChildMarker>()?.propagatesTo(s.te)??false) return false;
      bool flag = s.te.getSelfCol();
      if(flag)s.te.setCollidability(false);
      bool res = orig(self,amount,cb,pusher);
      if(s.resetting) return res;
      if(flag)s.te.setCollidability(true);
      if(s.moveImmediately && !s.inRelpos) s.fixPosition();
      return res;
    } else{
      return orig(self, amount, cb, pusher);
    }
  }
  public static void PlayerUpdateHook(On.Celeste.Player.orig_Update orig, Player p){
    TemplateHoldable flag = null;
    if(p.Holding != null && p.Holding.Entity is TemplateHoldable th && th.keepCollidableAlways){
      th.te?.setCollidability(false);
      flag = th;
    }
    orig(p);
    if(flag!=null){
      flag.te?.setCollidability(true);
    }
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.Pickup += PickupHook;
    On.Celeste.Actor.MoveHExact+=MoveHHook;
    On.Celeste.Actor.MoveVExact+=MoveVHook;
    On.Celeste.Player.Update+=PlayerUpdateHook;
    ICustomHoldableRelease.hooks.enable();
  },()=>{
    On.Celeste.Player.Pickup -= PickupHook;
    On.Celeste.Actor.MoveHExact-=MoveHHook;
    On.Celeste.Actor.MoveVExact-=MoveVHook;
    On.Celeste.Player.Update-=PlayerUpdateHook;
  },auspicioushelperModule.OnEnterMap);
  public override string ToString() {
    return base.ToString()+this.GetHashCode();
  }
}
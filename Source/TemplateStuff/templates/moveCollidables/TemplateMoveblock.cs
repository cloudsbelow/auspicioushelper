



using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/TemplateMoveblock")]
public class TemplateMoveBlock:TemplateMoveCollidable{
  public Vector2 movedir;
  public float maxspeed;
  public float acceleration;
  bool respawning;
  float maxrespawntimer;
  float maxStuckTime;
  bool cansteer;
  int maxleniency;
  Vector2 origpos;
  string moveevent = "event:/game/04_cliffside/arrowblock_move";
  List<Vector2> decalOffset = new();
  List<MTexture> arrows;
  public TemplateMoveBlock(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateMoveBlock(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    movedir = d.Attr("direction") switch{
      "down"=> Vector2.UnitY,
      "up"=>-Vector2.UnitY,
      "left"=>-Vector2.UnitX,
      "right"=>Vector2.UnitX,
      _=>Vector2.UnitX
    };
    useOwnUncollidable = d.Bool("uncollidable_blocks",false);
    maxspeed = d.Float("speed",75);
    acceleration = d.Float("acceleration", 300);
    respawning = d.Bool("respawning",true);
    maxrespawntimer = d.Float("respawn_timer",2f);
    maxStuckTime = d.Float("Max_stuck",0.15f);
    cansteer = d.Bool("cansteer", false);
    maxleniency = d.Int("max_leniency",4);
    moveevent = d.Attr("movesfx","event:/game/04_cliffside/arrowblock_move");
    if(d.Nodes!=null) foreach(Vector2 node in d.Nodes){
      decalOffset.Add(node-d.Position);
    }
    origpos = Position;
    prop &= ~Propagation.Riding;
    Depth = d.Int("decal_depth",-10001)+depthoffset;
    if(decalOffset.Count>0) {
      arrows = GFX.Game.GetAtlasSubtextures("objects/auspicioushelper/templates/movearrows/"+d.Attr("arrow_texture","small"));
      Visible = true;
    }
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    Add(movesfx = new SoundSource());
    Add(new Coroutine(Sequence()));
  }

  SoundSource movesfx;
  Vector2 lastmovevec;
  IEnumerator Sequence(){
    float stuckTimer;
    float speed;
    float steerdelay;
    waiting:
      stuckTimer = maxStuckTime;
      steerdelay = 0.2f;
      while(!triggered && !hasRiders<Player>()) yield return null;
      disconnect();
      triggered=true;
      speed = 0;
      Audio.Play("event:/game/04_cliffside/arrowblock_activate", Position);
      shake(0.2f);
      yield return 0.2f;
      movesfx.Play(moveevent);
      movesfx.Param("arrow_stop", 0f);
    moving:
      yield return null;
      speed = Calc.Approach(speed,maxspeed,acceleration*Engine.DeltaTime);
      Query qs = getq(new Vector2(speed*Engine.DeltaTime+maxleniency,speed*Engine.DeltaTime+maxleniency).Ceiling());
      bool collideflag;
      Vector2 movevec = movedir;
      Player p = Scene.Tracker.GetEntity<Player>();
      if(movedir.Y==0){
        if(cansteer && p != null && p.StateMachine.state == 0 && Input.MoveY.Value!=0 && hasRiders<Player>()){
          if(steerdelay>0){
            steerdelay-=Engine.DeltaTime;
          } else {
            movevec.Y = Input.MoveY.Value;
          }
        } else {
          steerdelay=0.1f;
        }
        lastmovevec = movevec;
        movevec.Normalize();
        ownLiftspeed = movevec*speed;
        collideflag = MoveHCollide(qs,speed*movevec.X*Engine.DeltaTime,maxleniency,ownLiftspeed);
        if(collideflag) ownLiftspeed.X=0;
        if(MoveVCollide(qs,speed*movevec.Y*Engine.DeltaTime,0,ownLiftspeed)) ownLiftspeed.Y=0;
      } else {
        if(cansteer && p != null && p.StateMachine.state == 1 && Input.MoveX.Value!=0 && hasRiders<Player>()){
          if(steerdelay>0){
            steerdelay-=Engine.DeltaTime;
          } else {
            movevec.X = Input.MoveX.Value;
          }
        } else {
          steerdelay=0.1f;
        }
        lastmovevec=movevec;
        movevec.Normalize();
        ownLiftspeed = movevec*speed;
        collideflag = MoveVCollide(qs,speed*movevec.Y*Engine.DeltaTime,maxleniency,ownLiftspeed);
        if(collideflag) ownLiftspeed.Y=0;
        if(MoveHCollide(qs,speed*movevec.X*Engine.DeltaTime,0,ownLiftspeed)) ownLiftspeed.X=0;
      }
      if(collideflag){
        movesfx.Param("arrow_stop", 1f);
        if(stuckTimer>0) stuckTimer-=Engine.DeltaTime;
        else goto blocked;
      } else {
        movesfx.Param("arrow_stop", 0f);
        stuckTimer = maxStuckTime;
      }
      float tau = MathF.PI * 2f;
      int num = (int)Math.Floor((0f - (movevec * new Vector2(-1f, 1f)).Angle() + tau) % tau / tau * 8f + 0.5f);
      movesfx.Param("arrow_influence", num + 1);
      goto moving;
    blocked:
      movesfx.Stop();
      shake(0.2f);
      Audio.Play("event:/game/04_cliffside/arrowblock_break", Position);
      yield return 0.2f;
      gone=true;
      if(!respawning){
        destroy(true);
        yield break;
      } 
      destroyChildren();
      triggered=false;
      fgt = null;
      yield return maxrespawntimer;
      reconnect(origpos);
      remake();
      gone=false;
      yield return null;
      Audio.Play("event:/game/04_cliffside/arrowblock_reappear", Position);
      goto waiting;
  }
  bool gone;
  public override void Update() {
    lastmovevec = movedir;
    base.Update();
  }
  public override void Render() {
    base.Render();
    if(gone) return;
    float tau = MathF.PI * 2f;
    int dir = (int)Math.Floor((0f - lastmovevec.Angle() + tau) % tau / tau * 8f + 0.5f);
    Vector2 vec = virtLoc+gatheredShakeVec;
    foreach(Vector2 o in decalOffset){
      arrows[Calc.Clamp(dir, 0, 7)].DrawCentered(o+vec);
    }
  }
  public override void Removed(Scene scene) {
    base.Removed(scene);
    movesfx?.Stop();
  }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

internal static class TileHooks{
  static Rectangle hookAnimT(On.Celeste.AnimatedTiles.orig_GetClippedRenderTiles orig, AnimatedTiles a, int extend){
    Rectangle r = orig(a,extend);
    if(a.Entity is IBoundsHaver e){
      return e.GetTilebounds(a.Entity.Position+a.Position, r);
    }
    return r;
  }
  static Rectangle hookTiles(On.Monocle.TileGrid.orig_GetClippedRenderTiles orig, TileGrid a){
    Rectangle r = orig(a);
    if(a.Entity is IBoundsHaver e){
      return e.GetTilebounds(a.Entity.Position+a.Position, r);
    }
    return r;
  }
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.AnimatedTiles.GetClippedRenderTiles+=hookAnimT;
    On.Monocle.TileGrid.GetClippedRenderTiles+=hookTiles;
  },void ()=>{
    On.Celeste.AnimatedTiles.GetClippedRenderTiles-=hookAnimT;
    On.Monocle.TileGrid.GetClippedRenderTiles-=hookTiles;
  },auspicioushelperModule.OnEnterMap);
}

public interface IBoundsHaver{
  FloatRect bounds {get; set;}
  public Rectangle GetTilebounds(Vector2 loc, Rectangle isect){
    Vector2 tlc = ((bounds.tlc-loc)/8).Floor();
    Vector2 brc = ((bounds.brc-loc)/8).Ceiling();
    FloatRect levelclip = FloatRect.fromCorners(tlc, brc);
    levelclip.expandAll(0);
    return levelclip._intersect(new FloatRect(isect)).munane();
  }
}

internal class BgTiles:BackgroundTiles, ISimpleEnt, IBoundsHaver{
  public Template.Propagation prop => Template.Propagation.Shake;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public FloatRect bounds {get;set;}
  public BgTiles(templateFiller t, Vector2 posoffset, int depthoffset):base(posoffset+t.offset, t.bgt){
    toffset = t.offset;
    Depth+=depthoffset;
    TileHooks.hooks.enable();
  }
  public void setOffset(Vector2 ppos){}
  public void relposTo(Vector2 loc, Vector2 liftspeed){
    Position = loc+toffset;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    bounds = new FloatRect(SceneAs<Level>().Bounds);
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0)Collidable = col>0;
    if(act!=0)Active = act>0;
  }
}

[Tracked]
internal class FgTiles:SolidTiles, ISimpleEnt, IBoundsHaver, IChildShaker{
  public Template.Propagation prop=> Template.Propagation.All;
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public FloatRect bounds {get;set;}
  public FgTiles(templateFiller t, Vector2 posoffset, int depthoffset):base(posoffset+t.offset, t.fgt){
    toffset = t.offset;
    Depth+=depthoffset;
    TileHooks.hooks.enable();
    tileTypes = t.fgt;
    OnDashCollide = (Player p, Vector2 dir)=>((ITemplateChild) this).propagateDashhit(p,dir);
    if(PartialTiles.usingPartialtiles){
      Collider = new MiptileCollider(t.FgMipgrid, Vector2.One);
    }
    RemoveTag(Tags.Global);
  }
  public void setOffset(Vector2 ppos){
    Add(new ChildMarker(parent));
  }
  public override void Added(Scene scene){
    base.Added(scene);
    bounds = new FloatRect(SceneAs<Level>().Bounds);
  }
  public bool hasRiders<T>() where T:Actor{
    if(Scene==null)DebugConsole.Write("I hate hate hate hate hate hate hate you");
    if(typeof(T) == typeof(Player)) return UpdateHook.cachedPlayer?.IsRiding(this)??false;
    foreach(T a in Scene.Tracker.GetEntities<T>()){
      if(a.IsRiding(this)) return true;
    }
    return false;
  }
  public bool hasInside(Actor a){
    return Collider.Collide(a.Collider);
  }
  public void relposTo(Vector2 loc, Vector2 liftspeed){
    MoveTo(loc+toffset, liftspeed);
  }
  public void destroy(bool particles){
    Vector2 ls = parent.gatheredLiftspeed;
    if(particles){
      Rectangle bounds = (this as IBoundsHaver).GetTilebounds(Position,AnimatedTiles.GetClippedRenderTiles(32));
      Vector2 ppos = parent?.virtLoc??Center;
      for(int i=bounds.X; i<bounds.X+bounds.Width; i++){
        for(int j=bounds.Y; j<bounds.Height; j++){
          char tile = tileTypes[i,j];
          if(tile == '0' || tile == '\0') continue;
          Vector2 offset = new Vector2(i*8+4,j*8+4);
          Scene.Add(Engine.Pooler.Create<TileDebris>().Init(Position+offset,tile).RandFrom(ls));
        }
      }
    }
    RemoveSelf();
  }
  public void fakeDestroy(){
    Vector2 ls = parent.gatheredLiftspeed;
    Rectangle bounds = (this as IBoundsHaver).GetTilebounds(Position,AnimatedTiles.GetClippedRenderTiles(32));
    Vector2 ppos = parent?.virtLoc??Center;
    for(int i=bounds.X; i<bounds.X+bounds.Width; i++){
      for(int j=bounds.Y; j<bounds.Height; j++){
        char tile = tileTypes[i,j];
        if(tile == '0' || tile == '\0') continue;
        Vector2 offset = new Vector2(i*8+4,j*8+4);
        Scene.Add(Engine.Pooler.Create<TileDebris>().Init(Position+offset,tile).RandFrom(ls));
      }
    }
  }

  public override void MoveHExact(int move){
    GetRiders();
    Player player = null;
    player = base.Scene.Tracker.GetEntity<Player>();
    //What the heck does this do?
    if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && 
    !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY)){
      player.MoveV(1f);
    }

    base.X += move;
    MoveStaticMovers(Vector2.UnitX * move);
    if(!Collidable) return;
    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()){
      if (!entity.AllowPushing) continue;
      bool collidable = entity.Collidable;
      entity.Collidable = true;
      if (!entity.TreatNaive && CollideCheck(entity, Position)){
        Collidable = false;
        for(int i=0; i<Math.Abs(move); i++){
          if(!CollideCheck(entity, Position) || entity.MoveHExact(Math.Sign(move), entity.SquishCallback, this)) break;
        }
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      } else if (riders.Contains(entity)) {
        Collidable = false;
        if (entity.TreatNaive) entity.NaiveMove(Vector2.UnitX * move);
        else entity.MoveHExact(move);
        entity.LiftSpeed = LiftSpeed;
        Collidable = true;
      }
      entity.Collidable = collidable;
    }  
    riders.Clear();
  }
  public override void MoveVExact(int move){
    GetRiders();
    base.Y += move;
    MoveStaticMovers(Vector2.UnitY * move);
    if(!Collidable) return;
    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()){
      if (!entity.AllowPushing) continue;
      bool collidable = entity.Collidable;
      entity.Collidable = true;
      if (!entity.TreatNaive && CollideCheck(entity, Position)){
          Collidable = false;
          for(int i=0; i<Math.Abs(move); i++){
            if(!CollideCheck(entity, Position) || entity.MoveVExact(Math.Sign(move), entity.SquishCallback, this)) break;
          }
          entity.LiftSpeed = LiftSpeed;
          Collidable = true;
      } else if (riders.Contains(entity)){
          Collidable = false;
          if (entity.TreatNaive) entity.NaiveMove(Vector2.UnitY * move);
          else entity.MoveVExact(move);
          entity.LiftSpeed = LiftSpeed;
          Collidable = true;
      }
      entity.Collidable = collidable;
    }
    riders.Clear();
  }
  public override void Awake(Scene scene){
    base.Awake(scene);
    foreach (StaticMover smover in scene.Tracker.GetComponents<StaticMover>()){
      if (smover.Platform == null && smover.IsRiding(this)){
        staticMovers.Add(smover);
        smover.Platform = this;
        if (smover.OnAttach != null){
          smover.OnAttach(this);
        }
      }
    }
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0)Collidable = col>0;
    if(act!=0)Active = act>0;
    if(col>0) EnableStaticMovers();
    else if(col<0) DisableStaticMovers();
  }
  public Vector2 lastShake {get;set;} = Vector2.Zero;
}

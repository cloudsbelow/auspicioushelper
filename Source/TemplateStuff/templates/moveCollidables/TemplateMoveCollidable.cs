


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

public interface ITemplateTriggerable{
  void OnTrigger(StaticMover s);
}
public class TemplateMoveCollidable:TemplateDisappearer, ITemplateTriggerable{ 
  public override Vector2 gatheredLiftspeed => dislocated?ownLiftspeed:base.gatheredLiftspeed;
  public bool triggered;
  Vector2 movementCounter;
  public Vector2 exactPosition=>Position+movementCounter;
  public override Vector2 virtLoc => dislocated?Position.Round():Position;
  public bool useOwnUncollidable = false;
  public TemplateMoveCollidable(EntityData data, Vector2 pos, int depthoffset):base(data,pos,depthoffset){
    Position = Position.Round();
    movementCounter = Vector2.Zero;
    prop &= ~Propagation.Riding; 
    triggerHooks.enable();
  }
  bool dislocated = false;
  public bool detatched=>dislocated;
  
  MipGridCollisionCacher mgcc = new();
  public override void relposTo(Vector2 loc, Vector2 liftspeed) {
    if(!dislocated) base.relposTo(loc,liftspeed);
  }
  public virtual void disconnect(){
    Position = Position.Round();
    childRelposSafe();
    dislocated = true;
    prop &= ~Propagation.Shake;
  }
  public virtual void reconnect(Vector2? fallback=null){
    dislocated = false;
    prop |= Propagation.Shake;
    bool old = getSelfCol();
    setCollidability(false);
    relposTo(parent?.virtLoc??fallback??Position, Vector2.Zero);
    setCollidability(old);
  }
  public class QueryBounds {
    public List<FloatRect> rects=new();
    public List<MipGrid> grids=new();
    public bool Collide(FloatRect o, Vector2 offset){
      float nx = MathF.Round(o.x)+offset.X;
      float ny = MathF.Round(o.y)+offset.Y;
      foreach(var rect in rects){
        if(rect.CollideExRect(nx,ny,o.w,o.h)) return true;
      }
      FloatRect n = new FloatRect(nx,ny,o.w,o.h);
      foreach(var grid in grids){
        if(grid.collideFrOffset(n,offset)) return true;
      }
      return false;
    }
    public bool Collide(MipGrid g, Vector2 offset){
      foreach(var grid in grids){
        if(grid.collideMipGridOffset(g,offset)) return true;
      }
      foreach(var rect in rects){
        if(g.collideFrOffset(rect, -offset)) return true;
      }
      return false;
    }
    public bool Collide(QueryIn q, Vector2 offset){
      foreach(var g in q.grids) if(Collide(g,offset)) return true;
      foreach(var r in q.rects) if(Collide(r,offset)) return true;
      return false;
    }
  }
  public class QueryIn{
    public List<FloatRect> rects=new();
    public List<MipGrid> grids=new();
    public FloatRect bounds = FloatRect.empty;
    public HashSet<Solid> gotten;
    public bool Collide(FloatRect f){
      if(!bounds.CollideFr(f)) return false;
      foreach(var g in grids) if(g.collideFr(f)) return true;
      foreach(var r in rects) if(r.CollideFr(f)) return true;
      return false;
    }
  }
  QueryBounds getQinfo(FloatRect f, HashSet<Solid> exclude){
    QueryBounds res  =new();
    foreach(Solid s in Scene.Tracker.GetEntities<Solid>()){
      if(!s.Collidable || exclude.Contains(s)) continue;
      FloatRect coarseBounds = new FloatRect(s);
      if(s.Collider is Hitbox h && f.CollideFr(coarseBounds)) res.rects.Add(coarseBounds);
      if(s.Collider is Grid g && f.CollideFr(coarseBounds)) res.grids.Add(MipGrid.fromGrid(g));
    }
    return res;
  }
  public QueryIn getQself(){
    QueryIn res = new();
    FloatRect bounds = FloatRect.empty;
    var all = GetChildren<Solid>(Propagation.Shake);
    res.gotten = new(all);
    foreach(Solid s in all){
      if(useOwnUncollidable || s.Collidable){
        FloatRect coarseBounds = new FloatRect(s);
        if(s.Collider is Grid g) res.grids.Add(MipGrid.fromGrid(g));
        else if(s.Collider is Hitbox f) res.rects.Add(coarseBounds);
        else continue; 
        bounds = bounds._union(coarseBounds);
      }
    }
    res.bounds = bounds;
    return res;
  }
  public Vector2 TestMove(QueryBounds q, QueryIn s, int amount, Vector2 dirvec){
    int dir = Math.Sign(amount);
    int i = 0;
    while(i!=amount){
      if(q.Collide(s,dirvec*(i+dir))) return dirvec*i;
      i+=dir;
    }
    return dirvec*amount;
  }
  public bool TestMove(Query qs, int amount, Vector2 dirvec)=>amount==0||TestMove(qs.q,qs.s,amount,dirvec)!=Vector2.Zero;
  public Vector2 TestLeniency(QueryBounds q, QueryIn s, Vector2 ioffset, int maxLeniency, Vector2 leniencyVec){
    for(int i=1; i<=maxLeniency; i++){
      for(int j=-1; j<=1; j+=2){
        if(!q.Collide(s,i*j*leniencyVec+ioffset)) return i*j*leniencyVec+ioffset;
      }
    }
    return Vector2.Zero;
  }
  public bool TestMoveLeniency(QueryBounds q, QueryIn s, int amount, Vector2 dirvec, int maxLeniency, Vector2 leniencyVec, out Vector2 loc){
    if(amount == 0){
      if(q.Collide(s,Vector2.Zero)){
        loc = TestLeniency(q,s,Vector2.Zero,maxLeniency,leniencyVec);
        return loc==Vector2.Zero;
      }
      loc = Vector2.Zero;
      return false;
    }
    var tryMove = TestMove(q,s,amount,dirvec);
    if(tryMove != Vector2.Zero){
      loc = tryMove;
      return true;
    } else {
      loc = TestLeniency(q,s,dirvec*Math.Sign(amount),maxLeniency,leniencyVec);
      return loc==Vector2.Zero;
    }
  }
  public Vector2 TestMoveLeniency(QueryBounds q, QueryIn s, int amount, Vector2 dirvec, int maxLeniency, Vector2 leniencyVec){
    bool res = TestMoveLeniency(q,s,amount,dirvec,maxLeniency,leniencyVec, out var v);
    return v;
  }
  // public bool MoveBy(Vector2 amount, Vector2 liftspeed, Vector2? leniencyVec, int? maxLeniency){
  //   Vector2 leni = leniencyVec??Vector2.Zero;
  //   movementCounter+=amount;
  //   if(exactPosition.Round()!=Position){
  //     Vector2 delta = exactPosition.Round()-Position;
  //     var ml = maxLeniency??0;
  //     bool res;
  //     QueryIn s = getQself();
  //     Vector2 ex = amount.Abs()+leni.Abs()*ml;
  //     QueryBounds q = getQinfo(s.bounds._expand(ex.X+1,ex.Y+1),s.gotten);
  //     if(ml == 0){
  //       Vector2 npos = 
  //     }
  //   }
  //   return true;
  // }
  public bool MoveHCollideExact(QueryBounds q, QueryIn s, int amount, int leniency, Vector2 liftspeed){
    Vector2 v = leniency==0? TestMove(q,s,amount,new Vector2(1,0)) : TestMoveLeniency(q,s,amount,new Vector2(1,0),leniency,new Vector2(0,1));
    if(v!=Vector2.Zero){
      Position+=v;
      childRelposTo(virtLoc,liftspeed);
      return false;
    }
    return true;
  }
  public bool MoveHCollideExact(Query qs, int amount, int leniency, Vector2 liftspeed)=>MoveHCollideExact(qs.q,qs.s,amount,leniency,liftspeed);
  public bool MoveVCollideExact(QueryBounds q, QueryIn s, int amount, int leniency, Vector2 liftspeed){
    Vector2 v = leniency==0? TestMove(q,s,amount,new Vector2(0,1)) : TestMoveLeniency(q,s,amount,new Vector2(0,1),leniency,new Vector2(1,0));
    if(v!=Vector2.Zero){
      Position+=v;
      childRelposTo(virtLoc,liftspeed);
      return false;
    }
    return true;
  }
  public bool MoveVCollideExact(Query qs, int amount, int leniency, Vector2 liftspeed)=>MoveVCollideExact(qs.q,qs.s,amount,leniency,liftspeed);
  public bool MoveHCollide(QueryBounds q, QueryIn s, float amount, int leniency, Vector2 liftspeed){
    if(Math.Sign(movementCounter.X)!=Math.Sign(amount)) movementCounter.X = (float)Math.Clamp(movementCounter.X,-0.49,0.49);
    movementCounter.X+=amount;
    int dif = (int)Math.Round(movementCounter.X);
    bool fail = dif!=0 && MoveHCollideExact(q,s,dif,leniency,liftspeed);
    if(!fail) movementCounter.X-=dif;
    else movementCounter.X = (float)Math.Clamp(movementCounter.X, -0.501,0.501);
    return fail;
  }
  public bool MoveHCollide(Query qs, float amount, int leniency, Vector2 liftspeed)=>MoveHCollide(qs.q,qs.s,amount,leniency,liftspeed);
  public bool MoveVCollide(QueryBounds q, QueryIn s, float amount, int leniency, Vector2 liftspeed){
    if(Math.Sign(movementCounter.Y)!=Math.Sign(amount)) movementCounter.Y = (float)Math.Clamp(movementCounter.Y,-0.49,0.49);
    movementCounter.Y+=amount;
    int dif = (int)Math.Round(movementCounter.Y);
    bool fail = dif!=0 && MoveVCollideExact(q,s,dif,leniency,liftspeed);
    if(!fail) movementCounter.Y-=dif;
    else movementCounter.Y = (float)Math.Clamp(movementCounter.Y, -0.501,0.501);
    return fail;
  }
  public bool MoveVCollide(Query qs, float amount, int leniency, Vector2 liftspeed)=>MoveVCollide(qs.q,qs.s,amount,leniency,liftspeed);
  public class Query{
    public QueryBounds q;
    public QueryIn s;
    public Query(QueryBounds q, QueryIn s){
      this.q=q; this.s=s;
    }
  }
  public Query getq(Vector2 maxpotentialmovemagn){
    QueryIn s = getQself();
    Vector2 v = maxpotentialmovemagn.Abs().Ceiling();
    HashSet<Solid> toExclude = new(s.gotten);
    foreach(Solid p in s.gotten){
      foreach(StaticMover sm in p.staticMovers){
        if(sm.Entity is TemplateStaticmover smt){
          foreach(Solid sl in smt.GetChildren<Solid>(Propagation.Shake)) toExclude.Add(sl);
        }
      }
    }
    QueryBounds q = getQinfo(s.bounds._expand(v.X,v.Y),toExclude);
    return new(q,s);
  }

  public virtual void OnTrigger(StaticMover sm){
    triggered = true;
  }
  static void smtHook(On.Celeste.Platform.orig_OnStaticMoverTrigger orig, Platform p, StaticMover sm){
    if(p is ITemplateChild c){
      c.parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(sm);
    } else {
      ChildMarker m = p.Get<ChildMarker>();
      if(m!=null){
        m.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(sm);
      }
    }
    orig(p,sm);
  }
  public static HookManager triggerHooks = new HookManager(()=>{
    On.Celeste.Platform.OnStaticMoverTrigger+=smtHook;
  },()=>{
    On.Celeste.Platform.OnStaticMoverTrigger-=smtHook;
  },auspicioushelperModule.OnEnterMap); 
}
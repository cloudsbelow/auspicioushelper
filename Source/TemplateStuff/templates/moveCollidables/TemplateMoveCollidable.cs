


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;



public class TemplateMoveCollidable:TemplateDisappearer, ITemplateTriggerable{ 
  public override Vector2 gatheredLiftspeed => dislocated? ownLiftspeed+relocatorLiftspeed : base.gatheredLiftspeed;
  public bool triggered;
  Vector2 movementCounter;
  public Vector2 exactPosition=>Position+movementCounter;
  protected override Vector2 virtLoc => dislocated?Position.Round():Position;
  public bool useOwnUncollidable = false;
  bool hitJumpthrus;
  bool moveThroughDashblocks;
  public TemplateMoveCollidable(EntityData data, Vector2 pos, int depthoffset):base(data,pos,depthoffset){
    Position = Position.Round();
    movementCounter = Vector2.Zero;
    prop &= ~Propagation.Riding;
    hitJumpthrus = data.Bool("hitJumpthrus",false);
    moveThroughDashblocks = data.Bool("throughDashblocks",false);
    if(hitJumpthrus) MaddiesIop.hooks.enable();
  }
  bool dislocated = false;
  public bool detatched=>dislocated;
  
  public override void relposTo(Vector2 loc, Vector2 liftspeed) {
    if(!dislocated) base.relposTo(loc,liftspeed+ownLiftspeed);
  }
  public virtual void disconnect(){
    Position = Position.Round();
    childRelposSafe();
    dislocated = true;
    prop &= ~Propagation.Shake;
  }
  public void removeFromContainer(){
    parent?.GetFromTree<IRemovableContainer>()?.RemoveChild(this);
  }
  public virtual void reconnect(Vector2? fallback=null){
    dislocated = false;
    prop |= Propagation.Shake;
    bool old = getSelfCol();
    setCollidability(false);
    relposTo(parent?.roundLoc??fallback??Position, Vector2.Zero);
    setCollidability(old);
  }
  public class QueryBounds {
    public struct ColItem{
      public Collider c;
      public CollisionDirection dir;
      public ColItem(Collider c, CollisionDirection d=CollisionDirection.solid){
        this.c=c; this.dir=d;
      }
    }
    public List<ColItem> colliders=new();
    public HashSet<BreakableRect> breakable=null;
    public bool Collide(FloatRect r, Vector2 offset, CollisionDirection movedir=CollisionDirection.yes){
      foreach(var a in colliders) if((movedir&a.dir)!=0 && a.c.Collide(r.munane(Int2.Round(offset)))) return true;
      return false;
    }
    public bool Collide(Collider c, Vector2 offset, CollisionDirection movedir=CollisionDirection.yes){ 
      var ent = c.Entity;
      Vector2 oldpos = ent.Position;
      ent.Position = ent.Position+offset;
      foreach(var a in colliders) if((movedir&a.dir)!=0 && a.c.Collide(c)){
        ent.Position = oldpos;
        return true;
      }
      ent.Position = oldpos;
      return false;
    }
    public bool Collide(QueryIn q, Vector2 offset, CollisionDirection movedir){
      movedir = movedir|CollisionDirection.yes;
      foreach(var a in q.colliders) if(Collide(a,offset,movedir)) return true;
      return false;
    }
    public bool Collide(QueryIn q, Vector2 offset)=>Collide(q,offset,Util.getCollisionDir(offset));
  }
  public class QueryIn{
    public List<Collider> colliders = new();
    public FloatRect bounds = FloatRect.empty;
    public HashSet<Platform> gotten;
    public bool Collide(Collider c){
      if(c is Grid g) c=MiptileCollider.fromGrid(g);
      foreach(var a in colliders) if(a.Collide(c)) return true;
      return false;
    }
    public bool Collide(Entity e)=>Collide(e.Collider);
    public void BreakStuff(HashSet<BreakableRect> stuff, CollisionDirection dir){
      foreach(var desc in stuff){
        if(desc.toBreak.Collidable && (desc.dir&dir)!=0) if(Collide(desc.toBreak))desc.Break();
      }
    }
  }
  static Collider asMgUtil(Collider c)=>c is Grid g? MiptileCollider.fromGrid(g):c;
  public static QueryBounds getQinfo(FloatRect f, HashSet<Entity> exclude, Scene Scene){
    QueryBounds res  =new();
    foreach(Solid s in Scene.Tracker.GetEntities<Solid>()){
      if(!s.Collidable || exclude.Contains(s) || s.Collider is not {} c || !f.CollideFr(new FloatRect(s))) continue;
      res.colliders.Add(new(asMgUtil(c), CollisionDirection.solid));
    }
    return res;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void AddWithDir(List<Entity> from, FloatRect coarse, QueryBounds q, QueryIn s, HashSet<Entity> exclude, CollisionDirection d){
    foreach(var j in from){
      if(!j.Collidable || !coarse.CollideFr(new(j)) || exclude.Contains(j) || j.Collider is not {} c || s.Collide(c)) continue;
      q.colliders.Add(new(asMgUtil(c),d));
    }
  }
  public static void AddJumpthrus(FloatRect coarse, QueryBounds q, QueryIn s, HashSet<Entity> exclude, Scene Scene){
    AddWithDir(Scene.Tracker.GetEntities<JumpThru>(), coarse, q, s, exclude, CollisionDirection.up);
    if(MaddiesIop.dt!=null && Scene.Tracker.Entities.TryGetValue(MaddiesIop.dt,out var li)){
      AddWithDir(li, coarse, q, s, exclude, CollisionDirection.down);
    } 
    if(MaddiesIop.samah!=null && Scene.Tracker.Entities.TryGetValue(MaddiesIop.samah,out li)){
      AddWithDir(li, coarse, q, s, exclude, CollisionDirection.down);
    }
    if(MaddiesIop.jt!=null && Scene.Tracker.Entities.TryGetValue(MaddiesIop.jt, out li)) foreach(Entity j in li){
      if(!j.Collidable || !coarse.CollideFr(new(j)) || exclude.Contains(j) || j.Collider is not {} c || s.Collide(c)) continue;
      q.colliders.Add(new(asMgUtil(c),MaddiesIop.side.get(j)?CollisionDirection.right:CollisionDirection.left));
    }
  }
  public static QueryIn getQself(Template self, bool useUncol){
    QueryIn res = new();
    FloatRect bounds = FloatRect.empty;
    var all = self.GetChildren<Solid>(Propagation.Shake);
    res.gotten = new(all);
    foreach(Solid s in all){
      if(useUncol || s.Collidable){
        res.colliders.Add(asMgUtil(s.Collider));
        bounds = bounds._union(new FloatRect(s));
      }
    }
    res.bounds = bounds;
    return res;
  }
  public QueryIn getQself()=>getQself(this,useOwnUncollidable);
  public static Vector2 TestMove(QueryBounds q, QueryIn s, int amount, Vector2 dirvec){
    int dir = Math.Sign(amount);
    int i = 0;
    while(i!=amount){
      if(q.Collide(s,dirvec*(i+dir))) return dirvec*i;
      i+=dir;
    }
    return dirvec*amount;
  }
  public static bool TestMove(Query qs, int amount, Vector2 dirvec)=>amount==0||TestMove(qs.q,qs.s,amount,dirvec)!=Vector2.Zero;
  public static Vector2 TestLeniency(QueryBounds q, QueryIn s, Vector2 ioffset, int maxLeniency, Vector2 leniencyVec){
    for(int i=1; i<=maxLeniency; i++){
      for(int j=-1; j<=1; j+=2){
        if(!q.Collide(s,i*j*leniencyVec+ioffset)) return i*j*leniencyVec+ioffset;
      }
    }
    return Vector2.Zero;
  }
  public static bool TestMoveLeniency(QueryBounds q, QueryIn s, int amount, Vector2 dirvec, int maxLeniency, Vector2 leniencyVec, out Vector2 loc){
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
  public static Vector2 TestMoveLeniency(QueryBounds q, QueryIn s, int amount, Vector2 dirvec, int maxLeniency, Vector2 leniencyVec){
    bool res = TestMoveLeniency(q,s,amount,dirvec,maxLeniency,leniencyVec, out var v);
    return v;
  }
  public bool MoveHCollideExact(QueryBounds q, QueryIn s, int amount, int leniency){
    Vector2 v = leniency==0? TestMove(q,s,amount,new Vector2(1,0)) : TestMoveLeniency(q,s,amount,new Vector2(1,0),leniency,new Vector2(0,1));
    if(v!=Vector2.Zero){
      Position+=v;
      childRelposSafe();
      if(q.breakable!=null) s.BreakStuff(q.breakable,Util.getCollisionDir(v));
      return false;
    }
    return true;
  }
  public bool MoveHCollideExact(Query qs, int amount, int leniency)=>MoveHCollideExact(qs.q,qs.s,amount,leniency);
  public bool MoveVCollideExact(QueryBounds q, QueryIn s, int amount, int leniency){
    Vector2 v = leniency==0? TestMove(q,s,amount,new Vector2(0,1)) : TestMoveLeniency(q,s,amount,new Vector2(0,1),leniency,new Vector2(1,0));
    if(v!=Vector2.Zero){
      Position+=v;
      childRelposSafe();
      if(q.breakable!=null) s.BreakStuff(q.breakable,Util.getCollisionDir(v));
      return false;
    }
    
    return true;
  }
  public bool MoveVCollideExact(Query qs, int amount, int leniency)=>MoveVCollideExact(qs.q,qs.s,amount,leniency);
  public bool MoveHCollide(QueryBounds q, QueryIn s, float amount, int leniency){
    if(Math.Sign(movementCounter.X)!=Math.Sign(amount)) movementCounter.X = (float)Math.Clamp(movementCounter.X,-0.49,0.49);
    movementCounter.X+=amount;
    int dif = (int)Math.Round(movementCounter.X);
    bool fail = dif!=0 && MoveHCollideExact(q,s,dif,leniency);
    if(!fail) movementCounter.X-=dif;
    else movementCounter.X = (float)Math.Clamp(movementCounter.X, -0.501,0.501);
    return fail;
  }
  public bool MoveHCollide(Query qs, float amount, int leniency)=>MoveHCollide(qs.q,qs.s,amount,leniency);
  public bool MoveVCollide(QueryBounds q, QueryIn s, float amount, int leniency){
    if(Math.Sign(movementCounter.Y)!=Math.Sign(amount)) movementCounter.Y = (float)Math.Clamp(movementCounter.Y,-0.49,0.49);
    movementCounter.Y+=amount;
    int dif = (int)Math.Round(movementCounter.Y);
    bool fail = dif!=0 && MoveVCollideExact(q,s,dif,leniency);
    if(!fail) movementCounter.Y-=dif;
    else movementCounter.Y = (float)Math.Clamp(movementCounter.Y, -0.501,0.501);
    return fail;
  }
  public bool MoveVCollide(Query qs, float amount, int leniency)=>MoveVCollide(qs.q,qs.s,amount,leniency);
  public class Query{
    public QueryBounds q;
    public QueryIn s;
    public Query(QueryBounds q, QueryIn s){
      this.q=q; this.s=s;
    }
  }
  public struct BreakableRect{
    public Entity toBreak;
    public CollisionDirection dir;
    public BreakableRect(Entity toBreak){
      this.toBreak=toBreak;
      if(toBreak is Solid) dir=CollisionDirection.solid;
      else if(toBreak is JumpThru)dir = (
        (MaddiesIop.samah!=null && MaddiesIop.samah.IsInstanceOfType(toBreak))||
        (MaddiesIop.dt!=null && MaddiesIop.dt.IsInstanceOfType(toBreak))
      )?CollisionDirection.down:CollisionDirection.up;
      else if(MaddiesIop.jt!=null && MaddiesIop.jt.IsInstanceOfType(toBreak)){
        dir = MaddiesIop.side.get(toBreak)?CollisionDirection.right:CollisionDirection.left;
      }
    }
    public void Break(){
      if(toBreak is DashBlock db) db.Break(Vector2.Zero,Vector2.Zero,true,true);
      else if(toBreak.Get<ChildMarker>()?.parent is Template t){
        while(t!=null) if(t is TemplateBlock b && b.breakableByBlocks){
          b.breakBlock();
          return;
        } else t=t.parent;
      }
    }
  }
  public static Query getq(Template self, Vector2 maxpotentialmovemagn, bool getBreakables, bool useJts, bool useUncollidable){
    QueryIn s = getQself(self, useUncollidable);
    Vector2 v = maxpotentialmovemagn.Abs().Ceiling();
    //HashSet<Entity> toExclude = new(s.gotten);
    List<Entity> l = new();
    var qbounds = s.bounds._expand(v.X,v.Y);
    self.AddAllChildrenProp(l,Propagation.Shake);
    foreach(Solid p in s.gotten){
      foreach(StaticMover sm in p.staticMovers){
        if(sm.Entity is TemplateStaticmover smt){
          //foreach(Solid sl in smt.GetChildren<Solid>(Propagation.Shake)) toExclude.Add(sl);
          smt.AddAllChildrenProp(l,Propagation.Shake);
        }
      }
    }
    HashSet<Entity> toExclude = new(l);
    HashSet<BreakableRect> fbs = null;
    if(getBreakables){
      fbs = new();
      foreach(DashBlock f in self.Scene.Tracker.GetEntities<DashBlock>()){
        if(f.Get<ChildMarker>()?.propagatesTo(self)??false) continue;
        fbs.Add(new(f));
      }
      foreach(TemplateBlock b in self.Scene.Tracker.GetEntities<TemplateBlock>()){
        if(!b.breakableByBlocks || self.PropagateEither(b,Propagation.Shake)) continue;
        foreach(Platform p in b.GetChildren<Platform>()){
          if(p is Solid && qbounds.CollideFr(new(p))) fbs.Add(new(p));
          else if(useJts && p is JumpThru jt && qbounds.CollideFr(new(jt))) fbs.Add(new(jt));
        }
        if(useJts&&MaddiesIop.jt!=null) foreach(var jt in b.GetChildren(MaddiesIop.jt, Propagation.Shake)){
          if(qbounds.CollideFr(new(jt)))fbs.Add(new(jt));
        }
      }
      foreach(var d in fbs) toExclude.Add(d.toBreak);
    }
    QueryBounds q = getQinfo(qbounds,toExclude,self.Scene);
    q.breakable = fbs;
    if(useJts) AddJumpthrus(qbounds,q,s,toExclude,self.Scene);
    return new(q,s);
  }
  public Query getq(Vector2 maxpotentialmovemagn)=>getq(this, maxpotentialmovemagn,moveThroughDashblocks,hitJumpthrus,useOwnUncollidable);

  public virtual void OnTrigger(TriggerInfo info){
    if(TriggerInfo.TestPass(info,this)) triggered = true;
  }
  [OnLoad.OnHook(typeof(Platform),nameof(Platform.OnStaticMoverTrigger))]
  static void smtHook(On.Celeste.Platform.orig_OnStaticMoverTrigger orig, Platform p, StaticMover sm){
    if(p is ITemplateChild c){
      c.parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(TriggerInfo.SmInfo.getInfo(sm));
    } else {
      ChildMarker m = p.Get<ChildMarker>();
      if(m!=null){
        m.parent.GetFromTree<ITemplateTriggerable>()?.OnTrigger(TriggerInfo.SmInfo.getInfo(sm));
      }
    }
    orig(p,sm);
  }
}
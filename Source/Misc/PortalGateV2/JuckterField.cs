



using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/JuckterField")]
public class JuckterField:Entity{

  public class JuckterBase:IColliderWrapper.SimpleWrapperclass,IColliderWrapper{
    public JuckterField f;
    public int index;
    public Vector2 expandTlc;
    public Vector2 expandBrc;
    public JuckterBase(JuckterField field, Collider start, int idx):base(start){
      this.f = field;
      index = idx;
      expandTlc = f.bounds.tlc-f.locs[idx];
      expandBrc = f.bounds.brc-f.locs[idx];
    }
    Collider IColliderWrapper.interceptReplace(Monocle.Collider o){
      //DebugConsole.Write("Intercept Replace",o,this);
      if(o is JuckterBase or null) return o;
      if(o is Hitbox h) return new JuckterColliderHb(h,f,index);
      else return new JuckterColliderCompat(o,f,index);
    }
    public static void Set(Entity e, JuckterField f, int i){
      if(e.Collider is Hitbox hb) e.Collider = new JuckterColliderHb(hb,f,i);
      else e.Collider = new JuckterColliderCompat(e.Collider,f,i);
    }
    public bool SameCtx(JuckterBase other)=>other.f==f && other.index==index;
    public override void Render(Camera camera, Color color) {
      using(Util.WithRestore(ref Entity.Position)){
        Vector2 bpos = Entity.Position-f.locs[index];
        foreach(var l in f.locs){
          Entity.Position = l+bpos;
          wrapped.Render(camera,color);
        }
      }
    }
    public int CollideWhich(Collider c){
      using(Util.WithRestore(ref Entity.Position)){
        Vector2 bpos = Entity.Position-f.locs[index];
        for(int i=0; i<f.locs.Length; i++){
          Entity.Position = f.locs[i]+bpos;
          if(wrapped.Collide(c)) return i;
        }
        return -1;
      }
    }
    public Vector2 GetCenter(int idx)=>wrapped.Center+wrapped.Entity.Position-f.locs[index]+f.locs[idx];
    public Vector2 GetPos(int idx)=>wrapped.Entity.Position-f.locs[index]+f.locs[idx];
  }
  public class JuckterColliderHb:JuckterBase{
    Hitbox orig;
    public JuckterColliderHb(Hitbox o, JuckterField f, int idx):base(f,o,idx)=>orig=o;
    public override float Top=>orig.Position.Y+expandTlc.Y;
    public override float Left=>orig.Position.X+expandTlc.X;
    public override float Width=>orig.width+f.bounds.w;
    public override float Height=>orig.height+f.bounds.h;
    public override float Right=>orig.Right+expandBrc.X;
    public override float Bottom=>orig.Bottom+expandBrc.Y;
    bool Tesselate(Func<FloatRect,bool> hitfn){
      Vector2 offset = f.locs[index];
      Vector2 opos = Entity.Position-offset+orig.Position;
      FloatRect r=new(0,0,orig.width,orig.height);
      foreach(var v in f.locs){
        r.x=opos.X+v.x;
        r.y=opos.Y+v.y;
        if(hitfn(r)) return true;
      }
      return false;
    }
    public override bool Collide(Vector2 point)=>Tesselate(f=>f.CollidePoint(point));
    public override bool Collide(Circle c)=>Tesselate(f=>f.CollideCircle(c.AbsolutePosition,c.Radius));
    public override bool Collide(Rectangle r)=>Tesselate(f=>f.CollideExRect(r.X,r.Y,r.Width,r.Height));
    public override bool Collide(Hitbox h)=>Tesselate(f=>h.Collide(f.munane()));
    public override bool Collide(ColliderList l)=>Tesselate(f=>l.Collide(f.munane()));
    public override bool Collide(Grid g)=>Tesselate(f=>g.Collide(f.munane()));
    public override bool Collide(Vector2 a, Vector2 b)=>Tesselate(f=>f.CollideLine(a,b));
  }
  public class JuckterColliderCompat:JuckterBase{
    public JuckterColliderCompat(Collider o, JuckterField f, int idx):base(f,o,idx){}
    public override float Top=>wrapped.Top+expandTlc.Y;
    public override float Left=>wrapped.Left+expandTlc.X;
    public override float Width=>wrapped.Width+f.bounds.w;
    public override float Height=>wrapped.Height+f.bounds.h;
    public override float Right=>wrapped.Right+expandBrc.X;
    public override float Bottom=>wrapped.Bottom+expandBrc.Y;
    bool Tesselate(Func<bool> hitfn){
      Vector2 offset = f.locs[index];
      Vector2 opos = Entity.Position-offset;
      foreach(var v in f.locs){
        Entity.Position = opos+v;
        if(hitfn()){
          Entity.Position = opos+offset;
          return true;
        }
      }
      Entity.Position = opos+offset;
      return false;
    }
    public override bool Collide(Vector2 point)=>Tesselate(()=>wrapped.Collide(point));
    public override bool Collide(Circle c)=>Tesselate(()=>wrapped.Collide(c));
    public override bool Collide(Rectangle r)=>Tesselate(()=>wrapped.Collide(r));
    public override bool Collide(Hitbox h)=>Tesselate(()=>wrapped.Collide(h));
    public override bool Collide(ColliderList l)=>Tesselate(()=>wrapped.Collide(l));
    public override bool Collide(Grid g)=>Tesselate(()=>wrapped.Collide(g));
    public override bool Collide(Vector2 a, Vector2 b)=>Tesselate(()=>wrapped.Collide(a,b));
  }
  public class JuckterFake:Entity{
    Entity Other;
    JuckterBase with;
    public JuckterFake(Entity copy){
      Other = copy;
      Depth = Other.Depth;
      with = copy.Collider as JuckterBase;
    }
    public override void Update() {
      if(Other.Scene==null || Other.Collider is not JuckterBase f || !with.SameCtx(f)) RemoveSelf();
      Depth = Other.Depth;
      OverrideVisualComponent.EnsureParity(Other,this);
    }
    public override void Render(){
      if(Other.Scene==null || Other.Collider is not JuckterBase f || !with.SameCtx(f)) return;
      using(Util.WithRestore(ref Other.Position)){
        var li = with.f.locs;
        Vector2 offset = Other.Position-li[with.index];
        for(int i=0; i<li.Length; i++) if(i!=with.index){
          Other.Position = li[i]+offset;
          Other.Render();
        }
      }
    }  
  }
  Int2[] locs;
  IntRect bounds;
  Int2 size;
  List<(Holdable,int)> holding = new();
  public JuckterField(EntityData d, Vector2 o){
    locs=d.NodesWithPosition(o).Map(Int2.Round);
    bounds=IntRect.empty;
    foreach(var l in locs) bounds=bounds.union_(l);
    Depth = -100000;
    size = new(d.Width,d.Height);
    ResetEvents.LazyEnable(typeof(IColliderWrapper));
  }
  void AddEnt(Entity e, int i){
    JuckterBase.Set(e,this,i);
    if(e.Collider is not JuckterBase) throw new Exception($"Compat bad. {e} is not giving up old {e.Collider}");
    Scene.Add(new JuckterFake(e));
  }
  void Enter(Holdable h, int i){
    var e = h.Entity;
    holding.Add((h,i));
    AddEnt(e,i);
    if(e is TemplateHoldable t) using(new Template.ChainLock()){
      t.juckterChildren = new();
      foreach(var x in t.Te.GetChildren<Entity>()) if(x.Collider!=null) AddEnt(x,i);
      for(int j=0; j<locs.Length-1; j++){
        TemplateDisappearer d = t.makeCopyAt(e.Position-10000*Vector2.UnitY);
        d.addTo(Scene);
        d.setVisColAct(false,false,false);
        t.juckterChildren.Add(d);
      }
    }
  }
  static ParticleType pDisappear = new(){
    Source = GFX.Game["particles/zappysmoke"],
    Color = Calc.HexToColor("aa55cc"),
    FadeMode = ParticleType.FadeModes.Late,
    RotationMode = ParticleType.RotationModes.Random,
    LifeMin = 0.5f,
    LifeMax = 0.8f,
    Size = 0.7f,
    SizeRange = 0.25f,
    ScaleOut = true,
    Direction = 0,
    DirectionRange = MathF.PI*2,
    SpeedMin = 40f,
    SpeedMax = 80f,
    Acceleration = new Vector2(0f, 90f)
  };
  void Release(Holdable h){
    var e = h.Entity;
    if(e.Collider is not JuckterBase b) throw new Exception("idk");
    for(int i=0; i<b.f.locs.Length; i++) if(i!=b.index){
      var c = b.GetCenter(i);
      float aoff = Calc.Random.Range(0,MathF.PI*2/6);
      for(int aidx = 0; aidx<6; aidx++){
        float angle = aoff+aidx*MathF.PI*2/6;
        (base.Scene as Level).ParticlesBG.Emit(pDisappear, 2, c, new Vector2(3f, 3f), angle);
      }
      if(e is TemplateHoldable th && th.juckterChildren?.Count>0){
        var last = th.juckterChildren[^1];
        th.juckterChildren.RemoveAt(th.juckterChildren.Count-1);
        last.relposTo(b.GetPos(i),Vector2.Zero);
        last.ownLiftspeed = th.Te.ownLiftspeed;
        last.setVisCol(true,true);
        last.destroy(true);
        DebugConsole.Write("Moved child to", last.Position);
      }
    }
    if(e is TemplateHoldable the) the.juckterChildren=null;
    using(new IColliderWrapper.CollideDetourLock()){
      e.Collider = b.wrapped;
      if(e is TemplateHoldable t) foreach(var x in t.Te.GetChildren<Entity>()){
        if(x.Collider is JuckterBase be) x.Collider=be.wrapped;
      }
    }
  }
  static void Pickup(Holdable h){
    if(h.Entity.Collider is JuckterBase og) {
      if(h.PickupCollider != null) h.Entity.Collider=h.PickupCollider;
      JuckterBase b = h.Entity.Collider as JuckterBase;
      int idx = b.CollideWhich(h.Holder.Collider);
      if(idx==-1) throw new Exception("idk");
      h.Entity.Position += b.f.locs[idx]-b.f.locs[b.index];
      og.index = b.index = idx;
      h.Entity.Collider = og;
      b.f.Release(h);
      b.f.holding.RemoveAll(x=>x.Item1==h);
    }
  }
  bool inArea(Entity e, int i){
    if(!Collidable) return false;
    Rectangle m = new FloatRect(locs[i].x,locs[i].y,size.x,size.y).munane();
    if(e.Collider is JuckterBase b) return b.wrapped.Collide(m);
    return e.Collider.Collide(m);
  }
  public override void Update() {
    base.Update();
    holding.RemoveAll((pair)=>{
      if(pair.Item1.Entity is not {} e || e.Scene==null){
        if(pair.Item1.Entity is TemplateHoldable te) foreach(var v in te.juckterChildren) v.destroy(true);
        return true;
      }
      if(!inArea(e, pair.Item2) || pair.Item1.Holder!=null){
        Release(pair.Item1);
        return true;
      }
      else if(e.Collider is not JuckterBase) throw new Exception("Collider escaped on "+e.ToString());
      return false;
    });
    if(!Collidable) return;
    foreach(Holdable h in Scene.Tracker.GetComponents<Holdable>()) if(h.Holder == null && h.Entity.Collider is not JuckterBase){
      var col = h.Entity.Collider;
      if(h.Entity?.Scene==null || !h.Entity.Collidable) continue;
      for(int i=0; i<locs.Length; i++) if(col.Collide(new Rectangle(locs[i].x,locs[i].y,size.x,size.y))){
        Enter(h,i);
        break;
      }
    }
  }
  public override void Render(){
    base.Render();
    foreach(var l in locs) Draw.Rect(new Rectangle(l.x,l.y,size.x,size.y),Color.White*0.3f);
  }
  public override void Removed(Scene scene) {
    foreach(var (p,i) in holding) if(p.Entity?.Scene!=null) Release(p);
    base.Removed(scene);
  }
  [OnLoad.ILHook(typeof(Holdable),nameof(Holdable.Pickup))]
  static void PickupHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchLdfld<Holdable>(nameof(Holdable.OnPickup))  
    )){
      c.EmitLdarg0();
      c.EmitDelegate(Pickup);
      return;
    }
    DebugConsole.WriteFailure("Failed to add pickup hook",true);
  }
}
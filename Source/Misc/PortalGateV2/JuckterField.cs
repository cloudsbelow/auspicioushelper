



using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class JuckterField:Entity{

  public class JuckterBase:ColliderList,ColliderWrapper{
    public JuckterField field;
    public int index;
    public JuckterBase(JuckterField f, int idx){
      field = f;
      index = idx;
    }
    Collider ColliderWrapper.interceptReplace(Monocle.Collider o){
      if(o is JuckterBase or null) return o;
      if(o is Hitbox h) return new JuckterColliderHb(h,field,index);
      else return new JuckterColliderCompat(o,field,index);
    }
    public override void Added(Entity entity) {
      base.Added(entity);
      ((ColliderWrapper) this).wrapped?.Added(entity);
    }
    public override void Removed() {
      base.Removed();
      ((ColliderWrapper) this).wrapped?.Removed();
    }
  }
  public class JuckterColliderHb:JuckterBase, ColliderWrapper{
    Hitbox orig;
    Collider ColliderWrapper.wrapped=>orig;
    public JuckterColliderHb(Hitbox o, JuckterField f, int idx):base(f,idx){
      orig=o;
    }
    public override float Top=>orig.Position.Y;
    public override float Left=>orig.Position.X;
    public override float Width=>orig.width;
    public override float Height=>orig.height;
    public override float Right=>orig.Right;
    public override float Bottom=>orig.Bottom;
    bool Tesselate(Func<FloatRect,bool> hitfn){
      Vector2 offset = field.locs[index];
      Vector2 opos = Entity.Position-offset+orig.Position;
      FloatRect f=new(0,0,orig.width,orig.height);
      foreach(var v in field.locs){
        f.x=opos.X+v.X;
        f.y=opos.Y+v.Y;
        if(hitfn(f)) return true;
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
  public class JuckterColliderCompat:JuckterBase,ColliderWrapper{
    Collider orig;
    Collider ColliderWrapper.wrapped=>orig;
    public JuckterColliderCompat(Collider o, JuckterField f, int idx):base(f,idx){
      orig=o;
    }
    public override float Top=>orig.Position.Y;
    public override float Left=>orig.Position.X;
    public override float Width=>orig.Width;
    public override float Height=>orig.Height;
    public override float Right=>orig.Right;
    public override float Bottom=>orig.Bottom;
    bool Tesselate(Func<bool> hitfn){
      Vector2 offset = field.locs[index];
      Vector2 opos = Entity.Position-offset;
      foreach(var v in field.locs){
        Entity.Position = opos+v;
        if(hitfn()){
          Entity.Position = opos+offset;
          return true;
        }
      }
      Entity.Position = opos+offset;
      return false;
    }
    public override bool Collide(Vector2 point)=>Tesselate(()=>orig.Collide(point));
    public override bool Collide(Circle c)=>Tesselate(()=>orig.Collide(c));
    public override bool Collide(Rectangle r)=>Tesselate(()=>orig.Collide(r));
    public override bool Collide(Hitbox h)=>Tesselate(()=>orig.Collide(h));
    public override bool Collide(ColliderList l)=>Tesselate(()=>orig.Collide(l));
    public override bool Collide(Grid g)=>Tesselate(()=>orig.Collide(g));
    public override bool Collide(Vector2 a, Vector2 b)=>Tesselate(()=>orig.Collide(a,b));
  }
  public class JuckterFake:Entity{
    Holdable Other;
    public override void Update() {
      if(Other.Scene==null) RemoveSelf();
      
    }
    public override void Render(){
      if(Other.Scene==null) return;

    }  
  }
  Vector2[] locs;
  public JuckterField(EntityData d, Vector2 o){
    locs=d.NodesWithPosition(o);
  }
  void AddEnt(Entity e, int i){
    if(e.Collider is Hitbox hb) e.Collider = new JuckterColliderHb(hb,this,i);
    else e.Collider = new JuckterColliderCompat(e.Collider,this,i);
  }
  void Enter(Entity e,int i){
    
    if(e is TemplateHoldable h) using(new Template.ChainLock()){
      templateFiller f = h.getTemplate;
      if(f==null) return;
      for(int j=0; j<locs.Length; j++) if(j!=i){
        TemplateDisappearer t = new(locs[j]+Position-locs[i],0);
        t.setTemplate();
      }
    }
  }
}
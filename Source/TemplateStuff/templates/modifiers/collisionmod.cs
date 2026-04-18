using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateCollisionModifier")]
public class TemplateCollisionModifier:Template, Template.IRegisterEnts{
  class EKey():OnAnyRemoveComp(false,false){
    Util.HybridSet<HittableComp> inside = new();
    public static EKey Get(Entity e){
      if(e.Get<EKey>() is not {} ek) e.Add(ek = new());
      return ek;
    }
    public void Rem(HittableComp c)=>inside.Remove(c);
    public void Add(HittableComp c)=>inside.Add(c);
    public override void OnRemove() {
      foreach(var h in inside) h.Rem(Entity);
      inside.Clear();
    }
  }
  class HittableComp(TemplateCollisionModifier top):OnAnyRemoveComp(false,false){
    Dictionary<Entity,bool> can = new();
    public void Rem(Entity e)=>can.Remove(e);
    public override void OnRemove(){
      if(can==null) return;
      foreach(var (c,_) in can) EKey.Get(c).Rem(this);
      can=null;
    }
    public bool ShouldHit(Entity e){
      if(!can.TryGetValue(e,out var b)){
        EKey.Get(e).Add(this);
        can.Add(e,b=top.ShouldCollideWith(e));
      }
      return b;
    }
  }
  class FilterCollider(HittableComp f, Collider col):IColliderWrapper.SimpleWrapperclass(col), IColliderWrapper{
    Collider IColliderWrapper.interceptReplace(Collider o) {
      if(o is FilterCollider) return o;
      return new FilterCollider(f,o);
    }
    bool Try(Collider c)=>c.Entity==null || f.ShouldHit(c.Entity);
    public override bool Collide(Circle c)=>wrapped.Collide(c) && Try(c);
    public override bool Collide(Hitbox h)=>wrapped.Collide(h) && Try(h);
    public override bool Collide(ColliderList l)=>l.Collide(wrapped) && Try(l);
    public override bool Collide(Grid g)=>wrapped.Collide(g) && Try(g);
  }
  Util.Trie<bool> types = new(true);
  bool invTypes = false;
  Util.Trie<bool> paths = new(true);
  bool invPaths = false;
  bool ShouldCollideWith(Entity e){
    string typename = e.SourceData?.Name;
    if(typename == null) typename = e switch {
      SolidTiles=>"fg", Player=>"player", FastDebris=>"debris", _=>e.GetType().FullName
    };
    if(log) DebugConsole.Write("testing", typename);
    bool typev = typename!=null && types.GetOrDefault(typename);
    typev |= e is Actor && types.GetOrDefault("actor");
    bool pathv = false;
    if(paths!=null) foreach(var i in FoundEntity.foundIdents(e)) pathv |= paths.GetOrDefault(i);
    if(e.Get<ChildMarker>() is {} m){
      Template c = m.parent;
      do {
        typename = c.GetTypename();
        if(log) DebugConsole.Write("testing", typename);
        typev |= typename!=null && types.GetOrDefault(typename??"NULL");
        if(paths!=null && !string.IsNullOrEmpty(c.ownidpath)) pathv |= paths.GetOrDefault(c.fullpath);
      }while((c=c.parent)!=null);
    }
    typev = typev!=invTypes;
    pathv = pathv!=invPaths;
    return mode switch {
      CombinationMode.and=>typev && pathv,
      CombinationMode.or=>typev || pathv,
      CombinationMode.xor=>typev != pathv,
      CombinationMode.typeMinusPath=>typev && !pathv,
      CombinationMode.pathMinusType=>(!typev) && pathv,
      _=>typev!=pathv
    };
  }

  enum CombinationMode{
    and, xor, or, typeMinusPath, pathMinusType
  }
  CombinationMode mode;
  HittableComp comp;
  bool log;
  public TemplateCollisionModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateCollisionModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    foreach(var path in Util.listparseflat(d.Attr("paths",""))){
      if(path=="*") invPaths=true;
      else paths.Add(path,true);
    }
    if(!paths.hasStuff) paths=null;
    foreach(var type in Util.listparseflat(d.Attr("types",""))){
      if(type=="*") invTypes=true;
      else types.Add(type,true);
    }
    Add(comp = new(this));
    mode = d.Enum("combinationMode",CombinationMode.or);
    log = d.Bool("log",false);
  }
  public override void RegisterEnts(List<Entity> l) {
    base.RegisterEnts(l);
    foreach(var i in l) if(i.Get<ChildMarker>()?.parent.GetFromTree<TemplateCollisionModifier>()==this){
      i.Collider = new FilterCollider(comp,i.Collider);
    }
  }
}
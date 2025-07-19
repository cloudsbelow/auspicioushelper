



using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Registry;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public interface ITemplateChild{
  Template parent {get; set;}
  Template.Propagation prop {get=>Template.Propagation.All;}
  void relposTo(Vector2 loc, Vector2 liftspeed);
  void addTo(Scene s){}
  void templateAwake(){}
  void parentChangeStat(int vis, int col, int act);
  bool hasRiders<T>() where T : Actor{
    return false;
  }
  bool hasInside(Actor a){
    return false;
  }
  Template.Propagation propagatesTo(Template target, Template.Propagation p = Template.Propagation.All){
    ITemplateChild c=this;
    while(c != target && c!= null && p!=Template.Propagation.None){
      if(c==target) return p;
      c=c.parent;
      p&=c.parent.prop;
    }
    return Template.Propagation.None;
  }
  DashCollisionResults propagateDashhit(Player player, Vector2 direction){
    if((prop&Template.Propagation.DashHit) != Template.Propagation.None && (parent!=null)){
      if(parent.OnDashCollide != null) return parent.OnDashCollide(player, direction);
      return ((ITemplateChild)parent).propagateDashhit(player, direction);
    }
    return DashCollisionResults.NormalCollision;
  }
  void AddAllChildren(List<Entity> list);
  void setOffset(Vector2 ppos){}
  void destroy(bool particles);
  bool shouldAddAsChild=>true;
}
public interface IChildShaker{
  Vector2 lastShake {get;set;}
  void OnShake(Vector2 amount);
  void VanillaShake(Vector2 amount){
    if(lastShake!=amount) OnShake(amount-lastShake);
    lastShake = amount;
  }
  void OnShakeFrame(Vector2 amount){
    VanillaShake(amount);
  }
}

public class Template:Entity, ITemplateChild{
  public templateFiller t=null;
  public List<ITemplateChild> children = new List<ITemplateChild>();
  public int depthoffset;
  public Template parent{get;set;} = null;
  public string ownidpath;
  [Flags]
  public enum Propagation
  {
      None     = 0,      
      Riding   = 1 << 0, 
      DashHit  = 1 << 1,
      Weight   = 1 << 2,
      Shake = 1<<3,
      Inside = 1<<4,
      All = Riding|DashHit|Weight|Shake|Inside
  }
  public virtual Vector2 virtLoc=>Position;
  public Vector2 ownLiftspeed;
  public virtual Vector2 gatheredLiftspeed=>parent==null?ownLiftspeed:ownLiftspeed+parent.gatheredLiftspeed;
  public Vector2 parentLiftspeed=>parent==null?Vector2.Zero:parent.gatheredLiftspeed;
  public Propagation prop{get;set;} = Propagation.All; 
  public Vector2 toffset = Vector2.Zero;
  public Wrappers.BasicMultient basicents = null;
  public DashCollision OnDashCollide = null;
  string templateStr;
  public Template(EntityData data, Vector2 pos, int depthoffset):base(pos){
    templateStr = data.Attr("template","");
    this.depthoffset = depthoffset;
    this.Visible = false;
    Depth = 10000+depthoffset;
    this.ownidpath=getOwnID(data);
    MovementLock.skiphooks.enable();
    if(string.IsNullOrEmpty(templateStr)){
      TemplateBehaviorChain.AddEmptyTemplate(data);
    }
  }
  public virtual void relposTo(Vector2 loc, Vector2 liftspeed){
    Position = loc+toffset;
    childRelposTo(virtLoc, liftspeed);
  }
  public void childRelposTo(Vector2 loc, Vector2 liftspeed){
    foreach(ITemplateChild c in children){
      c.relposTo(loc, liftspeed);
    }
  }


  public void childRelposSafe(){
    using(new MovementLock())childRelposTo(virtLoc,gatheredLiftspeed);
  }
  internal Wrappers.FgTiles fgt = null;
  public void addEnt(ITemplateChild c){
    c.parent = this;
    if(c.shouldAddAsChild)children.Add(c);
    if(c is Template ct){
      ct.depthoffset+=depthoffset;
      if(t.chain!=null){
        ct.t=t.chain.NextFiller();
      }
    }
    c.addTo(Scene);
    c.setOffset(virtLoc);
  }
  public void setOffset(Vector2 ppos){
    this.toffset = Position-ppos;
  }
  void makeChildren(Scene scene, bool recursive = false){
    if(t==null) return;
    //if(t.bgt!=null) addEnt(new Wrappers.BgTiles(t,virtLoc,depthoffset));
    //if(t.fgt!=null) addEnt(fgt=new Wrappers.FgTiles(t, virtLoc, depthoffset));
    t.AddTilesTo(this);
    Level l = SceneAs<Level>();
    Vector2 simoffset = this.virtLoc-t.origin;
    string fp = fullpath;
    foreach(EntityData w in t.ChildEntities){
      Entity e = EntityParser.create(w,l,t.roomdat,simoffset,this,fp);
      if(e is ITemplateChild c){
        addEnt(c);
      }
      else if(e!=null)scene.Add(e);
    }
    foreach(DecalData d in t.decals){
      Decal e = new Decal(d.Texture, simoffset+d.Position, d.Scale, d.GetDepth(0), d.Rotation, d.ColorHex){
        DepthSetByPlacement = true
      };
      AddBasicEnt(e, simoffset+d.Position-virtLoc);
    }
    if(!recursive) templateAwake();
  }
  public virtual void templateAwake(){
    foreach(var c in children) c.templateAwake();
  }
  public virtual void addTo(Scene scene){
    if(Scene != null){
      DebugConsole.Write("Something has gone terribly wrong in recursive adding process");
    }
    Scene = scene;
    if(t==null && !MarkedRoomParser.getTemplate(templateStr, parent, scene, out t)){
      DebugConsole.Write($"No template found with identifier \"{templateStr}\" in {this} at {Position}");
    }
    if(basicents != null)basicents.sceneadd(scene);
    scene.Add(this);
    makeChildren(scene, parent!=null);
  }
  public override void Added(Scene scene){
    bool flag = string.IsNullOrWhiteSpace(templateStr) && t==null;
    if(Scene == null && !flag){
      //DebugConsole.Write($"Got top-level template {this} of {t?.name}");
      addTo(scene);
    }
    base.Added(scene);
    if(flag){
      Active = false;
      RemoveSelf();
      return;
    }
  }
  public void AddBasicEnt(Entity e, Vector2 offset){
    if(basicents == null){
      basicents = new Wrappers.BasicMultient(this);
      addEnt(basicents);
      if(Scene!=null)basicents.sceneadd(Scene);
    }
    basicents.add(e,offset);
  }
  public bool hasRiders<T>() where T:Actor{
    foreach(ITemplateChild c in children){
      //if(c is TemplateZipmover z) DebugConsole.Write($"check: {z.prop}");
      if((c.prop & Propagation.Riding)!=0 && c.hasRiders<T>()) return true;
    }
    return false;
  }
  public bool hasInside(Actor a){
    foreach(ITemplateChild c in children) 
      if(((c.prop&Propagation.Inside)!=Propagation.None) && c.hasInside(a)) return true;
    return false;
  }
  public override void Removed(Scene scene){
    destroy(false);
  }
  public void AddAllChildren(List<Entity> l){
    foreach(ITemplateChild c in children){
      c.AddAllChildren(l);
    }
    l.Add(this);
  }
  public void AddAllChildrenProp(List<Entity> l, Propagation p){
    foreach(ITemplateChild c in children){
      if(c is Template t){
        if((c.prop&p) == p) t.AddAllChildrenProp(l,p);
      } else {
        if((c.prop & p) == p) c.AddAllChildren(l);
      }
    }
  }
  public DashCollisionResults dashHit(Player p, Vector2 dir){
    if(OnDashCollide!=null) return OnDashCollide(p,dir);
    else return ((ITemplateChild)this).propagateDashhit(p,dir);
  }
  public List<T> GetChildren<T>(Propagation p = Propagation.None){
    List<Entity> list = new();
    if(p == Propagation.None) AddAllChildren(list);
    else AddAllChildrenProp(list,p);
    List<T> nlist = new();
    foreach(var li in list) if(li is T le) nlist.Add(le);
    return nlist;
  }
  public void AddChildren(ICollection<Entity> li, Type t, Propagation p = Propagation.None){
    List<Entity> l = new();
    if(prop!=Propagation.None)AddAllChildrenProp(l,p);
    else AddAllChildren(l);
    foreach(var e in l) if(t.IsInstanceOfType(e)) li.Add(e);
  }
  public void AddChildren(ICollection<Entity> li, List<Type> ti, Propagation p = Propagation.None){
    List<Entity> l = new();
    if(prop!=Propagation.None)AddAllChildrenProp(l,p);
    else AddAllChildren(l);
    foreach(var e in l){
      foreach(Type t in ti){
        if(t.IsInstanceOfType(e)){
          li.Add(e);
          break;
        } 
      }
    } 
  }
  public virtual void parentChangeStat(int vis, int col, int act){
    foreach(ITemplateChild c in children){
      c.parentChangeStat(vis,col,act);
    }
  }
  public virtual void destroy(bool particles){
    foreach(ITemplateChild c in children){
      c.destroy(particles);
    }
    children.Clear();
    fgt=null;
    RemoveSelf();
  }
  public void destroyChildren(bool particles = true){
    foreach(ITemplateChild c in children){
      c.destroy(particles);
    }
    children.Clear();
    fgt = null;
    basicents = null;
  }
  public virtual void remake(){
    makeChildren(Scene);
  }
  public string fullpath=>parent==null?ownidpath.ToString():parent.fullpath+$"/{ownidpath}";
  public static string getOwnID(EntityData e){
    return e.ID.ToString();
  }
  
  public Vector2 ownShakeVec;
  public Vector2 gatheredShakeVec=>(parent==null||(Propagation.Shake&prop)==0)?ownShakeVec:ownShakeVec+parent.gatheredShakeVec;
  public float shakeTimer;
  public static Dictionary<Entity, Vector2> prevpos = new();
  static void restorePos(){
    foreach(var pair in prevpos){
      pair.Key.Position=pair.Value;
    }
    prevpos.Clear();
  }
  public void shake(float time){
    if(time <= 0) return;
    if(shakeTimer<=0) Add(new Coroutine(shakeRoutine()));
    shakeTimer = MathF.Max(time,shakeTimer);
  }
  BeforeAfterRender bar;
  IEnumerator shakeRoutine(){
    if(bar!=null)Remove(bar);
    Add(bar = new(()=>{
      foreach(Entity e in GetChildren<Entity>(Propagation.Shake)){
        prevpos.TryAdd(e,e.Position);
        e.Position+=ownShakeVec;
        if(e is IChildShaker s) s.OnShakeFrame(ownShakeVec);
      }
    }));
    shakeHooks.enable();
    while(shakeTimer>0){
      shakeTimer-=Engine.DeltaTime;
      if(Scene.OnInterval(0.04f)) ownShakeVec = Calc.Random.ShakeVector();
      yield return null;
    }
    ownShakeVec = Vector2.Zero;
    Remove(bar);
    bar=null;
    foreach(Entity e in GetChildren<Entity>(Propagation.Shake)){
      if(e is IChildShaker s) s.OnShakeFrame(ownShakeVec);
    }
    yield break;
  }
  public static HookManager shakeHooks = new HookManager(()=>{
    BeforeAfterRender.postafter.Add(restorePos);
  },()=>{
    BeforeAfterRender.postafter.Remove(restorePos);
  });

  public T GetFromTree<T>(){
    if(this is T a) return a;
    if(parent != null) return parent.GetFromTree<T>();
    return default(T);
  }
}


public class MovementLock:IDisposable{
  static HashSet<Actor> alreadyX = new();
  static HashSet<Actor> alreadyY = new();
  static int instances = 0;
  static int csinstances = 0;
  static bool canSkip=>instances>0;
  bool always;
  public MovementLock(bool always=true){
    this.always=always;
    if(always) csinstances++;
    if(instances++==0){
      alreadyX.Clear(); alreadyY.Clear();
    }
  }
  public virtual void Dispose(){
    if(always) csinstances--;
    if(--instances == 0){
      alreadyX.Clear(); alreadyY.Clear();
    }
  }
  static bool moveHHook(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int move, Collision cb, Solid pusher){
    if(pusher == null && cb == null && canSkip){
      if(alreadyX.Contains(self)) return false;
      bool flag = orig(self, move, cb, pusher);
      if(!flag) alreadyX.Add(self);
      return flag;
    }
    return orig(self, move, cb, pusher);
  }
  static bool moveVHook(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int move, Collision cb, Solid pusher){
    if(pusher == null && cb == null && canSkip){
      if(alreadyY.Contains(self)){
        return false;
      }
      bool flag = !orig(self, move, cb, pusher);
      if(flag || move<0){
        alreadyY.Add(self);
      }
      return flag;
    }
    return orig(self, move, cb, pusher);
  }
  public static HookManager skiphooks = new HookManager(()=>{
    On.Celeste.Actor.MoveHExact+=moveHHook;
    On.Celeste.Actor.MoveVExact+=moveVHook;
  },void ()=>{
    On.Celeste.Actor.MoveHExact-=moveHHook;
    On.Celeste.Actor.MoveVExact-=moveVHook;
  }, auspicioushelperModule.OnEnterMap);
}
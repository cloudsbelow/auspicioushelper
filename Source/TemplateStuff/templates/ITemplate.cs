



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
  void relposTo(Vector2 loc, Vector2 parentLiftspeed);
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
    while(c!= null && p!=Template.Propagation.None){
      if(c==target) return p;
      p&=c.prop;
      c=c.parent;
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
      Shake = 1<<3,
      Inside = 1<<4,
      All = Riding|DashHit|Shake|Inside
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
  /// <summary>
  /// WARNING: by the time templates are constructed, the entitydata
  /// position has already been added to pos. 
  /// </summary>
  /// <param name="data"></param>
  /// <param name="pos">THE POSITION OF THE ENTITIY</param>
  /// <param name="depthoffset"></param>
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
  public Template(Vector2 pos, int depthoffset=0):base(pos){
    this.Visible = false;
    Depth = 10000+depthoffset;
    MovementLock.skiphooks.enable();
    this.ownidpath="";
  }
  public virtual void relposTo(Vector2 loc, Vector2 parentLiftspeed){
    Position = loc+toffset;
    childRelposTo(virtLoc, parentLiftspeed+ownLiftspeed);
  }
  public void childRelposTo(Vector2 loc, Vector2 liftspeed){
    foreach(ITemplateChild c in children){
      c.relposTo(loc, liftspeed);
    }
  }


  public void childRelposSafe(){
    using(new MovementLock()){
      Entity movewith = UpdateHook.getFollowEnt();
      Vector2 mwp = movewith?.Position??Vector2.Zero;
      childRelposTo(virtLoc,gatheredLiftspeed);
      if(movewith!=null && mwp!=movewith.Position && UpdateHook.cachedPlayer is Player p){
        Vector2 del = movewith.Position-mwp;
        if(p.TreatNaive){
          if(!MovementLock.movedX(p))p.NaiveMove(Vector2.UnitX*del.X);
          if(!MovementLock.movedY(p))p.NaiveMove(Vector2.UnitY*del.Y);
        } else {
          if(!MovementLock.movedX(p))p.MoveH(del.X);
          if(!MovementLock.movedY(p))p.MoveV(del.Y);
        }
        Template parent = null;
        if(movewith is ITemplateChild itc)parent=itc.parent;
        if(movewith.Get<ChildMarker>() is ChildMarker cm)parent=cm.parent;
        if(parent!=null) p.LiftSpeed = parent.gatheredLiftspeed;
        else p.LiftSpeed = del/MathF.Max(0.001f,Engine.DeltaTime);
      }
    }
  }
  public void relposOne(ITemplateChild c){
    var tc = children;
    children = new List<ITemplateChild>(){c};
    childRelposSafe();
    children = tc;
  }
  public bool PropagateEither(Template other,Propagation req){
    Template c=other;
    while(c!=null){
      if(c==this) return true;
      if((c.prop & req) != req) break;
      c=c.parent;
    }
    c=this;
    while(c!=null){
      if(c==other) return true;
      if((c.prop & req)!= req) break;
      c=c.parent;
    }
    return false;
  }
  internal Wrappers.FgTiles fgt = null;
  public void addEnt(ITemplateChild c){
    c.parent = this;
    if(c.shouldAddAsChild)children.Add(c);
    if(c is Template ct){
      ct.depthoffset+=depthoffset;
      if(t.chain!=null){
        ct.t??=t.chain.NextFiller();
      }
    }
    c.addTo(Scene??addingScene);
    c.setOffset(virtLoc);
  }
  public void restoreEnt(ITemplateChild c){
    c.parent = this;
    if(c.shouldAddAsChild)children.Add(c);
    relposOne(c);
  }
  public void setOffset(Vector2 ppos){
    this.toffset = Position-ppos;
  }
  bool expanded = false;
  public void MarkExpanded()=>expanded=true;
  public void makeChildren(Scene scene, bool recursive = false){
    if(t==null || expanded) return;
    expanded = true;
    //if(t.bgt!=null) addEnt(new Wrappers.BgTiles(t,virtLoc,depthoffset));
    //if(t.fgt!=null) addEnt(fgt=new Wrappers.FgTiles(t, virtLoc, depthoffset));
    t.AddTilesTo(this, scene);
    Level l = scene as Level;
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
      Decal e = new Decal(d.Texture, simoffset+d.Position, d.Scale, d.Depth??0, d.Rotation, d.ColorHex){
        DepthSetByPlacement = true
      };
      AddBasicEnt(e, simoffset+d.Position-virtLoc);
    }
    if(!recursive) templateAwake();
  }
  public virtual void templateAwake(){
    foreach(var c in children) c.templateAwake();
  }
  internal Scene addingScene;
  public void setTemplate(string s=null, Scene scene=null){
    templateStr=s??templateStr;
    if(t==null && !MarkedRoomParser.getTemplate(templateStr, parent, scene, out t)){
      DebugConsole.Write($"No template found with identifier \"{templateStr}\" in {this} at {Position}");
    }
  }
  public virtual void addTo(Scene scene){
    addingScene = scene;
    setTemplate(templateStr, scene);
    if(basicents != null)basicents.sceneadd(scene);
    scene.Add(this);
    makeChildren(scene, parent!=null);
    addingScene = null;
  }
  public override void Added(Scene scene){
    bool flag = string.IsNullOrWhiteSpace(templateStr) && t==null;
    if(parent == null && !flag && !expanded){
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
      if((Scene??addingScene)!=null)basicents.sceneadd(Scene??addingScene);
    }
    basicents.add(e,offset);
  }
  public bool hasRiders<T>() where T:Actor{
    foreach(ITemplateChild c in children){
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
    base.Removed(scene);
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
  public void MarkChildrenImmediate(){
    List<Entity> list = new();
    foreach(ITemplateChild c in children){
      if(c is Template temp) temp.MarkChildrenImmediate();
      else AddAllChildren(list);
    }
    foreach(Entity e in list){
      if(e.Get<ChildMarker>()==null){
        e.Add(new ChildMarker(this));
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
    if(typeof(T) == typeof(Entity)) return (List<T>)(object)list;
    List<T> nlist = new();
    foreach(var li in list) if(li is T le) nlist.Add(le);
    return nlist;
  }
  public List<Entity> GetChildren(Type t, Propagation p = Propagation.None){
    List<Entity> list = new();
    if(p == Propagation.None) AddAllChildren(list);
    else AddAllChildrenProp(list,p);
    List<Entity> nlist = new();
    foreach(var li in list) if(t.IsInstanceOfType(li)) nlist.Add(li);
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
    if(act != 0) Active = act>0;
    if(act==-1) ownLiftspeed = Vector2.Zero;
  }
  bool destroying;
  public virtual void emancipate(){
    if(parent is { } p && !p.destroying){
      parent.children.Remove(this);
      parent = null;
    }
  }
  public virtual void destroy(bool particles){
    destroyChildren(particles);
    emancipate();
    RemoveSelf();
  }
  public void destroyChildren(bool particles = true){
    destroying=true;
    foreach(ITemplateChild c in children){
      c.destroy(particles);
    }
    children.Clear();
    fgt = null;
    basicents = null;
    expanded=false;
    destroying = false;
  }
  public virtual void remake(){
    makeChildren(Scene);
    UpdateHook.EnsureUpdateAny();
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
  Coroutine shroutine = null;
  public void shake(float time){
    if(time <= 0) return;
    if(shakeTimer<=0 && shroutine==null){
      if(bar!=null)Remove(bar);
      Add(bar = new(()=>{
        foreach(Entity e in GetChildren<Entity>(Propagation.Shake)){
          prevpos.TryAdd(e,e.Position);
          e.Position+=ownShakeVec;
          if(e is IChildShaker s) s.OnShakeFrame(ownShakeVec);
        }
      }));
      shakeHooks.enable();
      Add(shroutine=new Coroutine(shakeRoutine()));
    } 
    shakeTimer = MathF.Max(time,shakeTimer);
  }
  public void EndShake()=>shakeTimer = 0;
  public virtual Vector2? getShakeVector(float n)=>Scene.OnInterval(0.04f)?Calc.Random.ShakeVector():null;
  BeforeAfterRender bar;
  IEnumerator shakeRoutine(){
    while(shakeTimer>0){
      float nTimer = shakeTimer-Engine.DeltaTime;
      if(getShakeVector(nTimer) is Vector2 v)ownShakeVec=v;
      shakeTimer = nTimer;
      yield return null;
    }
    ownShakeVec = Vector2.Zero;
    Remove(bar);
    bar=null;
    foreach(Entity e in GetChildren<Entity>(Propagation.Shake)){
      if(e is IChildShaker s) s.OnShakeFrame(ownShakeVec);
    }
    shroutine = null;
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
  public virtual void RegisterEnts(List<Entity> l){}
  public virtual void OnNewEnts(List<Entity> l){
    RegisterEnts(l);
    parent?.OnNewEnts(l);
  }
  public void AddNewEnts(List<Entity> l){
    OnNewEnts(l);
    if(GetFromTree<TemplateDisappearer>() is {} disap)disap.enforce();
  }
  public int TemplateDepth(){
    Template c = this;
    for(int i=0;;i++){
      if(c==null) return i;
      c=c.parent;
    }
  }
  public override string ToString()=>base.ToString()+GetHashCode();
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
      if(alreadyX.Contains(self))return false;
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
  static void naiveMoveHook(On.Celeste.Actor.orig_NaiveMove orig, Actor self, Vector2 move){
    if(canSkip){
      if(alreadyX.Contains(self))move.X=0;
      if(alreadyY.Contains(self))move.Y=0;
      if(move.X!=0)alreadyX.Add(self);
      if(move.Y!=0)alreadyY.Add(self);
    }
    orig(self,move);
  }
  public static bool movedX(Actor a)=>alreadyX.Contains(a);
  public static bool movedY(Actor a)=>alreadyY.Contains(a);
  public static HookManager skiphooks = new HookManager(()=>{
    On.Celeste.Actor.MoveHExact+=moveHHook;
    On.Celeste.Actor.MoveVExact+=moveVHook;
    On.Celeste.Actor.NaiveMove+=naiveMoveHook;
  },void ()=>{
    On.Celeste.Actor.MoveHExact-=moveHHook;
    On.Celeste.Actor.MoveVExact-=moveVHook;
    On.Celeste.Actor.NaiveMove-=naiveMoveHook;
  }, auspicioushelperModule.OnEnterMap);
}



using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class Bumperw : Bumper, ISimpleEnt {
  public Template parent { get; set; }
  public void relposTo(Vector2 pos, Vector2 ls) {
    rpp = pos;
    anchor = rpp+toffset+twoffset;
    if(!Active)UpdatePosition();
  }
  
  Vector2 rpp;
  public Vector2 toffset {get;set;}
  Vector2 twoffset = Vector2.Zero;
  public Bumperw(EntityData e, Vector2 o):base(e,o){
    var pc = Get<PlayerCollider>();
    var orig = pc.OnCollide;
    pc.OnCollide = (Player p)=>{
      bool flag = respawnTimer<=0;
      orig(p);
      if(flag) parent?.GetFromTree<TemplateTriggerModifier>()?.OnTrigger(new TemplateTriggerModifier.TouchInfo(p,TemplateTriggerModifier.TouchInfo.Type.bumper));
    };
    Tween tw = Get<Tween>();
    if(tw == null) return;
    Vector2 delta = e.Nodes[0]-e.Position;
    tw.OnUpdate = (Tween t)=>{
      if(goBack){
        twoffset = Vector2.Lerp(delta,Vector2.Zero,t.Eased);
      } else {
        twoffset = Vector2.Lerp(Vector2.Zero,delta,t.Eased);
      }
      anchor = rpp+toffset+twoffset;
    };

  }
  public override void Update() {
    anchor = rpp+toffset+twoffset;
    base.Update();
  }
  public void setOffset(Vector2 ppos) {
    toffset=Position-ppos;
    rpp = ppos;
    Depth+=parent.depthoffset;
  }
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0) Visible = vis>0;
    if(col!=0) Collidable = col>0;
    if(act!=0) Active = act>0;
  }
}

public class PufferW:Component, ISimpleWrapper{
  public Entity wrapped=>e;
  Puffer e;
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  bool displaced;
  TemplateDisappearer.vcaTracker vca = new();
  bool ownCollidable;
  Vector2 origin;
  public PufferW(Level l, EntityData d, Vector2 o):this(l,new Puffer(d,o)){}
  public PufferW(Level l, Puffer p):base(false,false){
    l.Add(e = p);
    e.Add(this);
    ResetEvents.LazyEnable(typeof(PufferW));
    origin = e.anchorPosition;
  }
  void ITemplateChild.setOffset(Vector2 ppos)=>toffset = origin-ppos;
  void ITemplateChild.relposTo(Vector2 ploc, Vector2 ls){
    Vector2 npos = ploc+toffset;
    e.returnCurve.End = e.startPosition = npos;
    Vector2 del = npos-origin;
    if(del!=Vector2.Zero && e.state != Puffer.States.Hit && (e.anchorPosition-origin).LengthSquared()<=16){
      if(!(e.MoveH(del.X)||e.MoveV(del.Y))){
        e.anchorPosition+=del;
        e.returnCurve.Begin+=del;
        e.returnCurve.Control+=del;
      }
    }
    origin = npos;
  }
  void ITemplateChild.parentChangeStat(int v, int c, int a){
    vca.Align(v,c,a);
  }
  [ResetEvents.OnHook(typeof(Puffer), nameof(Puffer.GotoIdle))]
  static void Hook(On.Celeste.Puffer.orig_GotoIdle orig, Puffer p){
    orig(p);
    if(p.Get<PufferW>() is {} w){
      w.ownCollidable = true;
      p.Visible = w.ownCollidable && w.vca.Visible;
      p.Collidable = w.ownCollidable && w.vca.Collidable;
    }
  }
  [ResetEvents.OnHook(typeof(Puffer), nameof(Puffer.GotoGone))]
  static void Hook(On.Celeste.Puffer.orig_GotoGone orig, Puffer p){
    orig(p);
    if(p.Get<PufferW>() is {} w) w.ownCollidable = false;
  }
  [ResetEvents.OnHook(typeof(Puffer),nameof(Puffer.ProximityExplodeCheck))]
  static bool Hook(On.Celeste.Puffer.orig_ProximityExplodeCheck orig, Puffer p){
    return (p.Get<PufferW>() is not {} w || w.vca.Collidable) && orig(p);
  }
}
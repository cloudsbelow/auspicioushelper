


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

public class PufferW: Component, ISimpleWrapper{
  public Entity wrapped=>e;
  Puffer e;
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  Vector2 origin;
  bool disconnected=false;
  public PufferW(Puffer p):base(true,false){
    OverrideVisualComponent.Get(e=p).AddToOverride(new(IOverrideVisuals.blocker, -29999, false, true));
    ResetEvents.Hooks<PufferW>.enable();
    origin = e.startPosition = e.anchorPosition;
  }
  public override void Update() {
    if(e.hitSpeed!=Vector2.Zero || e.state==Puffer.States.Hit) disconnected=true;
    if(e.state==Puffer.States.Gone) disconnected=false;
  }
  void ITemplateChild.setOffset(Vector2 ppos)=>toffset = origin-ppos;
  void ITemplateChild.relposTo(Vector2 ploc, Vector2 ls){
    Vector2 npos = ploc+toffset;
    e.returnCurve.End = e.startPosition = npos;
    Vector2 del = npos-origin;
    origin = npos;
    if(!disconnected){
      switch(e.state){
        case Puffer.States.Hit:
          disconnected=true;
          return;
        
        case Puffer.States.Gone:
          e.returnCurve.End = origin;
          e.returnCurve.Control += del/2;
          //e.returnCurve.Begin += del;
          break;

        case Puffer.States.Idle: default:
          Vector2 sine = e.idleSine!=null? new Vector2(e.idleSine.Value * 3f, e.idleSine.ValueOverTwo * 2f):Vector2.Zero;
          Vector2 pdel = origin+sine - e.ExactPosition;
          disconnected |= e.MoveH(pdel.X);
          disconnected |= e.MoveV(pdel.Y);
          break;
      }
    }
  }
  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    if(act!=0) e.Active=act>0;
    if(col!=0) BlockingCollider.SetCol(e, col>0);
    if(vis!=0){
      OverrideVisualComponent.Get(e).SetStealUse(IOverrideVisuals.blocker, vis<0, true);
    }
  }
}
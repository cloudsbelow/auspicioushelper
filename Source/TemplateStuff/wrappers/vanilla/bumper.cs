


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
      DebugConsole.Write("HJere",p);
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
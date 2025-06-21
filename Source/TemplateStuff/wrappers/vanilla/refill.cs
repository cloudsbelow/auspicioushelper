


using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

[CustomEntity("auspicioushelper/CustomRefill")]
public class RefillW:Refill,ISimpleEnt {
  public Template parent {get;set;}
  public Template.Propagation prop=>Template.Propagation.None;
  public float respawnTime = 2.5f;
  public RefillW(EntityData d, Vector2 offset):base(d,offset){
    hooks.enable();
    respawnTime = d.Float("respawnTimer",2.5f);
    if(respawnTime!=2.5f) Get<PlayerCollider>().OnCollide = CustomOnPlayer;
  }
  public Vector2 toffset {get;set;}
  public void setOffset(Vector2 ppos){
    toffset = Position-ppos;
    Depth+=parent.depthoffset;
  }
  public void relposTo(Vector2 loc, Vector2 ls){
    Position = toffset+loc;
  }

  bool selfCol = true;
  bool parentCol = true;
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0){
      parentCol = col>0;
      if(col>0)Collidable = selfCol;
      else{
        selfCol=Collidable;
        Collidable = false;
      }
    }
    if(act!=0) Active = act>0;
  }
  void CustomOnPlayer(Player p){
    if (p.UseRefill(twoDashes)){
      Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", Position);
      Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
      Collidable = false;
      Add(new Coroutine(RefillRoutine(p)));
      respawnTimer = respawnTime;
    }
  }

  static void respawnHook(On.Celeste.Refill.orig_Respawn orig, Refill self){
    if(self is RefillW rw){
      rw.Collidable = false;
      orig(rw);
      rw.selfCol = true;
      rw.Collidable = rw.parentCol;
      rw.Depth+=rw.parent?.depthoffset??0;
    } else orig(self);
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Refill.Respawn+=respawnHook;
  }, void ()=>{
    On.Celeste.Refill.Respawn-=respawnHook;
  });
}
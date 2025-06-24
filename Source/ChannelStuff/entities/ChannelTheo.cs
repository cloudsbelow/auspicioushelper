


using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[Tracked]
[CustomEntity("auspicioushelper/ChannelTheo")]
public class ChannelTheo:TheoCrystal, IChannelUser{
  public string channel {get; set;}
  public bool active=false;
  public bool switchThrown;
  public bool swapPos;
  public bool swapPosCareful;
  public float playerfrac;
  public float theofrac;
  public static Player lastPickup;
  public ChannelTheo(EntityData data, Vector2 offset):base(data, offset){
    
    channel = data.Attr("channel","");
    switchThrown = data.Bool("switch_thrown_momentum",false);
    swapPos = data.Bool("swap_thrown_positions",false);
    swapPosCareful = data.Bool("swap_thrown_positions_nodie",false);
    if(data.Bool("take_velocity",false)){
      playerfrac =0;
      theofrac = 1;
    } else {
      playerfrac = data.Float("player_momentum_weight",1);
      theofrac = data.Float("theo_momentum_weight",0);
    };
    Hold.OnPickup = OnPickupHook;
    hooks.enable();
  }
  public static void OnDie(On.Celeste.TheoCrystal.orig_Die orig, TheoCrystal self){
    if((self.Scene is Level L) && L.Transitioning){
      self.RemoveSelf();
    } else {
      orig(self);
    }
  }
  public override void Update(){
    if((this.Scene is Level L) && L.Transitioning && !Hold.IsHeld) return;
    base.Update();
  }
  public override void Added(Scene scene){
    base.Added(scene);
    ChannelState.watch(this);
    setChVal(ChannelState.readChannel(channel));
  }
  public void setChVal(int val){
    active = (val&1)==1;
  }
  public static void PlayerThrowHook(On.Celeste.Player.orig_Throw orig, Player self){
    Holdable held = self.Holding;
    orig(self);
    if(held!=null && held.Entity is ChannelTheo t){
      if(t.switchThrown && t.active){
        Vector2 temp = self.Speed;
        self.Speed = t.Speed;
        t.Speed = temp;
      }
      if(t.swapPos && t.active){
        Vector2 temp = self.Position;
        self.Position = t.Position;
        t.Position = temp;
        if(Collide.Check(self, self.Scene.Tracker.GetEntities<Solid>())){
          self.Die(self.Speed);
        }
      }
      if(t.swapPosCareful && t.active){
        float temp = self.Position.Y;
        self.MoveToY(t.Position.Y);
        t.MoveToY(temp);
      }
    }
  }
  
  public static bool PickupHook(On.Celeste.Holdable.orig_Pickup orig, Holdable self, Player player){
    lastPickup = player;
    return orig(self, player);
  }
  public void OnPickupHook(){
    if(lastPickup != null && active){
      lastPickup.Speed = lastPickup.Speed*playerfrac+Speed*theofrac;
    }
    base.OnPickup();
  }
  static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.Throw += PlayerThrowHook;
    On.Celeste.Holdable.Pickup += PickupHook;
    On.Celeste.TheoCrystal.Die += OnDie;
  }, void ()=>{
    On.Celeste.Player.Throw -= PlayerThrowHook;
    On.Celeste.Holdable.Pickup -= PickupHook;
    On.Celeste.TheoCrystal.Die -= OnDie;
  });
}
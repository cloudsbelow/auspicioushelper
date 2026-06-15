
using System;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Monocle;


namespace Celeste.Mod.auspicioushelper;
[Tracked]
internal class JumpListener:Component{
  Action<int> OnJump;
  int flags;
  public JumpListener(Action<int> jumpcallback, int flags=15):base(true,false){
    this.flags=flags;
    OnJump=jumpcallback;
    ResetEvents.Hooks<JumpListener>.enable();
  } 
  static void alertJumpListeners(int type){
    foreach(JumpListener l in Engine.Scene.Tracker.GetComponents<JumpListener>()){
      if((l.flags & type)!=0 && l.OnJump!= null) l.OnJump(type);
    }
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.Jump))]
  static void JumpHook(On.Celeste.Player.orig_Jump orig, Player p, bool vfx, bool sfx){
    orig(p, vfx, sfx);
    alertJumpListeners(1);
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.WallJump))]
  static void WallHook(On.Celeste.Player.orig_WallJump orig, Player p, int dir){
    orig(p,dir);
    alertJumpListeners(2);
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.SuperJump))]
  static void SuperHook(On.Celeste.Player.orig_SuperJump orig, Player p){
    orig(p);
    alertJumpListeners(4);
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.SuperWallJump))]
  static void SuperWallHook(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir){
    orig(self, dir);
    alertJumpListeners(8);
  }
}

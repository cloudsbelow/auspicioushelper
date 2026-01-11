


using System;
using System.Drawing;
using Celeste.Mod.auspicioushelper;
using IL.Monocle;
namespace Celeste.Mod.auspicioushelper;
public class RcbHelper{
  static int rightframes;
  static int leftframes;
  static float right;
  static float left;

  /** 
    Left/right indicate the facing of the wall. If the player
    is checking the wall to their left, we use RIGHT.
  */
  public static void give(bool dir_right, float pos, int frames=2){
    ResetEvents.LazyEnable(typeof(RcbHelper));
    if(dir_right){
      if(rightframes>0) right = Math.Max(pos,right);
      else right = pos;
      rightframes = frames;
    } else {
      if(leftframes>0) left = Math.Min(pos,left);
      else left = pos;
      leftframes=frames;
    }
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.WallJumpCheck))]
  public static bool walljumpCheckHook(On.Celeste.Player.orig_WallJumpCheck orig, Player p, int dir){
    bool o = orig(p,dir);
    
    if(o || !((rightframes>0 && dir<0)||(leftframes>0 && dir>0))) return o;
    FloatRect f = new FloatRect(p);
    f.expandXto(dir<0? right:left);
    return true;
  }
  [ResetEvents.OnHook(typeof(Player),nameof(Player.Update))]
  public static void playerUpdateHook(On.Celeste.Player.orig_Update orig, Player p){
    orig(p);
    if(rightframes>0) rightframes--;
    if(leftframes>0) leftframes--;
  }
}
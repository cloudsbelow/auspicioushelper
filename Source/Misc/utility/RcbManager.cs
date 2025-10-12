


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
  public static bool walljumpCheckHook(On.Celeste.Player.orig_WallJumpCheck orig, Player p, int dir){
    bool o = orig(p,dir);
    if(portalHooks.hooks.active){
      FloatRect check = new FloatRect(p);
      check.expandXto(dir<0? p.Left-3:p.Right+3);
      foreach(PortalGateH t in p.Scene.Tracker.GetEntities<PortalGateH>()){
        if(check.x<=t.Position.X && check.x+check.w>=t.Position.X){
          o = o && !(check.y>=t.Position.Y && check.y+check.h<=t.Position.Y+t.height);
        }
        if(check.x<=t.npos.X && check.x+check.w>=t.npos.X){
          o = o && !(check.y>=t.npos.Y && check.y+check.h<=t.npos.Y+t.height);
        }
      }
    }
    if(o || !((rightframes>0 && dir<0)||(leftframes>0 && dir>0))) return o;
    FloatRect f = new FloatRect(p);
    f.expandXto(dir<0? right:left);
    return true;
  }
  public static void playerUpdateHook(On.Celeste.Player.orig_Update orig, Player p){
    orig(p);
    if(rightframes>0) rightframes--;
    if(leftframes>0) leftframes--;
  }
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.WallJumpCheck +=walljumpCheckHook;
    On.Celeste.Player.Update += playerUpdateHook;
  },()=>{
    On.Celeste.Player.WallJumpCheck-=walljumpCheckHook;
    On.Celeste.Player.Update-=playerUpdateHook;
  },auspicioushelperModule.OnEnterMap);
}
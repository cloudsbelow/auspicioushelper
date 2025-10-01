

using System;

namespace Celeste.Mod.auspicioushelper;

//also gravityhelper 
public static class MaddiesIop{
  public static Type jt;
  public static Type dt;
  public static Type at;
  public static Type samah;
  public static Util.FieldHelper<bool> side;
  public static Util.FieldHelper<Solid> playerInteractingSolid;
  public static HookManager hooks = new(()=>{
    jt = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.SidewaysJumpThru");
    dt = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru");
    at = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.AttachedSidewaysJumpThru");
    samah = Util.getModdedType("GravityHelper","Celeste.Mod.GravityHelper.Entities.UpsideDownJumpThru");
    if(jt == null) return;
    side = new Util.FieldHelper<bool>(jt, "AllowLeftToRight", true);
    playerInteractingSolid = new(at,"playerInteractingSolid");
  }, auspicioushelperModule.OnEnterMap);
}
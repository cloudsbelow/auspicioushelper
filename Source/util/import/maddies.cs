

using System;

namespace Celeste.Mod.auspicioushelper;

//also gravityhelper 
public static class MaddiesIop{
  public static Type jt;
  public static Type dt;
  public static Type samah;
  public static Util.FieldHelper<bool> side;
  public static HookManager hooks = new(()=>{
    jt = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.SidewaysJumpThru");
    dt = Util.getModdedType("MaxHelpingHand","Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru");
    samah = Util.getModdedType("GravityHelper","Celeste.Mod.GravityHelper.Entities.UpsideDownJumpThru");
    if(jt == null) return;
    side = new Util.FieldHelper<bool>(jt, "AllowLeftToRight", true);
  }, auspicioushelperModule.OnEnterMap);
}
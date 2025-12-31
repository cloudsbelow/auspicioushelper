

using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper;

[ModImportName("GravityHelper")]
public class GelperIop{
  public static Func<string, int> GravityTypeToInt;
  public static Action<Actor,int,float> SetActorGravity;
  public static Func<Actor,bool> IsActorInverted;
  public static void TryFlip(Actor a){
    if(SetActorGravity!=null) SetActorGravity(a,2,1);
  }
  public static bool IsFlipped(Actor a)=>(IsActorInverted is {} fn)?fn(a):false;
  [OnLoad]
  static void Load(){
    typeof(GelperIop).ModInterop();
  }
}
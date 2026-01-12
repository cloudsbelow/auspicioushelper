



using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper.Import;

public static class CommunalHelperIop{
  #pragma warning disable CS0649
  [ModImportName("CommunalHelper.DashStates")]
  public static class CommunalHelperImports {
    public static Func<int> GetDreamTunnelDashState;
    public static Func<bool> HasDreamTunnelDash;
    public static Func<int> GetDreamTunnelDashCount;
  }
  #pragma warning restore CS0649

  static int tunnelstate = -1;
  static int lastDashcount;
  static bool DoTunnel(Player p){
    lastDashcount = CommunalHelperImports.GetDreamTunnelDashCount();
    return p.StateMachine.State == CommunalHelperImports.GetDreamTunnelDashState() || p.StateMachine.State == Player.StDreamDash;
  }
  static bool ExitCond(Player p){
    return CommunalHelperImports.GetDreamTunnelDashCount()!=lastDashcount;
  }
  public static bool InTunnel(Player p)=>CommunalHelperImports.GetDreamTunnelDashState is {} fn && p.StateMachine.State==fn();
  public static void load(){
    typeof(CommunalHelperImports).ModInterop();
    if(CommunalHelperImports.GetDreamTunnelDashState!=null){
      tunnelstate = CommunalHelperImports.GetDreamTunnelDashState();
      DebugConsole.Write($"Setting up communal helper interop.");
      Anti0fZone.skipNormal.Add(DoTunnel);
      Anti0fZone.exitNormal.Add(ExitCond);
      Anti0fZone.runNaive.Add(DoTunnel);
      Anti0fZone.exitNaive.Add(ExitCond);
    }
  }
}
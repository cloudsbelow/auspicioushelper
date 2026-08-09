



using System;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper;

public static class ExtendedCameraIop{
  #pragma warning disable CS0649
  [ModImportName("ExtendedCameraDynamics")]
  public static class ExCameraIop {
    public static Func<bool> ExtendedCameraHooksEnabled;
    public static Func<int> BufferWidthOverride;
    public static Func<int> BufferHeightOverride;
    public static Func<Level,Vector2> GetCameraDimensions;
  }
  #pragma warning restore CS0649

  static bool enabled=false;
  static bool prototype=false;
  static Func<int> protoScale = null;
  public static void printCamerastatus(Level l){
    if(!enabled){
      DebugConsole.Write($"Excamera not enabled");
      return;
    }
    DebugConsole.Write(
      $"{l} camera overriden {ExCameraIop.ExtendedCameraHooksEnabled()}:"+
      $" {{{ExCameraIop.BufferWidthOverride()},{ExCameraIop.BufferHeightOverride()}}} buffer size"+
      $" and {ExCameraIop.GetCameraDimensions(l)} camera dim"
    );
  }
  public static Tuple<int,int> cameraSize(){
    if(!enabled || !ExCameraIop.ExtendedCameraHooksEnabled()){
      var scale = prototype? protoScale():1;
      return new(320*scale,180*scale);
    }
    return new(ExCameraIop.BufferWidthOverride(),ExCameraIop.BufferHeightOverride());
  }
  public static void load(){
    typeof(ExCameraIop).ModInterop();
    if(ExCameraIop.ExtendedCameraHooksEnabled!=null){
      DebugConsole.Write("Doing exCamera setup");
      enabled=true;
    }
    if(Everest.Modules.FirstOrDefault(m=>m.Metadata.Name == "ZoomOutHelperPrototype")?.GetType() is {} protoType){
      prototype = true;
      protoScale = protoType?.GetProperty("CanvasScale",Util.GoodBindingFlags).GetGetMethod().CreateDelegate<Func<int>>();
    }
  }
}
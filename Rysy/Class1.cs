using Rysy;
using Rysy.Mods;

namespace auspicioushelper.Rysy;

public sealed class YourModModule : ModModule{
  public override void Load(){
    base.Load();
    ComponentRegistry.Add(new ConnectedTileRender());
    // Called when your mod is loaded.
    // Use ComponentRegistry to register additional components which will have the same lifetime as your mod.
  }
}
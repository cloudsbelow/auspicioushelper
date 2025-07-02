
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public static class HookVanilla{
  static void heartPlayerTemplateHook(On.Celeste.HeartGem.orig_OnPlayer orig, HeartGem self, Player p){
    ChildMarker cm= self.Get<ChildMarker>();
    orig(self,p);
    if(cm!=null) new TriggerInfo.EntInfo("Heart",cm.Entity).PassTo(cm.parent);
  }
  static HookManager heartHooks = new HookManager(()=>{
    On.Celeste.HeartGem.OnPlayer+=heartPlayerTemplateHook;
  },void ()=>{
    On.Celeste.HeartGem.OnPlayer-=heartPlayerTemplateHook;
  },auspicioushelperModule.OnEnterMap);
  public static HeartGem HeartGem(Level l, LevelData d, Vector2 o, EntityData e){
    var hg = new HeartGem(e,o);
    if(EntityParser.currentParent!=null) hg.Add(new ChildMarker(EntityParser.currentParent));
    heartHooks.enable();
    return hg;
  }
}




using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[Tracked]
public class BeforeAfterRender:Component{
  Action b;
  Action a;
  public BeforeAfterRender(Action before, Action after=null):base(false,false){
    b=before; a=after;
  }
  public static Util.HybridSet<Action> prebefore = new();
  public static Util.HybridSet<Action> postbefore = new();
  public static Util.HybridSet<Action> preafter = new();
  public static Util.HybridSet<Action> postafter = new();
  [OnLoad.OnHook(typeof(Level),nameof(Level.BeforeRender))]
  static void Before(On.Celeste.Level.orig_BeforeRender orig, Level self){
    foreach(var f in prebefore) f();
    orig(self);
    foreach(BeforeAfterRender c in self.Tracker.GetComponents<BeforeAfterRender>()){
      if(c.b!=null) c.b();
    }
    foreach(var f in postbefore) f();
    TileOccluder.HandleThing(self);
  }
  [OnLoad.OnHook(typeof(Level),nameof(Level.AfterRender))]
  static void After(On.Celeste.Level.orig_AfterRender orig, Level self){
    foreach(var f in preafter) f();
    orig(self);
    foreach(BeforeAfterRender c in self.Tracker.GetComponents<BeforeAfterRender>()){
      if(c.a!=null) c.a();
    }
    foreach(var f in postafter) f();
  }
}
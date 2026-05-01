



using System;
using System.Collections.Generic;
using Monocle;
using MonoMod.Cil;

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

// public class DepthOffsetManager:Component{
//   int offset;
//   public static void ApplyOffset(Entity e, int offset){
//     if(e.Get<DepthOffsetManager>() is not {} d) e.Add(d=new());
//     d.offset+=offset;
//   }
//   public static void ApplyRoot(Entity e, int root){
//     if(e.Get<DepthOffsetManager>() is not {} d) e.Add(d=new());
//     d.offset = root-(e.Depth-d.offset);
//   }
//   public DepthOffsetManager():base(false,false){}
//   static int depthWithOffset(Entity e, int orig){
//     if(e.Get<DepthOffsetManager>() is {} d) return orig+d.offset;
//     return orig;
//   }
//   [OnLoad.ILHook(typeof(Entity),nameof(Entity.Depth),Util.HookTarget.PropSet)]
//   static void Hook(ILContext ctx){
//     var c = new ILCursor(ctx);
//     c.EmitLdarg0();
//     c.EmitLdarg1();
//     c.EmitDelegate(depthWithOffset);
//     c.EmitStarg(1);
//   }
// }

internal struct RespawnCountdown(ChannelState.FloatCh timeFac){
  float timer=0;
  public void Begin()=>timer=1;
  public bool Prog(float inc){
    if(timeFac<0) timer=1;
    else if(timeFac == 0) timer=0;
    else timer-=inc/timeFac;
    return timer<=0;
  }
  public bool isNeg()=>timeFac<0; 
  public bool Active=>timer>0;
}



using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper.iop;


/* 
  Paste this entire class into your mod to use the interop
*/
[ModImportName("auspicioushelper.channels2")]
public static class ChannelIop2{
  //Read the channel at a given string. Can be given modified channels.
  public static Func<string,double> readChannel;

  //Set the channel at the specified string. Will propagate to all derived modified channels.
  public static Action<string, double> setChannel;

  //Get a watcher component that will call the given function with the new value of the
  //channel whenever it changes. Can be used with modified channels. Function is not called
  //if channel is not set to a different value.
  public static Func<string, Action<double>, Component> getWatcher;

  //Get the current value of the channel the watcher is looking at 
  public static Func<Component, double> watcherValue;

  /** 
    For use with the mathcontroller. Identifier should be the name of the function in
    channel math controller code. For example, if you had the function
    DoThing<fish,cow>(1,2,3) in your code, the interop function registered with the 
    string DoThing would be called with arguments List<string>{"DoThing", "fish", "cow"} 
    and List<double>{1,2,3}. Look at ChannelStuff/controllers/ChannelMathController for examples.

    Please deregister your functions when your mod unloads (polite)
  */
  public static Action<string, Func<List<string>,List<double>,double>> registerIopFunc;
  public static Action<string, Func<List<string>,List<double>,double>>deregisterIopFunc;
}


[ModExportName("auspicioushelper.channels2")]
public static class ChannelIopExp2{
  public static double readChannel(string ch)=>ChannelState.readChannel(ch);
  public static void setChannel(string ch, double val)=>ChannelState.SetChannel(ch,val);
  public static Component getWatcher(string ch, Action<double> onChannelChange)=>new ChannelTracker(ch, onChannelChange);
  public static double watcherValue(Component w){
    if(w is ChannelTracker ct) return ct.value;
    return 0;
  }
  public static void registerIopFunc(string identifier, Func<List<string>,List<double>,double> fn){
    ChannelMathController.registerInterop(identifier,fn);
  }
  public static void deregisterIopFunc(string identifier, Func<List<string>,List<double>,double> fn){
    ChannelMathController.deregisterInterop(identifier, fn);
  }
}



/** Old methods kept for compatibility */
[ModExportName("auspicioushelper.channels")]
public static class ChannelIopExp{
  public static int readChannel(string ch){
    return (int)ChannelState.readChannel(ch);
  }
  public static void setChannel(string ch, int val){
    ChannelState.SetChannel(ch,val);
  }
  public static Component getWatcher(string ch, Action<int> onChannelChange){
    return new ChannelTracker(ch, (double d)=>onChannelChange((int) d));
  }
  public static int watcherValue(Component w){
    if(w is ChannelTracker ct) return (int)ct.value;
    return 0;
  }
  public static void registerIopFunc(string identifier, Func<List<string>,List<int>,int> fn){
    ChannelMathController.registerInterop(identifier,fn);
  }
  public static void deregisterIopFunc(string identifier, Func<List<string>,List<int>,int> fn){
    ChannelMathController.deregisterInterop(identifier, fn);
  }
}


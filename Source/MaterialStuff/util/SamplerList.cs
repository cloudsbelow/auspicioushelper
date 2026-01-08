


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.auspicioushelper;

public class SamplerList{
  List<Tuple<int, SamplerState>> list = null;
  public SamplerList(){}
  static SamplerState currentForce = null;
  public static SamplerState currentS0(SamplerState fallback)=>currentForce??fallback;
  SamplerState r0v;
  public static Dictionary<string, SamplerState> predefinedSamplerStates = new(){
    {nameof(SamplerState.AnisotropicClamp),SamplerState.AnisotropicClamp}, 
    {nameof(SamplerState.AnisotropicWrap),SamplerState.AnisotropicWrap}, 
    {nameof(SamplerState.PointClamp),SamplerState.PointClamp},    {"pc",SamplerState.PointClamp}, 
    {nameof(SamplerState.PointWrap),SamplerState.PointWrap},      {"pw",SamplerState.PointWrap}, 
    {nameof(SamplerState.LinearClamp),SamplerState.LinearClamp},  {"lc",SamplerState.LinearClamp},
    {nameof(SamplerState.LinearWrap),SamplerState.LinearWrap},    {"lw",SamplerState.LinearWrap}, 
  };
  public SamplerList Add(int slot, SamplerState state){
    if(slot==0) r0v = state;
    if(list == null) list = new();
    foreach(var pair in list) if(pair.Item1 == slot){
      DebugConsole.WriteFailure("Binding multiple sampler types to same slot");
      list.Remove(pair);
      break;
    }
    list.Add(new(slot, state));
    return this;
  }
  public void Add(int slot, string state){
    if(predefinedSamplerStates.TryGetValue(state, out var ss)) Add(slot, ss);
    else {
      DebugConsole.WriteFailure($"{state} is currently not a valid sampelrstate");
    }
  }
  public struct OverridenSamplerstate:IDisposable{
    List<Tuple<int, SamplerState>> orig=null;
    SamplerState origr0;
    GraphicsDevice gd;
    public OverridenSamplerstate(SamplerList from, GraphicsDevice on){
      gd=on;
      orig=new();
      origr0 = currentForce;
      if(from.r0v != null) currentForce = from.r0v;
      foreach(var pair in from.list){
        orig.Add(new(pair.Item1,gd.SamplerStates[pair.Item1]));
        gd.SamplerStates[pair.Item1]=pair.Item2;
      }
    }
    public OverridenSamplerstate(){}
    void IDisposable.Dispose(){
      currentForce = origr0;
      if(orig==null) return;
      foreach(var pair in orig){
        gd.SamplerStates[pair.Item1]=pair.Item2;
      }
    }
  }
  public OverridenSamplerstate Apply(GraphicsDevice gd){
    return list==null?new OverridenSamplerstate():new OverridenSamplerstate(this,gd);
  }
}
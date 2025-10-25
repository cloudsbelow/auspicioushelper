

using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;
[Tracked]
[CustomEntity("auspicioushelper/DecalRecolor")]
[MapenterEv(nameof(Preload))]
public class DecalRecolor:Entity{
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  [Import.SpeedrunToolIop.Static]
  static Util.Trie<Util.SetStack<Util.ColorRemap>> Recolors=new();
  public class DecalRerouter:IDisposable{
    static Stack<Util.ColorRemap> remaps = new();
    public static Util.ColorRemap Current=>remaps.Count>0?remaps.Peek():null;
    Util.ColorRemap mine;
    public DecalRerouter(Util.ColorRemap r){
      remaps.Push(mine=r);
    }
    void IDisposable.Dispose(){
      if(remaps.Pop()!=mine) DebugConsole.WriteFailure("non stack ordered application of recolor wrapper",true);
    }
  }
  enum Scopes{
    invalid, wholeMap, wholeRoom, areaOnly
  }
  static void Preload(EntityData d){
    hooks.enable();
    if(d.Enum<Scopes>("WholeMap",Scopes.wholeMap)==Scopes.wholeMap)Apply(d);
  }
  static void Apply(string tex, string val){
    DebugConsole.Write($"Applying recolor {val} to {tex}");
    if(!Recolors.TryGet(tex, out var st)) Recolors.Add(tex, st=new());
    st.Push(Util.ColorRemap.Get(val));
  }
  static void Apply(EntityData d){
    if(d.tryGetStr("texture", out string texstr))Apply(texstr,d.Attr("recolor"));
    if(d.tryGetStr("extraList", out string manytexstr)){
      foreach(var v in manytexstr.Split(';')) if(!string.IsNullOrWhiteSpace(v)) continue;
    }
  }
  static void DecalCtor(Action<Decal,string,Vector2,Vector2,int> orig, Decal self, string tex, Vector2 pos, Vector2 scale, int depth){
    if(Recolors.TryGet(tex, out var st) && st.Count>0) { 
      Util.ColorRemap remap=st.Peek();
      using(new DecalRerouter(remap)) orig(self,tex,pos,scale,depth); 
      DebugConsole.Write(tex);
      self.textures = self.textures.Map(remap.RemapTex);
    } else orig(self,tex,pos,scale,depth);
  }
  static void DecalAwake(On.Celeste.Decal.orig_Awake orig, Decal d, Scene s){
    if(d.Get<DecalMarker>() is {} dm && Recolors.TryGet(dm.texstr, out var st) && st.Count>0){ 
      using(new DecalRerouter(st.Peek())) orig(d,s); 
    } else orig(d,s);
  }
  static void DecalAdded(On.Celeste.Decal.orig_Added orig, Decal d, Scene s){
    if(d.Get<DecalMarker>() is {} dm && Recolors.TryGet(dm.texstr, out var st) && st.Count>0){ 
      using(new DecalRerouter(st.Peek())) orig(d,s); 
    } else orig(d,s);
  }
  public override void Added(Scene scene) {
    base.Added(scene);
    RemoveSelf();
  }
  static Hook ctorhook;
  public static HookManager hooks = new(()=>{
    On.Celeste.Decal.Added+=DecalAdded;
    On.Celeste.Decal.Awake+=DecalAwake;
    ctorhook = new Hook(typeof(Decal).GetMethod(nameof(Decal.orig_ctor)), DecalCtor);
  },()=>{
    On.Celeste.Decal.Added-=DecalAdded;
    On.Celeste.Decal.Awake-=DecalAwake;
    ctorhook.Dispose();
  });
}


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
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnEnter)]
  [Import.SpeedrunToolIop.Static]
  static Util.Trie<Util.SetStack<Util.ColorRemap>> Recolors=new();
  public class DecalRerouter:IDisposable{
    static Stack<Util.ColorRemap> remaps;
    public static Util.ColorRemap Current=>remaps.Count>0?remaps.Peek():null;
    public DecalRerouter(Util.ColorRemap r){
      remaps.Push(r);
    }
    void IDisposable.Dispose()=>remaps.Pop();
  }
  static void Preload(EntityData d){
    if(d.Bool("WholeMap",true))Apply(d);
  }
  static void Apply(string tex, string val){
    if(!Recolors.TryGet(tex, out var st)) Recolors.Add(tex, st=new());
    st.Push(Util.ColorRemap.Get(val));
  }
  static void Apply(EntityData d){
    if(d.tryGetStr("texture", out string texstr))Apply(texstr,d.Attr("recolor"));
    if(d.tryGetStr("extraList", out string manytexstr)){
      foreach(var v in manytexstr.Split(';')) if(!string.IsNullOrWhiteSpace(v)) continue;
    }
  }
  static Decal DecalCtor(Func<string, Vector2, Vector2, int, Decal> ctor, string tex, Vector2 pos, Vector2 scale, int depth, Decal self){
    if(Recolors.TryGet(tex, out var st) && st.Count>0) { 
      using(new DecalRerouter(st.Peek())) return ctor(tex,pos,scale,depth); 
    } else return ctor(tex,pos,scale,depth);
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
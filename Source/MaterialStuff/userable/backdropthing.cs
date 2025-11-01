


using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Celeste.Mod.Helpers;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

public class BackdropCapturer{
  static BackdropRenderer renderer = new();
  public class BackdropUnlock:IDisposable{
    static int num=0;
    public BackdropUnlock(){num++;}
    void IDisposable.Dispose()=>num--;
    static public bool unlocked => num>0;
  }
  public class BackdropRef:MaterialResource{
    public void onChange(bool c){
      if(c) b.Visible=true;
      else b.Visible=origvis;
    }
    Backdrop b;
    public bool origvis;
    public BackdropRef(Backdrop b):base(null){
      a=onChange;
      this.b=b;
    }
  }
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  static ConditionalWeakTable<Backdrop,BackdropRef> back = new();
  public class CapturedBackdrops:IMaterialLayerSimple{
    public MaterialLayerInfo info {get;set;} = new(true, int.MaxValue/2);
    List<Backdrop> captured = new();
    public static void FixFromSrt(Level l){
      foreach(var g in groups) g.Value.last = null;
    }
    bool IMaterialLayer.checkdo()=>info.enabled;
    RenderTargetPool.RenderTargetHandle tex;
    RenderTarget2D IMaterialLayer.outtex=>tex;
    bool IMaterialLayer.autoManageRemoval=>false;
    bool IMaterialLayer.drawInScene=>false;
    string selector;
    public MaterialResource resource;
    Level last = null;
    public CapturedBackdrops(Level l, string selector){
      tex = new(false);
      this.selector = "%"+selector;
      resource = new((bool change)=>{
        if(change){ 
          MaterialPipe.addLayer(this);
        }
        else MaterialPipe.removeLayer(this);
      });
      setupForLevel(l);
    }
    public void setupForLevel(Level l){
      expensiveHooks.enable();
      if(last == l) return;
      foreach(var b in captured){
        if(back.TryGetValue(b, out var br))br.Unuse();
      }
      captured.Clear();
      if(l == null) return;
      last = l;
      foreach(var b in l.Background.Backdrops) if(b.OnlyIn?.Contains(selector)??false){
        captured.Add(b);
        if(!back.TryGetValue(b, out var br))back.Add(b, br=new BackdropRef(b));
        br.Use();
      }
      foreach(var b in l.Foreground.Backdrops) if(b.OnlyIn?.Contains(selector)??false){
        captured.Add(b);
        if(!back.TryGetValue(b, out var br))back.Add(b, br=new BackdropRef(b));
        br.Use();
      }
      DebugConsole.Write($"Backdrop layer {selector} captured {captured.Count} layers");
    }
    public void render(SpriteBatch sb, Camera c){
      MaterialPipe.gd.SetRenderTarget(tex);
      Session s = MaterialPipe.renderingLevel?.Session;
      if(s == null){
        DebugConsole.Write("Null Session in backdrop rendering section");
      }
      setupForLevel(MaterialPipe.renderingLevel);
      bool[] oldvis = new bool[captured.Count];
      for(int i=0; i<captured.Count; i++){
        Backdrop b = captured[i];
        oldvis[i] = captured[i].Visible;
        if(b.ForceVisible) goto yes;
        if(!string.IsNullOrEmpty(b.OnlyIfNotFlag) && s.GetFlag(b.OnlyIfNotFlag)) goto no;
        if(!string.IsNullOrEmpty(b.AlsoIfFlag) && s.GetFlag(b.AlsoIfFlag))goto yes;
        if(b.Dreaming.HasValue && b.Dreaming.Value != s.Dreaming) goto no;
        if(!string.IsNullOrEmpty(b.OnlyIfFlag) && !s.GetFlag(b.OnlyIfFlag)) goto no;
        yes:
          b.Visible=true;
          continue;
        no:
          b.Visible=false;
      }
      renderer.Backdrops = captured;
      renderer.BeforeRender(MaterialPipe.renderingLevel);
      MaterialPipe.gd.SetRenderTarget(tex);
      MaterialPipe.gd.Clear(Color.Transparent);
      using(new BackdropUnlock())renderer.Render(MaterialPipe.renderingLevel);
      renderer.Backdrops = null;
      for(int i=0; i<captured.Count; i++) captured[i].Visible=oldvis[i];
    } 
    public void onEnable(){
      tex.Claim();
    }
    public void onRemove(){
      tex.Free();
    }
    public override string ToString()=>base.ToString()+":"+selector;
  }
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  public static Dictionary<string, CapturedBackdrops> groups = new();
  public static CapturedBackdrops Get(string s){
    if(groups.TryGetValue(s, out var c)) return c;
    return  groups[s] = new(null,s);
  }
  static bool Hook(On.Celeste.Backdrop.orig_IsVisible orig, Backdrop self, Level l){
    bool res = orig(self, l);
    if(back.TryGetValue(self, out var br)){
      br.origvis = res;
      return true;
    }
    return res;
  }
  static void BackdropRoomhook(HashSet<string> s, string name){
    if(string.IsNullOrWhiteSpace(name)) return;
    name=name.Trim();
    if(name[0]=='%') s.Add(name);
  }
  static void RoomParseHook(ILContext ctx){
    ILCursor c = new(ctx);
    if(c.TryGotoNextBestFit(MoveType.Before,
      i=>i.MatchLdloc3(),
      i=>i.MatchLdcI4(42)
    )){
      c.EmitLdloc0();
      c.EmitLdloc3();
      c.EmitDelegate(BackdropRoomhook);
    } else DebugConsole.WriteFailure("Failed to add backdrop hook");
  }
  static bool SkipDelegate(Backdrop b){
    return BackdropUnlock.unlocked || !back.TryGetValue(b, out var br) || br.origvis;
  }
  static void SkipHook(ILContext ctx){
    ILCursor c = new(ctx);
    ILLabel target = null;
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchStloc2(),
      itr=>itr.MatchLdloc2(),
      itr=>itr.MatchLdfld<Backdrop>(nameof(Backdrop.Visible)),
      itr=>itr.MatchBrfalse(out target)
    )){
      c.EmitLdloc2();
      c.EmitDelegate(SkipDelegate);
      c.EmitBrfalse(target);
    } else DebugConsole.WriteFailure("Failed to add skip hook");
  }
  [OnLoad]
  public static HookManager hooks = new(()=>{
    IL.Celeste.MapData.ParseLevelsList+=RoomParseHook;
    IL.Celeste.BackdropRenderer.Render+=SkipHook;
  }, ()=>{
    IL.Celeste.MapData.ParseLevelsList-=RoomParseHook;
    IL.Celeste.BackdropRenderer.Render-=SkipHook;
  });
  public static HookManager expensiveHooks = new(()=>{
    On.Celeste.Backdrop.IsVisible+=Hook;
  },()=>{
    On.Celeste.Backdrop.IsVisible-=Hook;
  },auspicioushelperModule.OnEnterMap);
}
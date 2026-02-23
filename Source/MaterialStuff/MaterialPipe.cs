
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.auspicioushelper;




public static class MaterialPipe {
  [Import.SpeedrunToolIop.Static]
  static List<IMaterialLayer> layers = new List<IMaterialLayer>();
  static public bool check(IMaterialLayer l)=>layers.Contains(l);
  static MaterialPipe(){
    auspicioushelperModule.OnEnterMap.enroll(new ScheduledAction(()=>{
      layers.Clear();
      entering.Clear();
      leaving.Clear();
      toRemove.Clear();
      return false;
    }));
  }
  public static bool dirty;
  public static GraphicsDevice gd;
  public static bool orderFlipped{get;private set;}
  public static Camera camera = new();
  static bool needsImmUpdate;
  static public IntRect clipBounds;
  public static Level renderingLevel; 
  static List<Action> afterRender = new();
  static SamplerList samplers = new SamplerList().Add(1, SamplerState.PointClamp).Add(2, SamplerState.PointClamp);
  static public void addAfterAction(Action a)=>afterRender.Add(a);
  [OnLoad.OnHook(typeof(GameplayRenderer),nameof(GameplayRenderer.Render))]
  public static void GameplayRender(On.Celeste.GameplayRenderer.orig_Render orig, GameplayRenderer self, Scene scene){
    orderFlipped = false;
    var camdim = ExtendedCameraIop.cameraSize();
    RenderTargetPool.Resize(camdim.Item1,camdim.Item2);
    renderingLevel = scene as Level;
    Int2 cloc = Int2.Round(renderingLevel?.Camera.Position??Vector2.Zero);
    clipBounds = new IntRect(cloc.x,cloc.y,camdim.Item1,camdim.Item2).expandAll_(8);
    if(transroutine!=null) transroutine.Update();
    if(layers.Count==0){
      orig(self, scene);
      goto end;
    }
    camera = self.Camera;
    gd = Engine.Instance.GraphicsDevice;
    if(toRemove.Count>0){
      List<IMaterialLayer> nlist = new();
      foreach(var i in layers) if(!toRemove.Contains(i)) nlist.Add(i);
      layers = nlist;
      toRemove.Clear();
    }
    if(dirty) layers.Sort((a, b) => -a.depth.CompareTo(b.depth));
    dirty=false;
    if(blockedAddingEnt.Count>0 || needsImmUpdate){
      foreach(var e in blockedAddingEnt) scene.Add(e);
      scene.Entities.UpdateLists();
      blockedAddingEnt.Clear();
      needsImmUpdate = false;
    }
    
    using(samplers.Apply(gd)){
      foreach(IMaterialLayer l in layers){
        if(l.useMarkingEntity && l.markingEntity.Scene!=scene){
          DebugConsole.Write("Weirdness occurred (should only happen from map reloading and debug menu use)");
          l.markingEntity.RemoveSelf();
          scene.Add(new LayerMarkingEntity(l));
        }
        if(l.usesbg && !orderFlipped && Engine.Instance.scene is Level v){
          gd.SetRenderTarget(GameplayBuffers.Level);
          gd.Clear(v.BackgroundColor);
          v.Background.Render(v);
          orderFlipped = true;
          gd.SetRenderTarget(GameplayBuffers.Gameplay);
        }
        if(l.independent){
          if(l.checkdo()){
            l.render();
            l.diddraw = true;
          }
          else l.diddraw = false;
        }
      }
      gd.SetRenderTarget(GameplayBuffers.Gameplay);
      OverrideVisualComponent.Override(scene);
      orig(self, scene);
      OverrideVisualComponent.Restore(scene);
    }
    renderingLevel = null;
    end:
      if(afterRender.Count>0){
        foreach(var a in afterRender) a();
        afterRender.Clear();
      }
  }

  public static void continueDefault(){
    gd.SetRenderTarget(GameplayBuffers.Gameplay);
    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix);
  }
  public static float GetTransitionAlpha(IMaterialLayer l){
    return transroutine == null? 1:(
      leaving.Contains(l) || entering.Contains(l)? l.transalpha(leaving.Contains(l),camAt):1
    );
  }

  static float camAt;
  static float NextTransitionDuration = 0.65f;
  [Import.SpeedrunToolIop.Static]
  static HashSet<IMaterialLayer> entering = new();
  [Import.SpeedrunToolIop.Static]
  static HashSet<IMaterialLayer> leaving = new();
  static Coroutine transroutine = null;
  [OnLoad.OnHook(typeof(Level),nameof(Level.TransitionTo))]
  static void ontrans(On.Celeste.Level.orig_TransitionTo orig, Level self, LevelData next, Vector2 dir){
    camAt = 0;
    entering.Clear();
    foreach(var l in layers) if(l.autoManageRemoval){
      leaving.Add(l);
    }
    NextTransitionDuration = self.NextTransitionDuration;
    orig(self, next, dir);
    transroutine = new Coroutine(transitionRoutine());
  }
  static IEnumerator transitionRoutine(){
    while(camAt<1){
      camAt = Calc.Approach(camAt, 1f, Engine.DeltaTime / NextTransitionDuration);
      yield return null;
    }
    remLeaving();
    transroutine = null;
    yield break;
  }
  static List<LayerMarkingEntity> blockedAddingEnt = new();
  public static void addLayer(IMaterialLayer l){
    if(!leaving.Remove(l))entering.Add(l);
    if(l.enabled) return;
    l.enabled=true;
    toRemove.Remove(l);
    if(l.useMarkingEntity){
      if(l.markingEntity!=null) throw new Exception("Layer marking entities are leaking");

      if(Engine.Instance.scene is Level lv)lv.Add(new LayerMarkingEntity(l));
      else if(Engine.Instance.scene is LevelLoader ld) ld.Level.Add(new LayerMarkingEntity(l));
      else blockedAddingEnt.Add(new LayerMarkingEntity(l));

      if(UpdateHook.inUpdate) UpdateHook.EnsureUpdateAny();
      else needsImmUpdate = true;
    }
    l.onEnable();
    if(layers.Contains(l)) return;
    dirty = true;
    layers.Add(l);
  }
  [Import.SpeedrunToolIop.Static]
  static HashSet<IMaterialLayer> toRemove = new();
  public static void removeLayer(IMaterialLayer l){
    if(l.enabled==false) return;
    toRemove.Add(l);
    l.enabled = false;
    l.onRemove();
    if(l.useMarkingEntity){
      l.markingEntity.RemoveSelf();
      l.markingEntity=null;
    }
    leaving.Remove(l);
    entering.Remove(l);
  }

  [OnLoad.EverestEvent(typeof(Everest.Events.Player),nameof(Everest.Events.Player.OnDie))]
  public static void onDie(Player p){
    foreach(var l in layers) if(l.autoManageRemoval)leaving.Add(l);
  }
  public static void remLeaving(){
    foreach(IMaterialLayer l in leaving) removeLayer(l);
    leaving.Clear();
  }
  public static void fixFromSrt(){
    foreach(var l in layers) if(l is CachedUserMaterial c)  MaterialController.fixmat(c);
  }

  public static bool backdropReorderDetour(){
    return orderFlipped;
  }
  [OnLoad.ILHook(typeof(Level),nameof(Level.Render))]
  static void reorderBg(ILContext ctx){
    ILCursor c = new ILCursor(ctx);
    //DebugConsole.DumpIl(c,0,50); 
    if(!c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchLdsfld(typeof(GameplayBuffers),nameof(GameplayBuffers.Level)),
      itr => itr.MatchCall(out _),
      itr=>itr.MatchCallvirt<GraphicsDevice>("SetRenderTarget")
    ))goto bad;
    ILCursor d = c.Clone();
    if(!d.TryGotoNextBestFit(MoveType.Before,
      itr=>itr.MatchLdsfld(typeof(GameplayBuffers),nameof(GameplayBuffers.Gameplay))
    )) goto bad;
    Instruction target = d.Next;
    c.EmitDelegate(backdropReorderDetour);
    c.EmitBrtrue(target);
    return;
    bad:
    DebugConsole.WriteFailure($"Failed to add background reordering hook");
  }
  [OnLoad.OnHook(typeof(Player),"")]
  public static void playerCtorHook(On.Celeste.Player.orig_ctor orig, Player p, Vector2 pos, PlayerSpriteMode s){
    orig(p,pos,s);
    remLeaving();
  }
  [OnLoad.OnHook(typeof(Level),nameof(Level.End))]
  public static void SceneEnd(On.Celeste.Level.orig_End orig, Level l){
    foreach(var v in layers) removeLayer(v);
    orig(l);
  }
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnExit)]
  public static void Cleanup(){
    foreach(var l in layers) removeLayer(l);
    toRemove.Clear();
    layers.Clear();
  }
}
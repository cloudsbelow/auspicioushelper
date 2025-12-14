
using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper.Wrappers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

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
    heartHooks.enable();
    return new HeartGem(e,o);
  }

  public class FireIcePatch:Component, ISimpleWrapper{
    public Entity wrapped {get;set;}
    public Template parent {get;set;}
    public Vector2 toffset {get;set;}
    Solid solid=>new DynamicData(wrapped).Get<Solid>("solid");
    bool getOwnCol(Level l){
      if(wrapped.GetType()==typeof(IceBlock)) return l.coreMode==Session.CoreModes.Cold;
      if(wrapped.GetType()==typeof(FireBarrier)) return l.coreMode==Session.CoreModes.Hot;
      return true;
    }
    public FireIcePatch(Entity wrapped):base(false,false){
      this.wrapped=wrapped;
      var comp = wrapped.Get<CoreModeListener>();
      var orig = comp.OnChange;
      comp.OnChange = (Session.CoreModes mode)=>{
        if(vca.Visible)orig(mode);
        ownCol = getOwnCol(wrapped.Scene as Level);
        setCol(vca.Collidable && ownCol);
      };
      wrapped.Add(this);
    }
    bool ownCol;
    TemplateDisappearer.vcaTracker vca = new();
    void setCol(bool n){
      wrapped.Collidable = n;
      if(solid is {} s) s.Collidable=n;
    }
    
    void ITemplateChild.parentChangeStat(int vis, int col, int act){
      vca.Align(vis,col,act);
      if(vis!=0) wrapped.Visible = vis>0;
      if(col!=0) setCol(vca.Collidable && ownCol);
      if(act!=0) wrapped.Active = col>0;
    }
    public override void EntityAdded(Scene scene) {
      base.EntityAdded(scene);
      setCol(vca.Collidable && (ownCol = getOwnCol(scene as Level)));
    }
    void ITemplateChild.destroy(bool particles){
      solid?.RemoveSelf();
      wrapped.RemoveSelf();
      if(particles && vca.Visible && ownCol){
        Level level = Scene as Level;
        Vector2 center = wrapped.Center;
        ParticleType p = null;
        if(wrapped.GetType()==typeof(IceBlock)) p=IceBlock.P_Deactivate;
        if(wrapped.GetType()==typeof(FireBarrier)) p=FireBarrier.P_Deactivate;
        if(p!=null) for (int i = 0; (float)i < wrapped.Width; i += 4){
          for (int j = 0; (float)j < wrapped.Height; j += 4){
            Vector2 vector = wrapped.Position + new Vector2(i + 2, j + 2) + Calc.Random.Range(-Vector2.One * 2f, Vector2.One * 2f);
            level.Particles.Emit(p, vector, (vector - center).Angle());
          }
        }
      }
    }
    void ITemplateChild.relposTo(Vector2 loc, Vector2 liftspeed) {
      wrapped.Position = (loc+toffset).Round();
      if(solid is {} s) solid.MoveTo(wrapped.Position+new Vector2(2f, 3f), liftspeed);
    }
    void ITemplateChild.AddAllChildren(List<Entity> list){
      list.Add(wrapped);
      if(solid is {} s) list.Add(s);
    }
    void UghRender(){
      wrapped.Collidable = ownCol;
      wrapped.Render();
      wrapped.Collidable = ownCol && vca.Collidable;
    }
    static FireIcePatch(){
      OverrideVisualComponent.custom.AddMultiple([typeof(IceBlock),typeof(FireBarrier)],static (Entity e)=>{
        var c= e.Get<FireIcePatch>();
        return new OverrideVisualComponent.PatchedRenderComp(){
          render = c.UghRender
        };
      });
    }
  }

  public class AnchorLocMod:ISimpleWrapper{
    public Entity wrapped {get;set;}
    public Template parent {get;set;}
    public Vector2 toffset {get;set;}
    public static readonly string[] _RotateSp = [nameof(RotateSpinner.center)];
    public static readonly string[] _TrackSp = [nameof(TrackSpinner.Start),nameof(TrackSpinner.End)];
    Vector2[] toffsets;
    string[] fields;
    DynamicData dd;
    public AnchorLocMod(Entity w, string[] fields){
      wrapped = w;
      dd = new DynamicData(w);
      this.fields=fields;
    }
    Vector2 lpos;
    void ITemplateChild.setOffset(Vector2 ppos){
      toffsets = fields.Map(s=>dd.Get<Vector2>(s)-ppos);
      toffset = wrapped.Position-ppos;
      lpos = wrapped.Position;
    }
    void ITemplateChild.relposTo(Vector2 loc, Vector2 parentLiftspeed){
      for(int i=0; i<fields.Length; i++)dd.Set(fields[i],loc+toffsets[i]);
      toffset = toffset+(wrapped.Position-lpos);
      wrapped.Position = loc+toffset;
      lpos = wrapped.Position;
    }
  }
}
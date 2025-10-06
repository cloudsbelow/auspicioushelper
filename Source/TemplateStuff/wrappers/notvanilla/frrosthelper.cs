

using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Editor;
using Iced.Intel;
using Microsoft.Xna.Framework;
using Mono.Cecil.Mdb;
using Mono.CompilerServices.SymbolWriter;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public static class FrostHelperStuff{
  public class Staticbumperwrapper:ISimpleWrapper{
    public Template parent {get;set;}
    public Entity wrapped {get;}
    public Vector2 toffset {get;set;}
    Vector2 twoffset = Vector2.Zero;
    DynamicData d;
    public Staticbumperwrapper(Entity e, EntityData dat){
      Tween tw = e.Get<Tween>();
      d=new DynamicData(e);
      wrapped = e;
      if(tw == null) return;
      Vector2 delta = dat.Nodes[0]-dat.Position;
      tw.OnUpdate = (Tween t)=>{
        if(d.Get<bool>("goBack")){
          twoffset = Vector2.Lerp(delta,Vector2.Zero,t.Eased);
        } else {
          twoffset = Vector2.Lerp(Vector2.Zero,delta,t.Eased);
        }
        d.Set("anchor",parent.roundLoc+toffset+twoffset);
      };
    }
    public void relposTo(Vector2 loc, Vector2 liftspeed){
      d.Set("anchor",loc+toffset+twoffset);
    }
  }

  public class SpinnerWrapper:UpdateHook, ISimpleWrapper{
    public Entity wrapped {get;set;}
    public Vector2 toffset {get;set;}
    public Template parent {get;set;}
    static Type spinnerType = null;
    static Util.FieldHelper<int> AttachGroup;
    static Util.FieldHelper<float> TimingOffset;
    static Util.FieldHelper<bool> Expanded;
    static Util.FieldHelper<bool> RegisteredToRenderers;
    static Util.FieldHelper<List<Image>> Images;
    static Util.FieldHelper<int> ID;
    static Func<object, bool> InView;
    static Action<object> RegisterToRenders;
    static Action<object,Scene> UnregisterFromRenderers;
    static Action<object,bool> SetVisible;
    static Action<object> CreateSpritesOrig;
    static Action<object> UpdateHue;
    static Util.FieldHelper filler;
    static Util.ValueHelper fills;
    static Util.ValueHelper<MTexture> FillerTexture;
    static Util.ValueHelper<Vector2> FillerPosition;
    public SpinnerWrapper(Entity spinner, EntityData dat):base(){
      afterAction = CustomUpdate;
      wrapped = spinner;
      if(spinnerType == null && spinner != null){
        spinnerType = spinner.GetType();
        AttachGroup = new(spinnerType, "AttachGroup");
        TimingOffset = new(spinnerType,"offset");
        Expanded = new(spinnerType,"expanded");
        RegisteredToRenderers = new(spinnerType,"RegisteredToRenderers",true);
        Images = new(spinnerType,"_images",true);
        ID = new(spinnerType, "ID");
        InView = Util.instanceFunc<bool>(spinnerType, "InView");
        RegisterToRenders = Util.instanceAction(spinnerType,"RegisterToRenderers");
        UnregisterFromRenderers = Util.instanceAction<Scene>(spinnerType,"UnregisterFromRenderers");
        SetVisible = Util.instanceAction<bool>(spinnerType, "SetVisible");
        CreateSpritesOrig = Util.instanceAction(spinnerType,"CreateSprites");
        UpdateHue = Util.instanceAction(spinnerType,"UpdateHue");
        OverrideVisualComponent.custom.Add(spinnerType,static (Entity e)=>{
          var c= e.Get<SpinnerWrapper>();
          if(c == null) throw new Exception("Could not get controlling component from spinner");
          return c.renderComp = new OverrideVisualComponent.PatchedRenderComp(){
            render = c.RenderTheUgh, onChangeNvis = c.setMatVis, isVis = c.origVis
          };
        });
        filler = new(spinnerType, "filler", true);
        fills = new(filler.vtype, "Fills", true);
        Type fillType = fills.vtype.GetGenericArguments()[0];
        FillerTexture = new(fillType, "Texture");
        FillerPosition = new(fillType, "Position");
      }
      int og = AttachGroup.get(wrapped);
      if(og==-1) AttachGroup.set(wrapped,HashCode.Combine(EntityParser.currentParent,og));
      offset = TimingOffset.get(wrapped);
      rainbow = dat.Bool("rainbow", false);
      hascollider = dat.Bool("collidable", true);
      wrapped.Visible = false;
      ID.set(wrapped,-ID.get(wrapped));
      wrapped.Add(this);
      StaticMover sm = null;
      while((sm = wrapped.Get<StaticMover>())!=null)wrapped.Remove(sm);
    }
    OverrideVisualComponent.PatchedRenderComp renderComp;
    bool parentvis=true;
    bool ownvis=false;
    bool overrideVis=true;
    bool gtvis =>parentvis && ownvis && overrideVis;
    bool parentcol=true;
    bool owncol=false;
    float offset;
    bool rainbow;
    bool hascollider;
    bool origVis()=> parentvis && ownvis;
    void setMatVis(bool nvis)=>setVis(ref overrideVis, nvis);
    void setVis(ref bool field, bool nval){
      //DebugConsole.Write($"Set nvis old; {ownvis} {parentvis} {overrideVis}");
      field = nval;
      //DebugConsole.Write($"Set nvis new; {ownvis} {parentvis} {overrideVis}");
      bool o = wrapped.Visible;
      bool n = parentvis && ownvis && overrideVis;
      if(o != n){
        if(n){
          RegisterToRenders(wrapped);
          SetVisible(wrapped, true);
          if(rainbow) UpdateHue(wrapped);
        } else {
          UnregisterFromRenderers(wrapped,null);
          SetVisible(wrapped, false);
        }
      }
    }
    void setCol(ref bool field, bool nval){
      field = nval;
      wrapped.Collidable = parentcol && owncol;
    }
    bool stratosphere = false;
    const int stratosphereHeight = -30000;
    void Destratosphere(){
      if(stratosphere){
        wrapped.Position.Y-=stratosphereHeight;
        stratosphere = false;
      }
    }
    void CustomUpdate(){
      if(RegisteredToRenderers.get(wrapped) && !wrapped.Visible) UnregisterFromRenderers(wrapped,Scene);
      if(wrapped.Visible!=gtvis) setVis(ref ownvis, ownvis);
      if(!ownvis){
        setCol(ref owncol, false);
        if(InView(wrapped)){
          if(!Expanded.get(wrapped)) expandGood();
          setVis(ref ownvis, true);
        }
      } else {
        if(rainbow && Scene.OnInterval(0.08f)) UpdateHue(wrapped);
        if(Scene.OnInterval(0.25f, offset) && !InView(wrapped)){
          setVis(ref ownvis, false);
        }
        if (hascollider && (cachedPlayer ??= Scene.Tracker.GetEntity<Player>()) is { } player){
          setCol(ref owncol,Math.Abs(player.X - wrapped.X) < 128f && Math.Abs(player.Y - wrapped.Y) < 128f);
        } 
      }
    }
    public void parentChangeStat(int vis, int col, int act){
      if(vis!=0) setVis(ref parentvis, vis>0);
      if(col!=0) setCol(ref parentcol, col>0);
    }
    public void setOffset(Vector2 ppos){
      toffset=wrapped.Position-Vector2.UnitY*(stratosphere?stratosphereHeight:0)-ppos;
    }
    void expandGood(){
      List<Entity> oldSts = Scene.Tracker.Entities[typeof(SolidTiles)];
      Scene.Tracker.Entities[typeof(SolidTiles)] = parent.fgt==null?[]:[parent.fgt];
      bool flag=false;
      if(parent.fgt?.Collidable==false)parent.fgt.Collidable = flag = true;
      CreateSpritesOrig(wrapped);
      if(flag) parent.fgt.Collidable=false;
      Scene.Tracker.Entities[typeof(SolidTiles)] = oldSts;
    }
    public void addTo(Scene s){
      s.Add(wrapped);
      Scene o = wrapped.Scene;
      wrapped.Scene = s;
      if(InView(wrapped)){
        wrapped.Position.Y+=stratosphereHeight;
        stratosphere=true;
        UpdateHook.AddAfterUpdate(Destratosphere,false,true);
      }
      wrapped.Scene = o;
      wrapped.Active = false;
    }
    public void RenderTheUgh(){
      if(!MaterialPipe.clipBounds.CollidePointExpand(Int2.Round(wrapped.Position),10)) return;
      foreach(Image i in Images.get(wrapped)){
        i.Render();
      }
      object fill = filler.get(wrapped);
      Vector2 pos = wrapped.Position;
      if(fill!=null && fills.get(fill) is IEnumerable e)foreach(object m in e){
        //DebugConsole.Write($"{pos} {FillerPosition.get(m)}");
        FillerTexture.get(m).DrawCentered(pos+FillerPosition.get(m));
      }
    }
  }

  public static void setup(){
    EntityParser.clarify("FrostHelper/StaticBumper", EntityParser.Types.unwrapped, (Level l, LevelData d, Vector2 o, EntityData e)=>{
      if(Level.EntityLoaders.TryGetValue("FrostHelper/StaticBumper",out var orig)) 
        EntityParser.currentParent.addEnt(new Staticbumperwrapper(orig(l,d,o,e),e));
      return null;
    });
    EntityParser.clarify(["FrostHelper/IceSpinner","FrostHelperExt/CustomBloomSpinner"], EntityParser.Types.unwrapped, (l,d,o,e)=>{
      if(Level.EntityLoaders.TryGetValue("FrostHelper/IceSpinner",out var orig)) 
        EntityParser.currentParent.addEnt(new SpinnerWrapper(orig(l,d,o,e),e));
      return null;
    });
  }
}
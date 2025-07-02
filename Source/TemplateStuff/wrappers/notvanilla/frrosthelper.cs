

using System;
using System.Collections.Generic;
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
        d.Set("anchor",parent.virtLoc+toffset+twoffset);
      };
    }
    public void relposTo(Vector2 loc, Vector2 liftspeed){
      d.Set("anchor",loc+toffset+twoffset);
    }
  }

  [Tracked]
  public class SpinnerWrapper:UpdateHook, ISimpleWrapper{
    public Entity wrapped {get;set;}
    public Vector2 toffset {get;set;}
    public Template parent {get;set;}
    static Type spinnerType = null;
    static Util.FieldHelper<int> AttachGroup;
    static Util.FieldHelper<float> TimingOffset;
    static Func<object, bool> InView;
    static Action<object> RegisterToRenders;
    static Action<object> UnregisterFromRenderers;
    static Action<object,bool> SetVisible;
    static Action<object> CreateSpritesOrig;
    static Action<object> UpdateHue;
    public SpinnerWrapper(Entity spinner, EntityData dat):base(){
      beforeAction = CustomUpdate;
      wrapped = spinner;
      if(spinnerType == null && spinner != null){
        spinnerType = spinner.GetType();
        AttachGroup = new Util.FieldHelper<int>(spinnerType, "AttachGroup");
        TimingOffset = new Util.FieldHelper<float>(spinnerType,"offset");
        InView = Util.instanceFunc<bool>(spinnerType, "InView");
        RegisterToRenders = Util.instanceAction(spinnerType,"RegisterToRenderers");
        UnregisterFromRenderers = Util.instanceAction(spinnerType,"UnregisterFromRenderers");
        SetVisible = Util.instanceAction<bool>(spinnerType, "SetVisible");
        CreateSpritesOrig = Util.instanceAction(spinnerType,"CreateSprites");
        UpdateHue = Util.instanceAction(spinnerType,"UpdateHue");
      }
      int og = AttachGroup.get(wrapped);
      if(og==-1) AttachGroup.set(wrapped,HashCode.Combine(EntityParser.currentParent,og));
      offset = TimingOffset.get(wrapped);
      DebugConsole.Write($"here: {offset} {og}");
      rainbow = dat.Bool("rainbow", false);
      hascollider = dat.Bool("collidable", true);
    }
    bool parentvis;
    bool ownvis;
    bool parentcol;
    bool owncol;
    float offset;
    bool rainbow;
    bool hascollider;
    void setVis(ref bool field, bool nval){
      field = nval;
      bool o = wrapped.Visible;
      bool n = parentvis && ownvis;
      if(o != n){
        if(n){
          RegisterToRenders(wrapped);
          SetVisible(wrapped, true);
          if(rainbow) UpdateHue(wrapped);
        } else {
          UnregisterFromRenderers(wrapped);
          SetVisible(wrapped, false);
        }
      }
    }
    void setCol(ref bool field, bool nval){
      field = nval;
      wrapped.Collidable = parentcol && owncol;
    }
    void CustomUpdate(){
      if(!ownvis){
        setCol(ref owncol, false);
        if(InView(wrapped)){
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
    void expandGood(){
      List<Entity> oldSts = Scene.Tracker.Entities[typeof(SolidTiles)];
      Scene.Tracker.Entities[typeof(SolidTiles)] = parent.fgt==null?[]:[parent.fgt];
      CreateSpritesOrig(wrapped);
      Scene.Tracker.Entities[typeof(SolidTiles)] = oldSts;
    }
    public void addTo(Scene s){
      s.Add(wrapped);
      wrapped.Scene = s;
      if(InView(wrapped))expandGood();
      wrapped.Active = false;
    }
  }

  public static void setup(){
    EntityParser.clarify("FrostHelper/StaticBumper", EntityParser.Types.unwrapped, (Level l, LevelData d, Vector2 o, EntityData e)=>{
      if(Level.EntityLoaders.TryGetValue("FrostHelper/StaticBumper",out var orig)) 
        EntityParser.currentParent.addEnt(new Staticbumperwrapper(orig(l,d,o,e),e));
      return null;
    });
    EntityParser.clarify(["FrostHelper/IceSpinner","FrostHelperExt/CustomBloomSpinner"], EntityParser.Types.unwrapped, (l,d,o,e)=>{
      DebugConsole.Write("frosthelper setup");
      if(Level.EntityLoaders.TryGetValue("FrostHelper/IceSpinner",out var orig)) 
        EntityParser.currentParent.addEnt(new SpinnerWrapper(orig(l,d,o,e),e));
      return null;
    });
    DebugConsole.Write("frosthelper setup");
  }
}
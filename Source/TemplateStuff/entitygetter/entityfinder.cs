


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

public static class Finder{
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnReload)]
  static List<Action<Entity>> finding = null;
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnReload)]
  static Entity last = null;
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  public static Dictionary<string, List<Action<Entity>>> flagged = new();
  public static void watch(string path, Action<Entity> thing){
    hooks.enable();
    foreach(var sig in path.Split(',')) try{
      if(string.IsNullOrWhiteSpace(sig)) continue;
      string cl = Regex.Replace(sig,@"\s+","");
      if(!flagged.TryGetValue(cl, out var li)){
        flagged.Add(cl,li = new());
      }
      li.Add(thing);
    } catch(Exception ex){
      DebugConsole.WriteFailure($"Your ID path could not be parsed: {sig} causes error \n{ex}\n"+
      "Please remember to format your path to match: \\d+(/\\d+)*");
    }
  }
  public static void StartingLoad(EntityData d){
    last = null; finding = null;
    if(flagged.TryGetValue(d.ID.ToString(), out var ident)){
      finding=ident;
      DebugConsole.Write($"Looking for entity on path {d.ID}. Has {ident.Count} actions enqueued");
      //finding = new FoundEntity(d, ident);
    }
  }
  public static void EndingLoad(EntityData d){
    if(finding!=null){
      if(last==null){
        DebugConsole.Write($"Failed to find the entity {d.Name} with id {d.ID} - (maybe this entity adds itself non-standardly?)");
      } else {
        DebugConsole.Write($"Found the entity {d.Name} with id {d.ID} - position {last.Position}");
        foreach(var a in finding) a(last);
      }
    } 
    finding = null;
    last = null;
  }
  public static void LLILHook(ILContext ctx){
    var c = new ILCursor(ctx);
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchCall<Level>("LoadCustomEntity"))){
      goto bad;
    }
    c.EmitLdloc(17);
    c.EmitDelegate(StartingLoad);
    Type et = typeof(List<EntityData>.Enumerator);
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchLdloca(16),instr=>instr.MatchCall(et,"MoveNext"))){
      goto bad;
    }
    c.Index++;
    c.EmitLdloc(17);
    c.EmitDelegate(EndingLoad);
    return;
    bad:
      DebugConsole.WriteFailure("Failed to add hook to entity finder");
  }
  static ILHook llhook;
  public static void AddHook(On.Monocle.Scene.orig_Add_Entity orig, Scene self, Entity e){
    if(self is Level l){
      //if(e!=null)DebugConsole.Write("Add "+ e?.ToString());
      if(finding!=null)last = e??last;
    }
    orig(self, e);
  } 
  [OnLoad]
  public static HookManager hooks = new HookManager(()=>{
    llhook = new ILHook(typeof(Level).GetMethod("orig_LoadLevel",Util.GoodBindingFlags), LLILHook);
    On.Monocle.Scene.Add_Entity+=AddHook;
  },void ()=>{
    llhook?.Dispose();
    On.Monocle.Scene.Add_Entity-=AddHook;
  });




  [CustomEntity("auspicioushelper/FinderDepth")]
  [MapenterEv(nameof(Search))]
  [CustomloadEntity]
  public class MarkingFlag:Entity{
    static void Search(EntityData d){
      Finder.watch(d.Attr("path"),(e)=>e.Depth = d.Int("depth",e.Depth));
    }
  }

  [CustomEntity("auspicioushelper/FinderCollider")]
  [MapenterEv(nameof(Search))]
  [CustomloadEntity]
  public class ColliderModifier:Entity{
    class CMod:ChannelTracker{
      Collider orig;
      Collider replace;
      bool restorable;
      Entity e;
      public CMod(EntityData d, Entity e, string c):base(c){
        orig = e.Collider;
        replace = buildCollider(d);
        this.e=e;
        SetOnchange(OnChange,true);
        restorable = d.Bool("restorable",true);
      }
      void OnChange(int nval){
        if(nval!=0) e.Collider = replace;
        else if(restorable && e.Collider.Equals(replace)) e.Collider = orig; 
      }
    }
    static void Search(EntityData d){
      Finder.watch(d.Attr("path"),(e)=>{
        if(d.tryGetStr("channel", out var str)){
          e.Add(new CMod(d,e,str));
        } else e.Collider = buildCollider(d);
      });
    }

    static Regex pattern = new Regex(@"(\w+):(.+)",RegexOptions.Compiled);
    static Collider buildCollider(EntityData d){
      List<string> things = Util.listparseflat(d.String("collider","rect:[-8,-8,16,16]"));
      var c = things.Map(Collider (s)=>{
        var M = pattern.Match(s);
        if(!M.Success) DebugConsole.WriteFailure("Failed to parse collider "+s);
        var u = Util.stripEnclosure(M.Groups[2].Value);
        switch(M.Groups[1].Value.ToLower()){
          case "circle": case "c":
            float[] vals = Util.csparseflat(u,8,0,0);
            return new Circle(vals[0],vals[1],vals[2]);
          case "hitbox": case "hb": case "h":
            vals = Util.csparseflat(u,8,8,0,0);
            return new Hitbox(vals[0],vals[1],vals[2],vals[3]);
          case "rectangle": case "rect": case "r":
            vals = Util.csparseflat(u,0,0,8,8);
            return new Hitbox(vals[2],vals[3],vals[0],vals[1]);
        }
        throw new Exception("Failed to parse collider "+s);
      });
      if(c.Count==1) return c[0];
      return new ColliderList(c.ToArray());
    }
  }
}
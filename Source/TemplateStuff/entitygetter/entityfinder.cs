


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

public static partial class Finder{
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnReload)]
  static List<Action<Entity>> finding = null;
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnReload)]
  static Entity last = null;
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  public static Dictionary<string, List<Action<Entity>>> flagged = new();

  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  [Import.SpeedrunToolIop.Static]
  static Dictionary<string, List<TypeHandlerCallback>> typeHandler = new();
  public class TypeHandlerCallback(Action<Entity> action):OnAnyRemoveComp(false,false){
    List<string> registered = new();
    public void EntityAdded(Entity e)=>action(e);
    // If a scene for retroactive is passed in, it MUST be during the AWAKE section of updateLists
    public TypeHandlerCallback RegisterTo(string s, Scene retroactive = null){
      if(registered.Contains(s)) return this;
      if(!typeHandler.TryGetValue(s, out var li)) typeHandler.Add(s, li=new());
      li.Add(this);
      registered.Add(s);
      if(retroactive is Level level){
        EntityList l = level.Entities;
        foreach(Entity e in l.entities.Concat(l.toAdd)){
          var name = e.SourceData is {} sd? sd.Name : e switch {
            SolidTiles=>"fg", BackgroundTiles=>"bg", Player=>"player", _=> null
          };
          if(name==s) action(e);
        }
      }
      return this;
    }
    public void Retroactive(Level retroactive){
      EntityList l = retroactive.Entities;
      HashSet<string> grr = [..registered];
      foreach(Entity e in l.entities.Concat(l.toAdd)){
        var name = e.SourceData is {} sd? sd.Name : e switch {
          SolidTiles=>"fg", BackgroundTiles=>"bg", Player=>"player", _=> null
        };
        if(grr.Contains(name)) action(e);
      }
    }
    public override void OnRemove() {
      foreach(string s in registered){
        if(typeHandler.TryGetValue(s, out var li)){
          li.Remove(this);
          if(li.Count == 0) typeHandler.Remove(s);
        }
      }
      registered.Clear();
    }
  }

  public static void watch(string path, Action<Entity> thing){
    foreach(var sig in path.Split(',')) try{
      if(string.IsNullOrWhiteSpace(sig)) continue;
      //DebugConsole.Write($"watching \"{sig}\"");
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
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  static HashSet<(string,string)> waiting=new();
  public static void enqueueIdent(string path, string ident=null){
    ident??=path;
    if(string.IsNullOrWhiteSpace(ident)) return;
    if(waiting.Contains((path,ident))) return;
    waiting.Add((path,ident));
    watch(path,e=>FoundEntity.addIdent(e,path));
  }
  public static void StartLoad(EntityData d, string prefix = ""){
    last = null; finding = null;
    if(flagged.TryGetValue(prefix+d.ID.ToString(), out var ident)){
      finding = ident;
    }
  }
  static void StartingLoad(EntityData d){
    last = null; finding = null;
    if(flagged.TryGetValue(d.ID.ToString(), out var ident)){
      finding = ident;
    }
  }
  static void StartingLoadTrigger(EntityData d){
    last = null; finding = null;
    string matchstr;
    if(
      flagged.TryGetValue(matchstr="t"+d.ID.ToString(), out var ident) ||
      flagged.TryGetValue(matchstr="trigger"+d.ID.ToString(), out ident)
    )finding = ident;
  }
  public static Level addingLevel;
  public static void EndingLoad(EntityData d, Level l){
    if(finding!=null){
      if(last==null){
        DebugConsole.WriteFailure($"Failed to find the entity {d.Name} with id {d.ID} - (maybe this entity adds itself non-standardly?)");
        if(auspicioushelperModule.InFolderMod) DebugConsole.MakePostcard($"Failed to find the entity {d.Name} with id {d.ID}. This entity may not be compatible or there may be mod conflicts.");
      } else {
        addingLevel = l;
        foreach(var a in finding) a(last);
        addingLevel = null;
      }
    } 
    finding = null;
    last = null;
  }

  [OnLoad.OnHook(typeof(EntityList),nameof(EntityList.Add),Util.HookTarget.Normal,[typeof(Entity)])]
  public static void AddHook(On.Monocle.EntityList.orig_Add_Entity orig, EntityList self, Entity e){
    if(self.Scene is Level l){
      if(finding!=null)last = e??last;
      if(e is Player p && flagged.TryGetValue("player",out var waaa)){
        DebugConsole.Write($"Found new player; {waaa.Count} actions");
        foreach(var a in waaa) a(p); 
      } 
      var name = e.SourceData is {} sd? sd.Name : e switch {
        SolidTiles=>"fg", BackgroundTiles=>"bg", Player=>"player", _=> null
      };
      if(name!=null && typeHandler.TryGetValue(name, out var handlers)){
        foreach(var h in handlers) h.EntityAdded(e);
      }
    }
    orig(self, e);
  } 

  [OnLoad.ILHook(typeof(Level),nameof(Level.orig_LoadLevel))]
  public static void LLILHook(ILContext ctx){
    var c = new ILCursor(ctx);
    Type et = typeof(List<EntityData>.Enumerator);
    //entities
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchCall<Level>("LoadCustomEntity"))) goto bad;
    c.EmitLdloc(17);
    c.EmitDelegate(StartingLoad);
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchLdloca(16),instr=>instr.MatchCall(et,"MoveNext"))) goto bad;
    c.Index++;
    c.EmitLdloc(17);
    c.EmitLdarg0();
    c.EmitDelegate(EndingLoad);

    //triggers
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchCall<Level>("LoadCustomEntity"))) goto bad;
    c.EmitLdloc(46);
    c.EmitDelegate(StartingLoadTrigger);
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchLdloca(16),instr=>instr.MatchCall(et,"MoveNext"))) goto bad;
    c.Index++;
    c.EmitLdloc(46);
    c.EmitLdarg0();
    c.EmitDelegate(EndingLoad);
    return;
    bad:
      DebugConsole.WriteFailure("Failed to add hook to entity finder",true);
  }
}



using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
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
      DebugConsole.Write($"watching \"{sig}\"");
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
  static void StartingLoad(EntityData d){
    last = null; finding = null;
    if(flagged.TryGetValue(d.ID.ToString(), out var ident)) finding = ident;
  }
  static void StartingLoadTrigger(EntityData d){
    last = null; finding = null;
    string matchstr;
    if(
      flagged.TryGetValue(matchstr="t"+d.ID.ToString(), out var ident) ||
      flagged.TryGetValue(matchstr="trigger"+d.ID.ToString(), out ident)
    )finding = ident;
  }
  public static void EndingLoad(EntityData d){
    if(finding!=null){
      if(last==null){
        DebugConsole.WriteFailure($"Failed to find the entity {d.Name} with id {d.ID} - (maybe this entity adds itself non-standardly?)");
        if(auspicioushelperModule.InFolderMod) DebugConsole.MakePostcard($"Failed to find the entity {d.Name} with id {d.ID}. This entity may not be compatible or there may be mod conflicts.");
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
    Type et = typeof(List<EntityData>.Enumerator);
    //entities
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchCall<Level>("LoadCustomEntity"))) goto bad;
    c.EmitLdloc(17);
    c.EmitDelegate(StartingLoad);
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchLdloca(16),instr=>instr.MatchCall(et,"MoveNext"))) goto bad;
    c.Index++;
    c.EmitLdloc(17);
    c.EmitDelegate(EndingLoad);

    //triggers
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchCall<Level>("LoadCustomEntity"))) goto bad;
    c.EmitLdloc(46);
    c.EmitDelegate(StartingLoadTrigger);
    if(!c.TryGotoNext(MoveType.Before, instr=>instr.MatchLdloca(16),instr=>instr.MatchCall(et,"MoveNext"))) goto bad;
    c.Index++;
    c.EmitLdloc(46);
    c.EmitDelegate(EndingLoad);
    return;
    bad:
      DebugConsole.WriteFailure("Failed to add hook to entity finder",true);
  }
  static ILHook llhook;
  public static void AddHook(On.Monocle.EntityList.orig_Add_Entity orig, EntityList self, Entity e){
    if(self.Scene is Level l){
      //if(e!=null)DebugConsole.Write("Add "+ e?.ToString());
      if(finding!=null)last = e??last;
      if(e is Player p && flagged.TryGetValue("player",out var waaa)){
        DebugConsole.Write($"Found new player; {waaa.Count} actions");
        foreach(var a in waaa) a(p); 
      } 
    }
    orig(self, e);
  } 
  [OnLoad]
  public static HookManager hooks = new HookManager(()=>{
    llhook = new ILHook(typeof(Level).GetMethod("orig_LoadLevel",Util.GoodBindingFlags), LLILHook);
    On.Monocle.EntityList.Add_Entity+=AddHook;
  },void ()=>{
    llhook?.Dispose();
    On.Monocle.EntityList.Add_Entity-=AddHook;
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
      void OnChange(double nval){
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

  [CustomEntity("auspicioushelper/CollisionCounter")]
  [MapenterEv(nameof(Search))]
  public class CollisionCounter:Entity{
    class GroupMarker:OnAnyRemoveComp{
      (int, bool) loc;
      public GroupMarker((int,bool) loc):base(false,false){
        this.loc=loc;
      }
      public override void Added(Entity entity) {
        base.Added(entity);
        if(!groups.TryGetValue(loc, out var ss)) groups.Add(loc,ss=new());
        ss.Add(this);
      }
      public override void OnRemove(){
        if(groups.TryGetValue(loc, out var ss))ss.Remove(this);
      }
    }
    [Import.SpeedrunToolIop.Static]
    [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
    static Dictionary<(int,bool),List<GroupMarker>> groups = new();
    static void Search(EntityData d){
      int num = d.ID;
      watch(d.Attr("groupA",""),(e)=>e.Add(new GroupMarker((num,false))));
      watch(d.Attr("groupB",""),(e)=>e.Add(new GroupMarker((num,true))));
    }
    int num;
    bool aCollidable=true;
    bool bCollidable=true;
    string channel="";
    int tCount;
    public CollisionCounter(EntityData d, Vector2 o):base(d.Position+o){
      num = d.ID;
      tCount = d.Attr("groupA","").Split(",").Length+d.Attr("groupB","").Split(",").Length;
      aCollidable=d.Bool("onlyCollidableA",true);
      bCollidable=d.Bool("onlyCollidableB",true);
      channel = d.Attr("channel","numCollisions");
    }
    public override void Update() {
      base.Update();
      var l1 = groups.GetValueOrDefault((num,false))?.FilterMap((GroupMarker c,out Entity e)=>(e=c.Entity).Collidable||!aCollidable);
      var l2 = groups.GetValueOrDefault((num,true))?.FilterMap((GroupMarker c,out Entity e)=>(e=c.Entity).Collidable||!bCollidable);
      if(l1 == null || l2 == null) return;
      if(l1.Count+l2.Count>tCount) DebugConsole.MakePostcard("Mysterious! please contact cloudsbelow that you've recieved this!");
      int count = 0;
      foreach(var e in l1) using(Util.WithRestore(ref e.Collidable,true)) foreach(var f in l2) if(f.CollideCheck(e)) count++;
      ChannelState.SetChannel(channel,count);
    }
  }
}
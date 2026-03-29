


using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.Linq;
using System.Reflection;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Celeste.Mod.Registry;
using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ConnectedBlocks", "auspicioushelper/ConnectedBlocksBg", "auspicioushelper/ConnectedContainer")]
[Tracked]
public class ConnectedBlocks:Entity{
  bool used;
  char tid;
  Vector2 levelOffset;
  Util.Trie<bool> permittedEnts = null;
  Util.Trie<bool> permittedDecals = null;
  bool allEnts;
  bool allDecals;
  bool excludeSolids;
  bool excludeTriggers;
  bool permits(Entity e){
    if(e is ConnectedBlocks or TemplateHoldable || e is Template t && !t.hasDeclaredTemplate) return false;
    if(e is Decal d){
      return d.Get<DecalMarker>() is DecalMarker dm && allDecals!=permittedDecals?.GetOrDefault(dm.texstr);
    } else if((excludeSolids && e is Solid) || (excludeTriggers && e is Trigger)){
      return e.SourceData?.Name is {} sn && (permittedEnts?.GetOrDefault(sn)??false);
    } else return e.SourceData?.Name is {} s && (permittedEnts?.GetOrDefault(s)??false)!=allEnts;
  }
  public bool permits(string s, bool decal, bool solid, bool trigger){
    if(decal)return allDecals!=permittedDecals?.GetOrDefault(s);
    if((excludeSolids && solid) || (excludeTriggers && trigger)){
      return permittedEnts?.GetOrDefault(s)??false;
    } else return (permittedEnts?.GetOrDefault(s)??false)!=allEnts;
  }
  public enum Category {
    fgt, bgt, ent
  }
  Category c;
  Vector2 leveloffset;
  public ConnectedBlocks(EntityData d, Vector2 offset):base(offset+d.Position){
    Collider = new Hitbox(d.Width,d.Height);
    tid = d.Attr("tiletype","0").FirstOrDefault();
    if(d.Name.EndsWith("Bg"))c = Category.bgt;
    else if(d.Name.EndsWith("er")) c=Category.ent;
    else c=Category.fgt;
    if(c == Category.ent){
      var v = Util.listparseflat(d.Attr("filterEntities", ""));
      if(v.Count>0) (permittedEnts = new()).SetAll(v,true);
      v = Util.listparseflat(d.Attr("filterDecals",""));
      if(v.Count>0) (permittedDecals = new()).SetAll(v,true);
      allEnts = d.Bool("getEntities",true);
      allDecals = d.Bool("getDecals",false);
      excludeSolids = d.Bool("excludeSolids",false);
      excludeTriggers = d.Bool("excludeTriggers",false);
    }
    levelOffset = offset;
    Depth = -10000000; //low depth type entity
    leveloffset=offset;
  }
  public static void FillRect(VirtualMap<char> m, Int2 tlc, Int2 brc, char v){
    for(int i=tlc.x; i<brc.x; i++) for(int j=tlc.y; j<brc.y; j++) m.orig_set_Item(i,j,v);
  }
  public const int padding = 1;
  static void processScene(Scene scene, Vector2 levelOffset){
    MaddiesIop.hooks.enable();
    List<(IntRect,ConnectedBlocks)> allThings = new();
    foreach(ConnectedBlocks c in scene.Tracker.GetEntities<ConnectedBlocks>()) if(!c.used){
      allThings.Add(new(new(c),c));
      c.used=true;
    }

    List<(Vector2, TemplateDisplacer.DisplacerData,int)> displacers = new();
    List<(templateFiller,MiptileCollider,int)> cbs = new();
    List<Action> onCompletion = new();
    HashSet<Entity> toRemove = new();
    while(allThings.Count>0){
      List<(IntRect,ConnectedBlocks)> things = new();
      int idx=0;
      things.Add(allThings[^1]);
      allThings.RemoveAt(allThings.Count-1);
      while(idx<things.Count){
        int nidx=things.Count;
        allThings.RemoveAll(x=>{
          for(int i=idx; i<nidx; i++) if(things[i].Item1.CollideIr(x.Item1)){
            things.Add(x);
            return true;
          }
          return false;
        });
        idx=nidx;
      } //

      Int2 min = things.ReduceMapI(a=>a.Item1.tlc,Int2.Min);
      Int2 max = things.ReduceMapI(a=>a.Item1.brc,Int2.Max);
      Int2 size = (max-min)/8+2*padding;
      things.Sort((x,y)=>y.Item2.actualDepth.CompareTo(x.Item2.actualDepth));
      VirtualMap<char> fgd = new(size.x,size.y,'0');
      VirtualMap<char> bgd = new(size.x,size.y,'0');
      QuickCollider<ConnectedBlocks> qcl = new();
      var layer = MipGrid.Layer.fromAreasize(size.x,size.y);
      int displacersUsed=1;
      foreach(var a in things){
        Int2 dloc = (a.Item1.tlc-min)/8;
        Int2 hloc = (a.Item1.brc-min)/8;
        layer.SetRect(true,dloc,hloc);
        switch(a.Item2.c){
          case Category.fgt: FillRect(fgd, dloc+padding, hloc+padding, a.Item2.tid);break;
          case Category.bgt: FillRect(bgd, dloc+padding, hloc+padding, a.Item2.tid);break;
          case Category.ent: qcl.Add(a.Item2,a.Item1); break;
        } 
      }

      SolidTiles s=null;
      BackgroundTiles b=null;
      templateFiller f = new(Int2.Zero+Int2.One*8*padding,max-min);
      f.setRoomdat((scene as Level).Session.LevelData);
      f.tiledata.setTiles(fgd);
      f.tiledata.setTiles(bgd,false);
      if(f.tiledata.fgt!=null)using(new PaddingLock()) s=new(Vector2.Zero, fgd);
      if(f.tiledata.bgt!=null)using(new PaddingLock()) b=new(Vector2.Zero, bgd);
      f.tiledata.initStatic(s,b);

      Util.OrderedSet<Entity> all = new();
      foreach(Entity e in scene){
        foreach(var t in qcl.Test(new(e))) if(t.permits(e)){
          addAllSms(e,all);
          break;
        } 
      }
      if(s!=null){
        s.Position+=min-Int2.One*8*padding;
        foreach (StaticMover smover in scene.Tracker.GetComponents<StaticMover>()){
          if (smover.Platform == null && smover.IsRiding(s))addAllSms(smover.Entity,all);
        }
      }
      RemChildren(all,min,levelOffset,f,toRemove);

      EntityData hit = null;
      MiptileCollider checker = new(layer, Vector2.One*8, min, true);
      List<KeyValuePair<Vector2, EntityData>> holds = new();
      foreach(var (k,v) in TemplateBehaviorChain.mainRoom){
        if(checker.collideFr(FloatRect.fromRadius(k+levelOffset,Vector2.One))){
          if(hit == null) hit = v;
          else if(auspicioushelperModule.InFolderMod){
            string erroring = "Connected tiles covered with more than one template entity. Cannot decide which to use! " +
              "For multiple behaviors, use chains.";
            foreach(var pair in holds) erroring+=$"{{n}}({pair.Key.X}, {pair.Key.Y}): {pair.Value.Name.RemovePrefix("auspicioushelper/")}";
            DebugConsole.MakePostcard(erroring);
          }
        }
      }
      if(hit==null) foreach(TemplateHoldable hold in scene.Tracker.GetEntities<TemplateHoldable>()){
        if(hold.isCreated || !string.IsNullOrWhiteSpace(hold.d.Attr("template",""))) continue;
        if(checker.collideFr(new(hold))){
          f.data.offset = min-(hold.Position+hold.Offset);
          onCompletion.Add(()=>hold.makeExternally(f));
          cbs.Add(new(f,checker,-1));
          goto endSingle;
        }
      }
      hit??=new EntityData(){Name=EntityParser.TemplateEmptyName,Position=min-levelOffset,Values=new()};
      f.data.offset = (min-levelOffset)-hit.Position; //i know order of ops, this just is comfortable ok?
      bool force = hit.Name=="auspicioushelper/TemplateBehaviorChain"&&hit.Bool("forceOwnPosition",false);
      Vector2? forcepos = force? hit.Position:null;
      var chain = new TemplateBehaviorChain.Chain(f, hit, forcepos, TemplateBehaviorChain.mainRoom);
      var first = chain.NextEnt(); 
      
      if(first.Name=="auspicioushelper/TemplateDisplacer"){
        templateFiller w = chain.NextFiller();
        foreach(var n in first.Nodes??[]){
          displacers.Add(new(n,new(){disp=w, Position=n},displacersUsed));
        }
        cbs.Add(new(f,checker,displacersUsed));
        displacersUsed++;
      } else {
        onCompletion.Add(()=>{
          if(Level.EntityLoaders.TryGetValue(first.Name, out var loader)){
            Level lv = scene as Level;
            Entity e = loader(lv,lv.Session.LevelData,levelOffset,first);
            if(e is Template te){
              te.t = chain.NextFiller();
              lv.Add(e);
            }else throw new Exception($"your chained entity is not a template? how did u do this? {e}");
          }
        });
        cbs.Add(new(f,checker,-1));
      }
      endSingle:;
    }
    foreach(var e in toRemove){
      if(ExtraRemovalSteps.TryGetValue(e.GetType(),out var er))er(e);
      if(e.SourceData?.Name is {} strname && ExtraRemovalSteps.TryGetValue(strname, out var er2))er2(e);
      e.RemoveSelf();
    }
    foreach(var (l,d,didx) in displacers){
      bool flag=false;
      foreach(var (f,checker,cidx) in cbs) if(didx!=cidx) {
        if(checker.collideFr(FloatRect.fromRadius(l+levelOffset,Vector2.One))){
          f.data.ChildEntities.Add(d);
          flag = true;
          DebugConsole.Write("ADded thing",l,f,d);
        }
      }
      if(!flag && TemplateDisplacer.ConstructAt(d,levelOffset) is {} temp) scene.Add(temp);
    }
    using(new Template.ChainLock()) foreach(var a in onCompletion) a();
    UpdateHook.EnsureUpdateAny();
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(!used) processScene(scene,leveloffset);
    RemoveSelf();
  }
  public static Dictionary<object, Action<Entity>> ExtraRemovalSteps = new(){
    {typeof(FireBarrier),(Entity e)=>(e as FireBarrier).solid.RemoveSelf()},
    {typeof(IceBlock),(Entity e)=>(e as IceBlock).solid.RemoveSelf()},
    {typeof(IntroCar),(Entity e)=>(e as IntroCar).wheels.RemoveSelf()},
    {"FrostHelper/CustomFireBarrier",(Entity e)=>((Entity)Util.ReflectGet(e,"solid",false))?.RemoveSelf()}
  };
  static void RemChildren(Util.OrderedSet<Entity> all, Vector2 minimum, Vector2 leveloffset, templateFiller f, HashSet<Entity> remove){
    HashSet<Entity> donot = new();
    foreach(var e in all) if(e is Template t) foreach(Entity en in t.GetChildren<Entity>()){
      if(en!=t || t.parent!=null)donot.Add(en);
    }
    foreach(var e in all){
      remove.Add(e);
      if(donot.Contains(e)) continue;
      
      if(e is Decal d){
        Vector2 fpos = e.Position-minimum+padding*8*Vector2.One;
        f.data.decals.Add(d.Get<DecalMarker>().withDepthAndForcepos(fpos));
      } else if(e.SourceData is EntityData dat){
        Vector2 fpos = dat.Position+leveloffset - minimum+padding*8*Vector2.One;
        f.data.ChildEntities.Add(Util.cloneWithForceposOffset(dat,fpos));
      }
    }
  }
  public static readonly List<string> allNames = new(){
    "auspicioushelper/ConnectedBlocks", 
    "auspicioushelper/ConnectedBlocksBg", 
    "auspicioushelper/ConnectedContainer"
  };



  public interface IShouldntInduct{}
  public static void addAllSms(Entity e, Util.OrderedSet<Entity> all){
    if(all.Contains(e) || e is IShouldntInduct) return;
    List<StaticMover> sms = null;
    if(e is Platform p) sms = p.staticMovers;
    if(MaddiesIop.at != null && e.GetType() == MaddiesIop.at){
      Solid intSolid = MaddiesIop.playerInteractingSolid.get(e);
      sms = new();
      bool old = intSolid.Collidable;
      intSolid.Collidable=true;
      foreach (StaticMover smover in e.Scene.Tracker.GetComponents<StaticMover>()){
        if (smover.Platform == null && smover.IsRiding(intSolid)) sms.Add(smover);
      }
      intSolid.Collidable=old;
      //sms = MaddiesIop.playerInteractingSolid.get(e).staticMovers;
    }
    all.Add(e);
    if(sms!=null) foreach(var sm in sms) addAllSms(sm.Entity, all);
  }
  
  public ref struct PaddingLock:IDisposable{
    static int ctr = 0;
    public PaddingLock(){
      ctr++;
      hooks.enable();
    }
    void IDisposable.Dispose()=>ctr--;
    static bool phDelegate(bool orig){
      return orig && ctr==0;
    }
    static void paddingHook(ILContext ctx){
      var c = new ILCursor(ctx);
      while(c.TryGotoNext(MoveType.Before,itr=>itr.MatchCallvirt<Autotiler>(nameof(Autotiler.GenerateMap)))){
        c.EmitDelegate(phDelegate);
        c.Index++;
      }
    }
    static HookManager hooks = new(()=>{
      IL.Celeste.SolidTiles.ctor+=paddingHook;
      IL.Celeste.BackgroundTiles.ctor+=paddingHook;
    },()=>{
      IL.Celeste.SolidTiles.ctor-=paddingHook;
      IL.Celeste.BackgroundTiles.ctor-=paddingHook;
    });
  }
}

class DecalMarker:Component{
  public DecalData d;
  public string texstr=>d.Texture.Substring(0,d.Texture.Length-4);
  bool fg;
  public DecalMarker(DecalData data, bool fg):base(false,false){
    this.d=data;
    this.fg = fg;
  }
  static void FgLoad(Decal d, DecalData dat){
    d.Add(new DecalMarker(dat, true));
  }
  static void BgLoad(Decal d, DecalData dat){
    d.Add(new DecalMarker(dat, false));
  }
  public DecalData withDepthAndForcepos(Vector2 forcepos){
    DecalData n = Util.shallowCopy(d);
    n.Depth = fg?-10500:8000;
    n.Position = forcepos;
    return n;
  }
  [OnLoad.ILHook(typeof(Level),nameof(Level.orig_LoadLevel))]
  static void Manip(ILContext ctx){
    ILCursor c = new(ctx);
    foreach(int reg in new List<int>(){51,52}){
      if(c.TryGotoNextBestFit(MoveType.After,
        itr=>itr.MatchLdloc(reg),
        itr=>itr.MatchLdfld<DecalData>(nameof(DecalData.ColorHex)),
        itr=>itr.MatchNewobj<Decal>()
      )){
        c.EmitDup();
        c.EmitLdloc(reg);
        c.EmitDelegate<Action<Decal,DecalData>>(reg==51?FgLoad:BgLoad);
      } else goto bad;
    }
    return;
    bad: DebugConsole.WriteFailure("Could not add decal marking hook",true);
  }
}

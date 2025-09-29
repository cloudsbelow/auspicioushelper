


using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using Celeste.Mod.auspicioushelper.Wrappers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ConnectedBlocks", "auspicioushelper/ConnectedBlocksBg")]
[Tracked]
public class ConnectedBlocks:Entity{
  bool used;
  char tid;
  bool bg;
  Vector2 levelOffset;
  public ConnectedBlocks(EntityData d, Vector2 offset):base(offset+d.Position){
    Collider = new Hitbox(d.Width,d.Height);
    tid = d.Attr("tiletype","0").FirstOrDefault();
    bg = d.Name.EndsWith("Bg");
    levelOffset = offset;
  }
  static void FillRect(VirtualMap<char> m, Int2 tlc, Int2 brc, char v){
    for(int i=tlc.x; i<brc.x; i++) for(int j=tlc.y; j<brc.y; j++) m.orig_set_Item(i,j,v);
  }
  const int padding = 1;
  public override void Awake(Scene scene) {
    base.Awake(scene);
    if(!used){
      used = true;
      List<(IntRect,char,bool)> things = [new(new(this),tid,bg)];
      search:
        foreach(ConnectedBlocks c in Scene.Tracker.GetEntities<ConnectedBlocks>()){
          if(c.used) continue;
          IntRect r = new(c);
          foreach(var t in things) if(t.Item1.CollideIr(r)){
            things.Add(new(r,c.tid,c.bg));
            c.used = true;
            goto search;
          }
        }
      
      Int2 minimum = Int2.Round(Position);
      Int2 maximum = Int2.Round(Position);
      foreach(var t in things){
        minimum = Int2.Min(minimum, t.Item1.tlc);
        maximum = Int2.Max(maximum, t.Item1.brc);
      }
      VirtualMap<char> fgd = new(maximum.x-minimum.x+2*padding,maximum.y-minimum.y+2*padding,'0');
      VirtualMap<char> bgd = new(maximum.x-minimum.x+2*padding,maximum.y-minimum.y+2*padding,'0');
      var l = MipGrid.Layer.fromAreasize((maximum-minimum).x,(maximum-minimum).y);
      foreach(var t in things){
        Int2 dloc = (t.Item1.tlc-minimum)/8;
        Int2 hloc = (t.Item1.brc-minimum)/8;
        FillRect(t.Item3?bgd:fgd, dloc+padding, hloc+padding,t.Item2);
        l.SetRect(true,dloc,hloc);
      }
      SolidTiles s=null;
      BackgroundTiles b=null;
      InplaceFiller f = new(Int2.Zero+Int2.One*8*padding,maximum-minimum);
      f.setTiles(fgd);
      f.setTiles(bgd,false);
      if(f.fgt!=null)using(new PaddingLock()) s=new(Vector2.Zero, fgd);
      if(f.bgt!=null)using(new PaddingLock()) b=new(Vector2.Zero, bgd);
      Scene.Add(s);
      f.initStatic(s,b);
      MiptileCollider checker = new(l, Vector2.One*8, minimum, true);
      foreach(var pair in TemplateBehaviorChain.mainRoom){
        if(checker.collideFr(FloatRect.fromRadius(pair.Key+levelOffset,Vector2.One))){
          TemplateBehaviorChain.Chain chain = new(f, new List<EntityData>(){pair.Value,InplaceTemplateWrapper.creationDat}); 
          var first = chain.NextEnt();
          if(first == null) throw new Exception("idk shouldn't be possible");
          if(Level.EntityLoaders.TryGetValue(first.Name, out var loader)){
            Level lv = scene as Level;
            Entity e = loader(lv,lv.Session.LevelData,minimum-first.Position,first);
            if(e is Template te){
              te.t = chain.NextFiller();
              lv.Add(e);
              UpdateHook.EnsureUpdateAny();
              goto end;
            }
            throw new Exception($"your chained entity is not a template? how did u do this? {e}");
          }
        }
      }
      if(s!=null){
        s.Position+=minimum-Int2.One*8*padding;
        Scene.Add(s);
      }
      if(b!=null){
        b.Position+=minimum-Int2.One*8*padding;
        Scene.Add(b);
      }
    }
    end:
      RemoveSelf();
  }
  public class InplaceFiller:templateFiller{
    internal FgTiles saved = null;
    public InplaceFiller(Int2 tlc, Int2 size):base(tlc,size){}
  }
  public class InplaceTemplateWrapper:Template{
    public InplaceTemplateWrapper(Vector2 position):base(position, 0){
    }
    public override void addTo(Scene scene) {
      if(t is not InplaceFiller tf) throw new Exception("how");
      if(tf.saved is {} z){
        base.addTo(scene);
        restoreEnt(z);
        fgt=z;
        z.parentChangeStat(1,1,1);
        z.Add(new ChildMarker(this));
      } else {
        base.addTo(scene);
        tf.fgt = null;
        tf.Fgt = null;
      }
    }
    TemplateDisappearer.vcaTracker vca = new();
    public override void parentChangeStat(int vis, int col, int act) {
      base.parentChangeStat(vis, col, act);
      vca.Align(vis,col,act);
    }
    public override void destroy(bool particles) {
      if(fgt is {} z){
        fgt.parentChangeStat(vca.Visible?-1:0,vca.Collidable?-1:0,vca.Active?-1:0);
        z.fakeDestroy();
        ((IChildShaker) z).OnShakeFrame(Vector2.Zero);
        DebugConsole.Write("shake",gatheredShakeVec);
        children.Remove(fgt);
        if(t is not InplaceFiller tf) throw new Exception("how");
        tf.saved=z;
        List<IFreeableComp> l = new();
        foreach(var v in z.Components) if(v is IFreeableComp fc) l.Add(fc); 
        foreach(var v in l) v.Free();
      }
      base.destroy(particles);
    }
    public static EntityData creationDat=>new EntityData(){
      Name = "auspicioushelper/ConnectedBlockHolder",
      Position = Vector2.Zero
    };
  }
  
  public class PaddingLock:IDisposable{
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
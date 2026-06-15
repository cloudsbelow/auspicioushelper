


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static partial class Finder{
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
    class WrapperImg(Entity on, CMod from, Color[] colors):Entity(on.Position){
      public override void Update() {
        base.Update();
        if(on.Scene == null) RemoveSelf();
        Depth = Math.Max(on.Depth+1,2);
      }
      public override void Render() {
        if(from != null && !on.Collider.Equals(from.replace)) return;
        IntRect r = new FloatRect(on).munane();
        if(!on.Collidable){
          if(colors.Length<3 || !MaterialPipe.clipBounds.CollideIr(r)) return;
          Color c = colors[2];
          for(int i=0; i<r.w-1; i+=2){
            Draw.Rect(new IntRect(r.x+i,r.y,1,1),c);
            Draw.Rect(new IntRect(r.x+i+1,r.y+r.h-1,1,1),c);
          }
          for(int i=0; i<r.h-1; i+=2){
            Draw.Rect(new IntRect(r.x,r.y+i+1,1,1),c);
            Draw.Rect(new IntRect(r.x+r.w-1,r.y+i,1,1),c);
          }
          return;
        }
        base.Render();
        if(colors.Length==1 || colors[1].A==255) Draw.Rect(r,colors[0]);
        else Draw.HollowRect(r,colors[0]);
        if(colors.Length==1) return;
        Draw.Rect(r.expandAll_(-1),colors[1]);
      }
    }
    class CMod:ChannelTracker{
      Collider orig;
      public Collider replace;
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
        CMod c=null;
        if(d.tryGetStr("channel", out var str)){
          e.Add(c=new CMod(d,e,str));
        } else e.Collider = buildCollider(d);
        if(Util.listparseflat(d.Attr("boundsColors","")).Map(Util.hexToColor) is {Count: >0} a){
          addingLevel.Add(new WrapperImg(e,c,a.ToArray()));
        }
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
    [ResetEvents.ClearOn(ResetEvents.Times.NewAssets)]
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
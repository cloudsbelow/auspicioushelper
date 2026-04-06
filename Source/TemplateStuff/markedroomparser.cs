


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Celeste.Mod.Registry;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
internal static class MarkedRoomParser{
  public class TemplateRoom{
    public Dictionary<Vector2, EntityData> emptyTemplates = new();
    public LevelData d;
    public string Name=>d.Name;
    public Dictionary<string, templateFiller> templates;
    public TemplateRoom(LevelData data){
      d=data;
    }
    public TemplateRoom setTemplates(Dictionary<string, templateFiller> parsedTemplates){
      templates=parsedTemplates;
      foreach(var pair in templates)pair.Value.room = this; 
      return this;
    }
    public void addEmpty(EntityData e){
      if(!emptyTemplates.TryAdd(e.Position,e)){
        DebugConsole.WriteFailure("overlapping empty templates");
      }
    }
  }
  public static Dictionary<string, TemplateRoom> staticRooms = new();
  static Dictionary<string, TemplateRoom> dynamicRooms = new();
  public static void clearDynamicRooms()=>dynamicRooms.Clear();
  public static void AddDynamicRooms(List<TemplateRoom> rooms){
    foreach(var room in rooms) dynamicRooms[room.Name] = room;
  }

  internal static string sigstr = "zztemplates";
  public const int tilepadding = 8;
  public const int padding = 1;
  static IEnumerable<templateFiller> GetPermitted(
    List<(QuickCollider<ConnectedBlocks>, templateFiller,MiptileCollider)> li, 
    FloatRect f, string str, bool d, bool s, bool t
  ){
    foreach(var (qcl, filler, _) in li) foreach(var c in qcl.Test(f)){
      if(c.permits(str,d,s,t)){
        yield return filler;
        break;
      }  
    }
  }
  internal static TemplateRoom parseLeveldata(LevelData l, bool simulatedRoom = false){
    List<(FloatRect,templateFiller)> fillerBounds = new();
    Dictionary<string, templateFiller> templates = new();
    var room = new TemplateRoom(l);
    foreach(EntityData d in l.Entities){
      templateFiller t = null;
      if(d.Name == "auspicioushelper/templateFiller"){
        t = new templateFiller(d, l.Position);
        t.setRoomdat(l);
        fillerBounds.Add(new(t.data.roomRect,t));
        t.tiledata.setTiles(l.Solids,l.Bg);
      } else if(d.Name == "auspicioushelper/TemplateFillerSwitcher"){
        templateFiller.FillerSwitcher sw = new(d);
        t = sw;
      }
      if(t == null) continue; 
      if(templates.ContainsKey(t.name)){
        DebugConsole.WriteFailure("Multiple templates with the same identifier "+t.name);
        if(t.name!="" && auspicioushelperModule.InFolderMod){
          DebugConsole.MakePostcard($"Failed to load. Room {l.Name} has multiple templates with name {t.name}");
        }
      } else templates.Add(t.name,t);
    }
    if(fillerBounds.Count==0) return null;

    List<EntityData> filtered = new(l.Entities.Count);
    List<(IntRect,EntityData, int)> allThings=new();
    foreach(EntityData d in l.Entities){
      if(d.Name == "auspicioushelper/templateFiller" || d.Name == "auspicioushelper/TemplateFillerSwitcher") continue;
      bool w=EntityParser.generateLoader(d, l, null, out var typ);
      if(typ==EntityParser.Types.template || d.Name == "auspicioushelper/TemplateBehaviorChain"){  
        if(string.IsNullOrWhiteSpace(d.Attr("template"))){
          room.addEmpty(d);
          continue;
        }
      }
      if(d.Name.StartsWith("auspicioushelper/Connected") && ConnectedBlocks.allNames.Contains(d.Name)){
        allThings.Add(new(new((int)d.Position.X,(int)d.Position.Y,d.Width,d.Height),d,allThings.Count));
      } else if(w) filtered.Add(d);
      if(!w) continue;
    }

    List<(QuickCollider<ConnectedBlocks>,templateFiller,MiptileCollider)> cbs = new();
    List<(Vector2, EntityData)> displacers = new();
    while(allThings.Count>0){
      List<(IntRect,EntityData, int)> things = new();
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
      things.Sort((a,b)=>a.Item3-b.Item3);
      VirtualMap<char> fgd = new(size.x,size.y,'0');
      VirtualMap<char> bgd = new(size.x,size.y,'0');
      QuickCollider<ConnectedBlocks> qcl = new();
      var layer = MipGrid.Layer.fromAreasize(size.x,size.y);
      foreach(var a in things){
        ConnectedBlocks.Category c = ConnectedBlocks.Category.fgt;
        if(a.Item2.Name.EndsWith("Bg"))c = ConnectedBlocks.Category.bgt;
        else if(a.Item2.Name.EndsWith("er")) c=ConnectedBlocks.Category.ent;
        Int2 dloc = (a.Item1.tlc-min)/8;
        Int2 hloc = (a.Item1.brc-min)/8;
        layer.SetRect(true,dloc,hloc);
        char tid = a.Item2.Attr("tiletype","0").FirstOrDefault();
        switch(c){
          case ConnectedBlocks.Category.fgt:ConnectedBlocks.FillRect(fgd, dloc+padding, hloc+padding,tid);break;
          case ConnectedBlocks.Category.bgt:ConnectedBlocks.FillRect(bgd, dloc+padding, hloc+padding,tid);break;
          case ConnectedBlocks.Category.ent: qcl.Add(new(a.Item2,Vector2.Zero),a.Item1); break;
        } 
      }

      templateFiller f = new(min,max-min);
      string name = "__auto_"+cbs.Count;
      while(!templates.TryAdd(name,f)) name+="_";
      f.name=name;
      f.tiledata.setTiles(fgd,true,Int2.One*padding);
      f.tiledata.setTiles(bgd,false,Int2.One*padding);
      f.tiledata.createStatically = true;
      f.setRoomdat(l);
      EntityData hit = null;
      MiptileCollider checker = new(layer, Vector2.One*8, min, true);
      foreach(var (k,v) in room.emptyTemplates) if(checker.collideFr(FloatRect.fromRadius(k,Vector2.One))){
        if(hit!=null)DebugConsole.MakePostcard($"Multiple empty templates cover a connected template in {l.Name}");
        else hit = v;
      } //
      hit??=new EntityData(){
        Name=EntityParser.TemplateEmptyName,
        Position=min, Values=new(),
        ID = things[0].Item2.ID
      };
      hit = hit.cloneWithValues([new("template",name)]);
      f.data.offset = min-hit.Position;
      cbs.Add(new(qcl,f,checker));
      foreach(var (r,t) in fillerBounds) if(checker.collideFr(r)){
        t.data.ChildEntities.Add(hit);
      }

      bool force = hit.Name=="auspicioushelper/TemplateBehaviorChain"&&hit.Bool("forceOwnPosition",false);
      Vector2? forcepos = force? hit.Position:null;
      var chain = new TemplateBehaviorChain.Chain(f, hit, forcepos, room.emptyTemplates);
      var disp = chain.NextEnt(); 
      disp??=new EntityData(){Name=EntityParser.TemplateEmptyName,Position=hit.Position,Values=new()}; 
      if(disp.Name=="auspicioushelper/TemplateDisplacer"){
        string name2 = name+"_disp";
        var first = chain.NextEnt();
        first??=new EntityData(){
          Name=EntityParser.TemplateEmptyName,
          Position=disp.Position, Values=new()
        };
        templateFiller dispFill = chain.NextFiller();//Even if chain is null, this is clamped to final
        if(dispFill==f){
          name2=name;
        } else while(!templates.TryAdd(name2,dispFill)) name2+="_";
        int depth = first.Int("depthoffset",0) + disp.Int("depthoffset",0);
        first = first.cloneWithValues([new("template",name2),new("depthoffset",depth),new("prefixid",hit.ID)]);
        int i=0;
        foreach(var n in disp.Nodes??[]){
          var dn = first.cloneWithForceposOffset(n);
          dn.ID = i++; 
          displacers.Add(new(n,dn));
        }
      }
    }   // if you're reading this you're probably really happy! I am too
    List<int> into = new();
    foreach(EntityData d in filtered){
      var types = EntityRegistry.GetKnownTypesFromSid(d.Name);
      var solid = types.Any(a=>a.IsAssignableTo(typeof(Solid)));
      bool flag=false;
      FloatRect bounds = new(d,0);
      if(bounds.w==0) bounds.expandAllH(1);
      if(bounds.h==0) bounds.expandAllV(1); 
      foreach(var t in GetPermitted(cbs,bounds,d.Name,false,solid,false)){
        t.data.ChildEntities.Add(d);
        flag = true;
      } //
      if(!flag) foreach(var (b,t) in fillerBounds) {
        if(b.CollidePointCompact(d.Position)) t.data.ChildEntities.Add(d);
      } //
    }
    foreach(var (n,d) in displacers){
      bool flag=false;
      foreach(var (_,t,checker) in cbs) if(checker.collideFr(FloatRect.fromRadius(n,Vector2.One))){
        t.data.ChildEntities.Add(d);
        flag = true;
      }
      if(!flag) foreach(var (b,t) in fillerBounds){
        if(b.CollidePointCompact(n)) t.data.ChildEntities.Add(d);
      }
    }
    foreach(EntityData d in l.Triggers){ 
      bool flag=false;
      foreach(var t in GetPermitted(cbs,new(d,0),d.Name,false,false,true)){
        t.data.ChildEntities.Add(d);
        flag = true;
      }
      if(!flag) foreach(var (b,t) in fillerBounds) {
        if(b.CollidePointCompact(d.Position)) t.data.ChildEntities.Add(d);
      } 
    } 
    foreach(DecalData d in l.FgDecals){
      var nd = d.WithFallbackDepth(-10500);
      bool flag=false;
      var bounds = FloatRect.fromRadius(d.Position,Vector2.One);
      foreach(var t in GetPermitted(cbs,bounds,d.Texture,true,false,false)){
        t.data.decals.Add(nd);
        flag = true;
      }
      if(!flag) foreach(var (b,t) in fillerBounds) {
        if(b.CollidePointCompact(d.Position)) t.data.decals.Add(nd);
      }
    }
    foreach(DecalData d in l.BgDecals){
      var nd = d.WithFallbackDepth(9000);
      bool flag=false;
      var bounds = FloatRect.fromRadius(d.Position,Vector2.One);
      foreach(var t in GetPermitted(cbs,bounds,d.Texture,true,false,false)){
        t.data.decals.Add(nd);
        flag = true;
      }
      if(!flag) foreach(var (b,t) in fillerBounds) {
        if(b.CollidePointCompact(d.Position)) t.data.decals.Add(nd);
      }
    }
    
    if(simulatedRoom) using(new ConnectedBlocks.PaddingLock()){
      var fgtd = Util.toCharmap(l.Solids, tilepadding);
      var bgtd = Util.toCharmap(l.Bg, tilepadding);
      var fgt = new SolidTiles(-Vector2.One*tilepadding*8,fgtd);
      var bgt = new BackgroundTiles(-Vector2.One*tilepadding*8,bgtd);
      foreach(var pair in templates){
        pair.Value.tiledata.initStatic(fgt,bgt);
      }
    }
    if(templates.Count==0) return null;
    return room.setTemplates(templates);
  }
  static Regex re = new(@"^\s*zz\w*(?:[^a-zA-Z0-9](.+)|())\s*$",RegexOptions.Compiled);
  internal static void parseMapdata(MapData m){
    staticRooms.Clear();
    dynamicRooms.Clear();
    foreach(LevelData l in m.Levels){
      var g = re.Match(l.Name);
      if(g.Success){
        var room = parseLeveldata(l);
        if(room!=null) staticRooms.Add(g.Groups[1].Success?g.Groups[1].Value:"",parseLeveldata(l));
      }
    }
  }
  static readonly HashSet<char> delimiters=new(){'$','#','@','%',':',';'};
  public static bool getTemplate(string templatestr, TemplateRoom parentRoom, Scene scene, out templateFiller filler){
    if(true){
      int i=0;
      for(;i<templatestr.Length && !delimiters.Contains(templatestr[i]);i++){}
      if(i!=templatestr.Length) templatestr = templatestr.Substring(i);
    }
    string[] ts = templatestr.Split('/');
    for(int idx = ts.Length-1; idx>=0; idx--){
      string b = "";
      for(int i=0; i<idx; i++)b+=ts[i];
      string e = "";
      for(int i=idx; i<ts.Length; i++)e+=ts[i];
      TemplateRoom dr=null;
      if(idx==0){
        if(parentRoom!=null){
          if(parentRoom.templates.TryGetValue(e, out filler)) goto success;
        } else {
          string ldn = (scene as Level)?.Session.LevelData.Name??"NULL";
          if(dynamicRooms.TryGetValue(ldn, out dr)){
            if(dr.templates.TryGetValue(e, out filler)) goto success;
          }
          if(staticRooms.TryGetValue(ldn,out dr)){
            if(dr.templates.TryGetValue(e, out filler)) goto success;
          }
        }
      }
      if(dynamicRooms.TryGetValue(b, out dr)){
        if(dr.templates.TryGetValue(e, out filler)) goto success;
      }
      if(staticRooms.TryGetValue(b,out dr)){
        if(dr.templates.TryGetValue(e, out filler)) goto success;
      }
    }
    filler = null;
    return false;
    success:
      filler = filler.GetInstance();
      return true;
  }
  static DecalData WithFallbackDepth(this DecalData d,int fallbackDepth)=>new DecalData(){
    Texture=d.Texture, Position=d.Position,
    Scale=d.Scale, Rotation=d.Rotation, ColorHex=d.ColorHex,
    Depth=d.Depth??fallbackDepth
  };
}
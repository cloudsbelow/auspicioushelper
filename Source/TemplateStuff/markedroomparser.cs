


using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Editor;
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
  const int tilepadding = 8;
  internal static TemplateRoom parseLeveldata(LevelData l, bool simulatedRoom = false){
    var rects = new StaticCollisiontree();
    var handleDict = new Dictionary<int, string>();
    Dictionary<string, templateFiller> templates = new();
    var room = new TemplateRoom(l);
    foreach(EntityData d in l.Entities){
      if(d.Name == "auspicioushelper/templateFiller"){
        templateFiller t = new templateFiller(d, l.Position){
          roomdat = l
        }; //we are in frame of room <3
        string id = t.name;
        if(templates.ContainsKey(id)){
          DebugConsole.WriteFailure("Multiple templates with the same identifier "+id);
          continue;
        }
        int handle = rects.add(new FloatRect(t));
        handleDict.Add(handle, id);
        templates.Add(id,t);
        //DebugConsole.Write(id);
        t.setTiles(l.Solids,l.Bg);
      }
      if(d.Name == "auspicioushelper/EntityMarkingFlag"){
        EntityMarkingFlag.hooks.enable();
        EntityMarkingFlag.watch(d.Attr("path"),d.Attr("identifier"));
      }
    }
    foreach(EntityData d in l.Entities){
      if(d.Name == "auspicioushelper/templateFiller") continue;
      //DebugConsole.Write("Looking at entity "+d.Name);
      var hits = rects.collidePointAll(d.Position);
      bool w=false;
      w = EntityParser.generateLoader(d, l, null, out var typ);
      if(typ==EntityParser.Types.template || d.Name == "auspicioushelper/TemplateBehaviorChain"){  
        if(string.IsNullOrWhiteSpace(d.Attr("template"))){
          room.addEmpty(d);
          continue;
        }
      }
      if(!w || hits.Count==0) continue;
      foreach(int handle in hits){
        string tid = handleDict[handle];
        templates.TryGetValue(tid, out var temp);
        if(temp == null) continue;
        temp.ChildEntities.Add(d);
      }
    }
    foreach(EntityData d in l.Triggers){
      var hits = rects.collidePointAll(d.Position);
      bool w=false;
      if(hits.Count >0) w = EntityParser.generateLoader(d, l);
      if(!w) continue;
      foreach(int handle in hits){
        string tid = handleDict[handle];
        templates.TryGetValue(tid, out var temp);
        if(temp == null) continue;
        temp.ChildEntities.Add(d);
      }
    }
    foreach(DecalData d in l.FgDecals){
      var hits = rects.collidePointAll(d.Position);
      foreach(int handle in hits){
        string tid = handleDict[handle];
        templates.TryGetValue(tid, out var temp);
        if(temp == null) continue;
        temp.decals.Add(new DecalData(){
          Texture = d.Texture,
          Position = d.Position,
          Scale = d.Scale,
          Rotation = d.Rotation,
          ColorHex = d.ColorHex,
          Depth = d.Depth??-10500
        });
      }
    }
    foreach(DecalData d in l.BgDecals){
      var hits = rects.collidePointAll(d.Position);
      foreach(int handle in hits){
        string tid = handleDict[handle];
        templates.TryGetValue(tid, out var temp);
        if(temp == null) continue;
        temp.decals.Add(new DecalData(){
          Texture = d.Texture,
          Position = d.Position,
          Scale = d.Scale,
          Rotation = d.Rotation,
          ColorHex = d.ColorHex,
          Depth = d.Depth??9000
        });
      }
    }
    
    if(simulatedRoom){
      var fgtd = Util.toCharmap(l.Solids, tilepadding);
      var bgtd = Util.toCharmap(l.Bg, tilepadding);
      var fgt = new SolidTiles(-Vector2.One*tilepadding*8,fgtd);
      var bgt = new BackgroundTiles(-Vector2.One*tilepadding*8,bgtd);
      foreach(var pair in templates){
        pair.Value.initStatic(fgt,bgt);
      }
    }
    return room.setTemplates(templates);
  }
  internal static void parseMapdata(MapData m){
    staticRooms.Clear();
    dynamicRooms.Clear();
    EntityMarkingFlag.clear();
    foreach(LevelData l in m.Levels){
      if(l.Name.StartsWith(sigstr+"-")||l.Name == sigstr){
        DebugConsole.Write("Parsing "+l.Name);
        string prefix = l.Name == sigstr?"":l.Name.Substring(sigstr.Length+1);
        staticRooms.Add(prefix,parseLeveldata(l));
      }
    }
  }
  public static bool getTemplate(string templatestr, Template parent, Scene scene, out templateFiller filler){
    string[] ts = templatestr.Split('/');
    for(int idx = ts.Length-1; idx>=0; idx--){
      string b = "";
      for(int i=0; i<idx; i++)b+=ts[i];
      string e = "";
      for(int i=idx; i<ts.Length; i++)e+=ts[i];
      TemplateRoom dr=null;
      if(idx==0){
        if(parent!=null){
          if(parent.t.room.templates.TryGetValue(e, out filler)) return true;
        } else {
          string ldn = (scene as Level)?.Session.LevelData.Name??"NULL";
          if(dynamicRooms.TryGetValue(ldn, out dr)){
            if(dr.templates.TryGetValue(e, out filler)) return true;
          }
          if(staticRooms.TryGetValue(ldn,out dr)){
            if(dr.templates.TryGetValue(e, out filler)) return true;
          }
        }
      }
      if(dynamicRooms.TryGetValue(b, out dr)){
        if(dr.templates.TryGetValue(e, out filler)) return true;
      }
      if(staticRooms.TryGetValue(b,out dr)){
        if(dr.templates.TryGetValue(e, out filler)) return true;
      }
    }
    filler = null;
    return false;
  }
}
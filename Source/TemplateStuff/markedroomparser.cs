


using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
internal static class MarkedRoomParser{

  internal static Dictionary<string, templateFiller> templates = new Dictionary<string, templateFiller>();
  internal static string sigstr = "zztemplates";
  internal static void parseLeveldata(LevelData l, string prefix){
    var rects = new StaticCollisiontree();
    var handleDict = new Dictionary<int, string>();
    foreach(EntityData d in l.Entities){
      if(d.Name == "auspicioushelper/templateFiller"){
        templateFiller t = new templateFiller(d, l.Position){
          roomdat = l
        }; //we are in frame of room <3
        string id = prefix+t.name;
        if(templates.ContainsKey(id)){
          DebugConsole.Write("Multiple templates with the same identifier "+id);
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
      if(hits.Count >0) w = EntityParser.generateLoader(d, l);
      if(!w) continue;
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
          Depth = d.GetDepth(-10500)
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
          Depth = d.GetDepth(9000)
        });
      }
    }
  }
  internal static void parseMapdata(MapData m){
    templates.Clear();
    EntityMarkingFlag.clear();
    foreach(LevelData l in m.Levels){
      if(l.Name.StartsWith(sigstr+"-")||l.Name == sigstr){
        DebugConsole.Write("Parsing "+l.Name);
        string prefix = l.Name == sigstr?"":l.Name.Substring(sigstr.Length+1)+"/";
        parseLeveldata(l, prefix);

      }
    }
  }
}



using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Rysy;
using Rysy.Entities;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Shared;

namespace auspicioushelper.Rysy;


public partial class RoomData{
  public Dictionary<string, TemplateFiller> templates = null;
  public string templatePrefix = null;
}
public partial class FrameRenderdata{
  public Dictionary<string, RoomData> templateRooms = null;
}
public class TemplateFiller:LonnEntity{
  public bool visibleExt;
  Int2 offset;
  IntRect bounds;
  Int2 origin=>bounds.tlc+offset;
  IntRect tr;
  string prefix;
  static Regex re = new(@"^\s*zz\w*(?:[^a-zA-Z0-9](.+)|())\s*$",RegexOptions.Compiled);
  public static void OnRoomChanged(Room r, RoomData rd){
    var m = re.Match(r.Name);
    if(m.Success && r.Entities.OfType<TemplateFiller>() is {Count: >1} li){
      string prefix = rd.templatePrefix = m.Groups[1].Success?m.Groups[1].Value:"";
      if(rd.templates is not {} te) te = rd.templates=new();
      else te.Clear();

      foreach(var t in li){
        var name = t.Attr("name");
        if(string.IsNullOrWhiteSpace(name)) continue;
        if(te.ContainsKey(name)){
          ausModule.Log(LogLevel.Warning,"Multiple templates with the same identifier",name);
        } else te.Add(name,t);
        t.bounds = new(Int2.Round(t.Pos),t.Width,t.Height);
        t.tr = new(Int2.Round(t.Pos)/8, t.Width/8, t.Height/8);
        t.offset = t.Nodes?.Count>0? Int2.Round(t.Pos-t.Nodes[0].Pos): Int2.Zero;
        t.prefix = prefix;
      }      
    } else {
      rd.templates = null;
      rd.templatePrefix = null;
    }
  }
  struct FillerTiler(TemplateFiller f):ConnectedTiles.ISimpleTilechecker{
    public char GetTileAt(int x, int y, char def){
      if(x<0 || y<0 || x>=f.tr.w || y>=f.tr.h) return def;
      var nloc = new Int2(x,y)+f.tr.tlc;
      var data = f.Room.Fg.Tiles;
      if(nloc.x<0 || nloc.y<0 || nloc.x>=data.GetLength(0) || nloc.y>=data.GetLength(1)) return def;
      return data[x,y];
    }
  }
  
  List<Entity> cachedEnts = null;
  static Action<Entity,Vector2> setPos = (Action<Entity,Vector2>)typeof(Entity)
    .GetMethod("SilentSetPos",Util.GoodBindingFlags)
    .CreateDelegate(typeof(Action<Entity,Vector2>));
  public IEnumerable<ISprite> renderAt(Int2 pos){
    if(cachedEnts == null){
      cachedEnts = new();
      foreach(var e in Room.Entities){
        if(e is TemplateFiller) continue;
        if(bounds.CollidePointCompact(Int2.Round(e.Pos))) cachedEnts.Add(e);
      }
      foreach(var e in Room.Triggers) if(bounds.CollidePointCompact(Int2.Round(e.Pos))) cachedEnts.Add(e);
      foreach(var e in Room.FgDecals) if(bounds.CollidePointCompact(Int2.Round(e.Pos))) cachedEnts.Add(e);
      foreach(var e in Room.BgDecals) if(bounds.CollidePointCompact(Int2.Round(e.Pos))) cachedEnts.Add(e);
    }
    IEnumerable<ISprite> enumerable = Array.Empty<ISprite>();
    Vector2 shift = pos-origin;
    foreach(var e in cachedEnts){
      Vector2 oldpos = e.Pos;
      setPos(e, oldpos+shift);
      enumerable.Concat(e.GetSprites());
      setPos(e,oldpos);
    }
    return enumerable;
  }

  static public void setupRooms(Map m){
    if(ausModule.frameData.templateRooms == null){
      var rs = ausModule.frameData.templateRooms = new();
      foreach(var r in m.Rooms){
        var match = re.Match(r.Name);
        if(match.Success && r.Entities.OfType<TemplateFiller>() is {Count: >1} 
          && ausModule.TryGetRoomdat(r, out var rd) && rd.templatePrefix is not null
        ){
          if(!rs.TryAdd(rd.templatePrefix, rd)){
          ausModule.Log(LogLevel.Warning, "Multiple template rooms map to prefix", rd.templatePrefix);
          } else ausModule.Log(LogLevel.Info, "Got room", rd.templatePrefix);
        }
      }
    }
  }
  static readonly HashSet<char> delimiters=new(){'$','#','@','%',':',';'};
  public static TemplateFiller getFiller(string templatestr, Template from){
    setupRooms(from.Room.Map);
    if(true){
      int i=0;
      for(;i<templatestr.Length && !delimiters.Contains(templatestr[i]);i++){}
      if(i!=templatestr.Length) templatestr = templatestr.Substring(i);
    }
    string[] ts = templatestr.Split('/');
    string e = ts[^1];
    string b = ts[0];
    var rooms = ausModule.frameData.templateRooms;
    if(ts.Length==1){
      RoomData singleRoom;
      if(ausModule.TryGetRoomdat(from.Room, out var srd) && srd.templates!=null) singleRoom = srd;
      else singleRoom = rooms.GetValueOrDefault(from.Room.Name);
      if(singleRoom!=null && singleRoom.templates.TryGetValue(e, out var f1)) return f1;
      b="";
    }
    if(rooms.TryGetValue(b, out var rd) && rd.templates.TryGetValue(e, out var f2)) return f2;
    return null;
  }
}
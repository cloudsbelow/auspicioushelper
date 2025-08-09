


using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/EvilPackedTemplateRoom")]
public class EvilPackedTemplateRoom:Entity{
  static Dictionary<int,List<MarkedRoomParser.TemplateRoom>> parsed = new();
  const int latestVersion = 1;
  static EvilPackedTemplateRoom(){
    auspicioushelperModule.OnEnterMap.enroll(new PersistantAction(()=>{
      parsed.Clear();
    },"evil packed template cleanup"));
  }
  public EvilPackedTemplateRoom(EntityData d,Vector2 offset):base(Vector2.Zero){
    string dat = d.Attr("EncodedRooms", "").Trim();
    DebugConsole.Write("Thing constructed");
    if(string.IsNullOrEmpty(dat)) return;
    if(parsed.TryGetValue(d.ID, out var l)){
      MarkedRoomParser.AddDynamicRooms(l);
    } else try {
      var arr = Convert.FromBase64String(dat);
      var stream = new MemoryStream(arr);
      using var header = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
      int version = header.ReadInt16();
      if(version>latestVersion || version<=0) throw new Exception("Invalid version");
      int nrooms = header.ReadInt16();
      if(nrooms>4096) throw new Exception("somehow I doubt having 4096+ packed tempalte rooms is valid; throwing this to be safe");

      var brotli = new BrotliStream(stream, CompressionMode.Decompress);
      var r = new BinaryReader(brotli,Encoding.UTF8);
      List<MarkedRoomParser.TemplateRoom> rooms = new();
      for(int i=0; i<nrooms; i++) rooms.Add(MarkedRoomParser.parseLeveldata(Util.ReadLeveldata(r), true));
      MarkedRoomParser.AddDynamicRooms(rooms);
      parsed.Add(d.ID, rooms);
    } catch(Exception ex){
      DebugConsole.WriteFailure("Could not load your packed template room: \n"+ex.ToString());
      Logger.Warn("auspicioushelper",$"Could not load your packed template room with id {d.ID}: \n"+ex.ToString());
    }
  }
  public static void PackTemplatesEvil(){
    using var ms = new MemoryStream();
    using (var header = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true)){
      header.Write((short)latestVersion);
      header.Write((short) MarkedRoomParser.staticRooms.Count);
    }
    using(var brotli = new BrotliStream(ms, CompressionLevel.Optimal, leaveOpen: true))
    using(var w = new BinaryWriter(brotli, Encoding.UTF8, leaveOpen:true)){
      foreach(var pair in MarkedRoomParser.staticRooms) Util.Write(w,pair.Value.d,pair.Key,latestVersion);
    }
    string str = Convert.ToBase64String(ms.ToArray());
    Logger.Info("auspicioushelper","\n\n========SERIALIZED ZZTEMPLATES ROOMS========\n"+str+"\n=========END ZZTEMPLATES ROOMS==========\n\n");
    DebugConsole.Write($"\n\n========SERIALIZED ZZTEMPLATES ROOMS ({str.Length})========\n"+str+"\n=========END ZZTEMPLATES ROOMS==========\n\n");
  }
  public override void Added(Scene scene) {
    RemoveSelf();
  }
}
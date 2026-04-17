using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Rysy;
using Rysy.Components;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Shared;

namespace auspicioushelper.Rysy;

public partial class RoomData{

}
public partial class FrameRenderdata{

}
public sealed class ausModule : ModModule{
  static ConditionalWeakTable<Room, RoomData> data = new();
  public static FrameRenderdata frameData = new();
  public static RoomData GetOrMakeDat(Room r){
    if(!data.TryGetValue(r, out var rd)) data.Add(r,rd = new());
    return rd;
  }
  public static bool TryGetRoomdat(Room r, out RoomData rd)=>data.TryGetValue(r, out rd);
  class AuspRenderProvider:IRoomSpriteProvider{
    //A test
    IReadOnlyList<ISprite> IRoomSpriteProvider.GetSprites(Room room) {
      RoomData rd = GetOrMakeDat(room);
      Instance.Logger.Log(LogLevel.Info,"Provider used on "+room.Name);
      TemplateFiller.OnRoomChanged(room,rd);
      var ret =  ConnectedTiles.ProcessScene(room,rd);
      frameData = new();
      return ret;
    }
  }
  public static ausModule Instance;
  public override void Load(){
    base.Load();
    ComponentRegistry.Add(new AuspRenderProvider());
    Logger.Log(LogLevel.Info,"Auspicious module load");
    Instance = this;
  }
  static public void Log(LogLevel level, params object[] items){
    string s = "";
    try{
      for(int i=0; i<items.Length; i++){
        if(i!=0) s+=" ";
        if(items[i] == null) s+="NULL";
        else if(items[i] is string str && string.IsNullOrWhiteSpace(str)) s+=$"\"{s}\"";
        else s+=items[i].ToString();
      }
      Instance?.Logger?.Log(level,s);
    }catch(Exception){}
  }
}



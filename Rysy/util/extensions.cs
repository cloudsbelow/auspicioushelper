


using Microsoft.Xna.Framework;
using Rysy.LuaSupport;

namespace auspicioushelper.Rysy;

public static class Extensions{
  public static RoomData GetRoomdata(this LonnEntity ent)=>ausModule.GetOrMakeDat(ent.Room);
  public static Vector4 AsVector(this Color c)=>new Vector4(c.R,c.G,c.B,c.A)/255;
}
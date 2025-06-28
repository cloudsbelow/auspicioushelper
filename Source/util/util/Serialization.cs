



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FMOD;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public partial class Util{
  public static void Write(BinaryWriter w, Vector2 v){
    w.Write(v.X); w.Write(v.Y);
  }
  public static Vector2 ReadVec2(BinaryReader r){
    return new Vector2(r.ReadSingle(),r.ReadSingle());
  }
  public static void WriteBoxed(BinaryWriter w, object o){
    switch(o){
      case bool b:
        w.Write((byte)(b?1:0)); break;
      case int i:
        w.Write((byte)2); w.Write(i); break;
      case float f:
        w.Write((byte)3); w.Write(f); break;
      case double d:
        w.Write((byte)4); w.Write(d); break;
      default:
        w.Write((byte)5); w.Write(o.ToString()); break;
    }
  }
  public static object ReadBoxed(BinaryReader r){
    switch(r.ReadByte()){
      case 0: return false;
      case 1: return true;
      case 2: return r.ReadInt32();
      case 3: return r.ReadSingle();
      case 4: return r.ReadDouble();
      case 5: return r.ReadString();
      default: throw new Exception("Bad object encoding");
    }
  }
  public static void Write(BinaryWriter w, EntityData d){
    w.Write(d.ID);
    w.Write(d.Name);
    Write(w,d.Position);
    w.Write7BitEncodedInt(d.Width);
    w.Write7BitEncodedInt(d.Height);
    if(d.Nodes!=null){
      w.Write7BitEncodedInt(d.Nodes.Length);
      foreach(Vector2 n in d.Nodes) Write(w,n);
    } else w.Write7BitEncodedInt(0);
    if(d.Values!=null){
      var l = d.Values.ToList();
      w.Write7BitEncodedInt(l.Count);
      foreach(var pair in l){
        w.Write(pair.Key); WriteBoxed(w,pair.Value);
      }
    } else w.Write7BitEncodedInt(0);
  }
  public static EntityData ReadEntitydata(BinaryReader r){
    EntityData e = new();
    e.ID=r.ReadInt32();
    e.Name=r.ReadString();
    e.Position=ReadVec2(r);
    e.Width=r.Read7BitEncodedInt();
    e.Height=r.Read7BitEncodedInt();
    int nlen = r.Read7BitEncodedInt();
    if(nlen != 0){
      e.Nodes = new Vector2[nlen];
      for(int i=0; i<nlen; i++) e.Nodes[i]=ReadVec2(r);
    } 
    int vlen = r.Read7BitEncodedInt();
    e.Values = new();
    for(int i=0; i<vlen; i++){
      e.Values.Add(r.ReadString(),ReadBoxed(r));
    }
    return e;
  }
  public static void Write(BinaryWriter w, DecalData d){
    w.Write(d.Texture);
    Write(w,d.Position);
    Write(w,d.Scale);
    w.Write(d.Rotation);
    w.Write(d.ColorHex);
    if(d.Depth is int depth){
      w.Write((byte) 1); w.Write(depth);
    } else w.Write((byte) 0);
  }
  public static DecalData ReadDecaldata(BinaryReader r){
    DecalData d = new();
    d.Texture = r.ReadString();
    d.Position = ReadVec2(r);
    d.Scale = ReadVec2(r);
    d.Rotation = r.ReadSingle();
    d.ColorHex = r.ReadString();
    if(r.ReadByte() == 1) d.Depth = r.ReadInt32();
    return d;
  }
  public static void Write(BinaryWriter w, LevelData d, string overridename = null, int version=1){
    w.Write(overridename??d.Name);
    w.Write7BitEncodedInt((int)d.Entities.Count);
    foreach(EntityData e in d.Entities) Write(w,e);
    w.Write7BitEncodedInt((int)d.Triggers.Count);
    foreach(EntityData e in d.Triggers) Write(w,e);
    w.Write7BitEncodedInt((int)d.FgDecals.Count);
    foreach(DecalData e in d.FgDecals) Write(w,e);
    w.Write7BitEncodedInt((int)d.BgDecals.Count);
    foreach(DecalData e in d.BgDecals) Write(w,e);
    w.Write(d.Solids);
    w.Write(d.Bg);
  }
  public static LevelData ReadLeveldata(BinaryReader r, int version=1){
    if(version!=1) throw new Exception("versioning error");
    LevelData d = (LevelData)RuntimeHelpers.GetUninitializedObject(typeof(LevelData));
    d.Name = r.ReadString();
    DebugConsole.Write($"Got smuggled room: {d.Name}");
    d.Entities = new();
    int n = r.Read7BitEncodedInt();
    DebugConsole.Write($"{n} entities");
    for(int i=0; i<n; i++) d.Entities.Add(ReadEntitydata(r));
    d.Triggers = new();
    n = r.Read7BitEncodedInt();
    DebugConsole.Write($"{n} triggers");
    for(int i=0; i<n; i++) d.Triggers.Add(ReadEntitydata(r));
    d.FgDecals = new();
    n = r.Read7BitEncodedInt();
    DebugConsole.Write($"{n} fgdecals");
    for(int i=0; i<n; i++) d.FgDecals.Add(ReadDecaldata(r));
    d.BgDecals = new();
    n = r.Read7BitEncodedInt();
    DebugConsole.Write($"{n} bgdecals");
    for(int i=0; i<n; i++) d.BgDecals.Add(ReadDecaldata(r));
    d.Solids = r.ReadString();
    DebugConsole.Write(d.Solids);
    d.Bg = r.ReadString();
    DebugConsole.Write(d.Bg);
    d.Bounds.X=0;
    d.Bounds.Y=0;
    return d;
  }

}
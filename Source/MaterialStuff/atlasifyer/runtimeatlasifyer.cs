


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static class Atlasifyer{
  static Dictionary<string, MTexture> contains=new();
  static List<MTexture> texs=new();
  static Atlasifyer(){
    auspicioushelperModule.OnEnterMap.enroll(new PersistantAction(()=>{
      DebugConsole.Write("Clearing atlases");
      contains.Clear();
      foreach(var tex in texs) tex.Texture.Dispose();
      texs.Clear();
      cy=th+1;
    }));
  }
  const int tw=1024;
  const int th=1024;
  static int cy=th+1;
  static int mh=0;
  static int cx=0;
  //this is a very unoptimal algo.
  static int texCounter=0;
  public static MTexture PushToAtlas(Color[] data, int w, int h, string uid){
    if(contains.TryGetValue(uid, out var tex)) return tex;
    if(w>tw||h>th) return null;
    if(w>tw-cx){
      cy+=mh; cx=0; 
      mh=0;
    }
    if(h>th-cy){
      texs.Add(new(new VirtualTexture($"ausp_atlas_{texCounter++}", tw, th, Color.Transparent)));
      cy=0; cx=0; mh=0;
    }
    var te = texs[texs.Count-1];
    Rectangle clipRect = new Rectangle(cx,cy,w,h);
    te.Texture.Texture.SetData(0,clipRect,data,0,data.Length);
    mh=Math.Max(mh,h);
    cx+=w;
    MTexture child = new(te,clipRect);
    contains.Add(uid, child);
    return child;
  }
  public static MTexture MakeLike(this MTexture change, MTexture copy){
    change.Width = copy.Width;
    change.Height = copy.Height;
    change.DrawOffset = copy.DrawOffset;
    change.ScaleFix = copy.ScaleFix;
    change.SetUtil();
    return change;
  }
}
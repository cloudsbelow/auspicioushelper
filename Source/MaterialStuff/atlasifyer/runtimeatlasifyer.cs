


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
  [Command("auspdebug_resetAtlases","Reset the runtime atlases")]
  static void Reset(){
    DebugConsole.Write("Clearing atlases");
    contains.Clear();
    foreach(var tex in texs) tex.Texture.Dispose();
    texs.Clear();
    ShelfAllocator.Reset();
    SkylineAllocator.Reset();
  }
  static Atlasifyer(){
    auspicioushelperModule.OnEnterMap.enroll(new PersistantAction(Reset));
  }
  const int tw=4096;
  const int th=256;
  static int texCounter=0;
  public static void addTex(){
    texs.Add(new(new VirtualTexture($"ausp_atlas_{texCounter++}", tw, th, Color.Transparent)));
    DebugConsole.Write($"ADDED NEW ATLAS TEXTURE {tw} x {th}! There are currently ",texs.Count);
  }
  static class ShelfAllocator{
    static int cy=th+1;
    static int mh=0;
    static int cx=0;
    public static Rectangle GetRect(int w, int h){
      if(w>tw-cx){
        cy+=mh; cx=0; 
        mh=0;
      }
      if(h>th-cy){
        addTex();
        cy=0; cx=0; mh=0;
      }
      Rectangle toRet =  new Rectangle(cx,cy,w,h);
      mh=Math.Max(mh,h);
      cx+=w;
      return toRet;
    }
    public static void Reset(){cy=th+1;}
  }
  static class SkylineAllocator{
    const int blocksize = 1;
    static short[] heights = new short[tw/blocksize];
    public static void Reset(){
      for(int i=0; i<heights.Length; i++) heights[i]=0;
    }
    public static int CalcScore(int s, int running, int max){
      return max*s*3-running*2;
    }
    public static Rectangle GetRect(int w, int h){
      int s = (w+blocksize-1)/blocksize;
      Util.RingDeque<short> indices = new(stackalloc short[Util.HigherPow2(s)]);
      indices.Push(0);
      int runningSum = heights[0];
      for(short i=1; i<s; i++){
        short cur = heights[i];
        runningSum+=cur;
        while(indices.Count>0 && heights[indices.Tail()]<=cur) indices.Pop();
        indices.Push(i);
      }
      int best = h>tw-heights[indices.Head()]?-1:0;
      int score = best==-1?int.MaxValue:CalcScore(s,runningSum,heights[indices.Head()]);
      short curY = heights[indices.Head()];
      for(short hidx=(short)s; hidx<tw/blocksize; hidx++){
        int lidx = hidx-s;
        if(indices.Head()<=lidx)indices.Dequeue();
        short cur = heights[hidx];
        runningSum+=cur-heights[lidx];
        while(indices.Count>0 && heights[indices.Tail()]<=cur) indices.Pop();
        indices.Push(hidx);
        if(h<=tw-heights[indices.Head()]){
          int nscore = CalcScore(s,runningSum,heights[indices.Head()]);
          if(nscore<score){
            score = nscore;
            best = lidx+1;
            curY = heights[indices.Head()];
          }
        }
      }
      if(best==-1 || texs.Count==0){
        addTex();
        Reset();
        return GetRect(w,h);
      }
      for(int i=0; i<s; i++) heights[best+i]=(short)(curY+h);
      return new Rectangle(best*blocksize, curY, w, h);
    }
  }
  public static MTexture PushToAtlas(Color[] data, int w, int h, string uid, out Rectangle clipRect){
    clipRect=Rectangle.Empty;
    if(contains.TryGetValue(uid, out var tex)) return tex;
    if(w>tw||h>th) return null;
    clipRect = SkylineAllocator.GetRect(w,h);

    var te = texs[texs.Count-1];
    te.Texture.Texture.SetData(0,clipRect,data,0,data.Length);
    MTexture child = new(te,clipRect);
    contains.Add(uid, child);
    return child;
  }
  public static MTexture PushToAtlas(Color[] data, int w, int h, string uid)=>PushToAtlas(data,w,h,uid,out var _);
  public static MTexture MakeLike(this MTexture change, MTexture copy){
    change.Width = copy.Width;
    change.Height = copy.Height;
    change.DrawOffset = copy.DrawOffset;
    change.ScaleFix = copy.ScaleFix;
    change.SetUtil();
    return change;
  }
  static int deubgCount=0;
  static Color Darken(this Color c)=>new(c.R/2,c.G/2,c.B/2,c.A);
  static Color[] DarkenBorderInplace(this Color[] dat, int w, int h){
    for(int i=0; i<w; i++){
      dat[i] = dat[i].Darken();
      dat[i+w*h-w] = dat[i+w*h-w].Darken();
    }
    for(int i=0; i<h; i++){
      dat[w*i] = dat[w*i].Darken();
      dat[w*i+w-1] = dat[w*i+w-1].Darken();
    }
    return dat;
  }
  [Command("auspDebug_AddAtlasRect","Adds a rectangle of solid color to the runtime atlas. For debugging.")]
  public static void AddDebug(int width, int height, string color=""){
    if(string.IsNullOrWhiteSpace(color)) color=Calc.Random.Choose("f00","ff0","0f0","0ff","00f", "0a0","000","fff","740","aaa");
    Color c = Util.hexToColor(color);
    var dat = new Color[width*height];
    for(int i=0; i<dat.Length; i++)dat[i]=c;
    PushToAtlas(dat.DarkenBorderInplace(width,height),width,height,$"debugColor_{deubgCount++}", out var r);
    Engine.Commands.Log($"Added new rectangle of color #{c} at cliprect {r}");
  }
  public static void DebugRenderAt(Vector2 pos){
    if(texs.Count>0) texs[texs.Count-1].Draw(pos);
  }
  [Command("auspDebug_AddAtlasRects","Add several rectangles of solid color")]
  public static void AddMany(int num, int maxs = 32, int mins=4){
    for(int i=0; i<num; i++)AddDebug(Calc.Random.Range(mins,maxs),Calc.Random.Range(mins,maxs));
  }
}
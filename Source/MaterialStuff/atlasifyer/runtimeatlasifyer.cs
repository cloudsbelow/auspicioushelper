


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
  [ResetEvents.RunOn(ResetEvents.RunTimes.OnEnter)]
  public static void Reset(){
    if(texs.Count>0)DebugConsole.Write("Clearing atlases");
    contains.Clear();
    foreach(var tex in texs) tex.Texture.Dispose();
    texs.Clear();
    SkylineAllocator.Reset();
  }
  const int itw=512;
  const int ith=256;
  const int MAXDIM = 4096;
  static int curw=0;
  static int curh=0;
  static int texCounter=0;
  public static bool ExpandTex(){
    if(texs.Count==0 || curh==MAXDIM) {
      texs.Add(new(new VirtualTexture($"ausp_atlas_{texCounter++}", curw=itw, curh=ith, Color.Transparent)));
      DebugConsole.Write($"ADDED NEW ATLAS TEXTURE {itw}x{ith}. There are currently ",texs.Count);
      return true;
    }
    if(curh<curw){
      curh = Math.Min(MAXDIM,Math.Min(curh*2,curh+1024));
    } else {
      curw = Math.Min(curw*2,MAXDIM);
    }
    GraphicsDevice gd = auspicioushelperGFX.gd;
    var last = texs[^1];
    var old = last.Texture.Texture;
    Texture2D ntex = last.Texture.Texture = new Texture2D(gd, curw,curh);
    Color[] dat = new Color[old.Width*old.Height];
    old.GetData(dat);
    ntex.SetData(0,new(0,0,old.Width,old.Height),dat,0,dat.Length);
    last.Width = last.Texture.Width = curw;
    last.Height = last.Texture.Height = curh;
    last.ClipRect = new Rectangle(0,0,curw,curh);
    DebugConsole.Write($"Expanding atlas texture to {curw}x{curh}");
    return false;
  }
  static class SkylineAllocator{
    const int blocksize = 4;
    static short[] heights = new short[MAXDIM/blocksize];
    public static void Reset(){
      for(int i=0; i<heights.Length; i++) heights[i]=0;
    }
    public static int CalcScore(int s, int running, int max){
      return max*s+Math.Clamp(max*s-running,0,s);
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
      int best = h>curh-heights[indices.Head()]?-1:0;
      int score = best==-1?int.MaxValue:CalcScore(s,runningSum,heights[indices.Head()]);
      short curY = heights[indices.Head()];
      for(short hidx=(short)s; hidx<curw/blocksize; hidx++){
        int lidx = hidx-s;
        if(indices.Head()<=lidx)indices.Dequeue();
        short cur = heights[hidx];
        runningSum+=cur-heights[lidx];
        while(indices.Count>0 && heights[indices.Tail()]<=cur) indices.Pop();
        indices.Push(hidx);
        if(h<=curh-heights[indices.Head()]){
          int nscore = CalcScore(s,runningSum,heights[indices.Head()]);
          if(nscore<score){
            score = nscore;
            best = lidx+1;
            curY = heights[indices.Head()];
          }
        }
      }
      if(best==-1 || texs.Count==0){
        if(ExpandTex()) Reset();
        return GetRect(w,h);
      }
      for(int i=0; i<s; i++) heights[best+i]=(short)(curY+h);
      return new Rectangle(best*blocksize, curY, w, h);
    }
  }
  public static MTexture PushToAtlas(Color[] data, int w, int h, string uid, out Rectangle clipRect){
    clipRect=Rectangle.Empty;
    if(contains.TryGetValue(uid, out var tex)) return tex;
    
    if(w>itw || h>ith){
      DebugConsole.Write($"Texture {uid} of size {w}x{h} too big to atlas (max allowed size {itw}x{ith}) Using fallback");
      var ntex = new VirtualTexture("CreatedFor_"+uid, w,h, Color.Transparent);
      ntex.Texture.SetData(data);
      tex = new(ntex);
    } else {
      clipRect = SkylineAllocator.GetRect(w,h);
      var te = texs[texs.Count-1];
      te.Texture.Texture.SetData(0,clipRect,data,0,data.Length);
      tex = new(te,clipRect);
    }
    contains.Add(uid, tex);
    return tex;
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
  public static MTexture GetAtlased(this Atlas atlas, string path){
    string fullpath = atlas.DataPath+path;
    if(contains.TryGetValue(fullpath, out var tex)) return tex;
    var m = atlas[path];
    var dat = Util.TexData(m, out var w, out var h);
    return PushToAtlas(dat,w,h,fullpath).MakeLike(m);
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
    if(string.IsNullOrWhiteSpace(color)) color=Calc.Random.Choose("f00","ff0","0f0","0ff","00f", "0a0","fff","740","aaa");
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
  [Command("auspDebug_AtlasSize","Print current atlas size")]
  public static void AtlasSize()=>Engine.Commands.Log($"There are {texs.Count} texutres. The most recent has size {curw}x{curh}");
}
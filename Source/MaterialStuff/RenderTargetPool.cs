



using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public static class RenderTargetPool{
  static List<RenderTarget2D> Pool = new();
  public const int maxCanUse = 128;
  public static Stack<int> available = new();
  static RenderTargetPool(){
    Clear();
    auspicioushelperModule.OnEnterMap.enroll(new ScheduledAction(()=>{
      Clear();
      return false;
    },"RenderTargetPool Clear"));
  }
  public static int currentWidth {get; private set;} = 320;
  public static int currentHeight {get; private set;} = 180;
  public static Vector2 pixelSize=>new Vector2(1/(float)currentWidth,1/(float)currentHeight);
  public static Vector2 size=>new Vector2(currentWidth,currentHeight);
  static int state =0;
  public class RenderTargetHandle {
    int index=-1;
    int instate;
    public static implicit operator RenderTarget2D(RenderTargetHandle h){
      if(h.index>=Pool.Count){
        throw new Exception("Cannot get the rendertarget with this handle");
      }
      if(h.index == -1) throw new Exception("This handle is not in use");
      return Pool[h.index];
    }
    public RenderTargetHandle(bool instantiate = true){
      if(instantiate) Claim();
    }
    public void Free(){
      if(index == -1) return;
      if(state!=instate){
        DebugConsole.Write("Freed a renderTargetHandle from previous thing");
        return;
      }
      available.Push(index);
      index = -1;
    }
    public void Claim(){
      if(index != -1) throw new Exception("Handle already allocated");
      if(available.Count ==0) throw new Exception("Render target pool is empty");
      index = available.Pop();
      instate = state;
      if(index>=Pool.Count){
        Pool.Add(new RenderTarget2D(Engine.Instance.GraphicsDevice,currentWidth,currentHeight));
      } 
    }
  }
  public static int InUse=>maxCanUse-available.Count;
  public static void Resize(int nwidth, int nheight, bool force=false){
    if(nwidth<=0 || nheight<=0 || nwidth>=1920 || nheight>=1080){
      DebugConsole.Write($"why the heck are you putting in these sizes {nwidth} {nheight}");
      return;
    }
    if(!force){
      nwidth = ((nwidth+63)/64)*64;
      nheight = ((nheight+35)/36)*36;
    }
    if(currentHeight == nheight && currentWidth == nwidth) return;
    DebugConsole.Write($"Resizing {Pool.Count} buffers to {nwidth} {nheight}");
    for(int i=0; i<Pool.Count; i++){
      Pool[i] = new RenderTarget2D(Engine.Instance.GraphicsDevice,nwidth,nheight);
    }
    currentWidth = nwidth;
    currentHeight = nheight;
    auspicioushelperGFX.gd.SetRenderTarget(zero);
    auspicioushelperGFX.gd.Clear(Color.Red);
  }
  public static RenderTargetHandle zero;
  public static void Clear(){
    foreach(var v in Pool) v.Dispose();
    Pool.Clear();
    available.Clear();
    for(int i=maxCanUse-1; i>=0; i--){
      available.Push(i);
    }
    state++;
    zero = new RenderTargetHandle(true);
    auspicioushelperGFX.gd.SetRenderTarget(zero);
    auspicioushelperGFX.gd.Clear(Color.Red);
    Resize(320,180,true);
  }
}

using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Xml;

namespace Celeste.Mod.auspicioushelper;
public static class auspicioushelperGFX {
  public static IGraphicsDeviceService graphicsDeviceService;
  public static GraphicsDevice gd{get{
    if (graphicsDeviceService == null)  
      graphicsDeviceService = Engine.Instance.Content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
    return graphicsDeviceService.GraphicsDevice;
  }}

  //from ShaderHelper
  public static VirtualShader LoadShader(string path)=>LoadExternShader("auspicioushelper/"+path);
  public static VirtualShader LoadExternShader(string path){
    return Fill(new VirtualShader(path));
  }
  public static VirtualShader Fill(VirtualShader v){
    //probably not a great method of doing this whatsoever
    //-well I don't even know what this is doing <3
    v.Clear();
    v.cacheNum = auspicioushelperModule.CACHENUM;
    if (graphicsDeviceService == null)  
      graphicsDeviceService = Engine.Instance.Content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
    ModAsset asset = Everest.Content.Get(Util.concatPaths("Effects",v.path)+".cso",true);
    if (asset == null){
      DebugConsole.WriteFailure("Failed to fetch shader at Effects/"+v.path);
    } else try{
      v.shader = new Effect(graphicsDeviceService.GraphicsDevice, asset.Data);
      v.fixBaseparams();
      // try {
      //   ModAsset qasset = Everest.Content.Get(Util.concatPaths("Effects",v.path)+"_quiet.cso",true);
      //   if(qasset!=null) v.quiet = new Effect(graphicsDeviceService.GraphicsDevice, qasset.Data);
      // }catch(Exception err2){
      //   DebugConsole.Write("Failed to load quiet shader Effects/"+v.path+"_quiet with exception "+ err2.ToString());
      // }
    }catch(Exception err){
      DebugConsole.WriteFailure("Failed to load shader Effects/"+v.path+" with exception "+ err.ToString());
    }
    return v;
  }
}
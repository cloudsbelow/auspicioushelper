

using System;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper;

/*
  Paste this class into your mod and add the modInterop thing to use
*/
[ModImportName("MaterialIop")]
public static class MaterialIop{
  // Returns true if any material layer is being used
  static Func<bool> hasActiveLayer;

  // How to render a entity type when in a material layer. Overrides the
  // default, which is to use 'Render'.
  static Action<Type, Action<Entity>> setCustomRenderFn;

  /* 
    A texture provider is an interface for providing textures to materials. 
    Multiple layers can use the same provider. When a layer using the provider
    is enabled when none are previously, Subscribed() is called. Unsubscribed
    is called when the last layer using this provider is disabled. Let's say
    layer A and B use a given provider. This is what would happen in order:

      - Your provider generation function is called when A or B is first loaded,
        and it returns instance I
      - A is enabled -> I.Subscribed is called
      - B is enabled -> nothing happens
      - A is disabled -> nothing happens
      - B is disabled -> I.Unsubscribed is called (no more enabled layers use it)
      - B is enabled -> I.Subscribed called again

    Thus any allocation/deallocation should happen in these functions.

    texture is called every time the texture is used to fill a slot in a layer. Because
    providers can be providing for many active layers (or slots in the same layer), this
    function should ideally return an already-rendered texture instead of doing any work.

    Prepare is analogous to BeforeRender and is called every frame before Level.Render
    when your provider is subscribed to. This is where you should render changes to your 
    texture if needed. If your texture doesn't change, feel free to skip implementing it.
  */
  public interface ITextureProvider{
    public Texture2D texture {get;}
    public void Subscribed();
    public void Unsubscribed();
    public void Prepare(){}
  }

  /*
    Call this function to register a provider provider. 'provider providers' are
    functions that return a provider given a string. Let's say someone uses the
    textures
       1: .YourHelper_Graident(1,2,3),
       2: .YourHelper_Gradient
    If you have created a ProviderProvider with the name 'YourHelper_Gradient'
    your func gets called with string argument "1,2,3" for the first item and
    null for the second. i.e. your provider provider func gets called with the
    string within the parenthesis if it exists.
  */
  public static void CreateProviderProvider(string name, Func<string,ITextureProvider> func){
    if(_setProvider is not {} fn) throw new Exception("Mod interop not set up yet");
    fn(name,func);
  }

  //If you do not plan to use parameters, you can use this function instead.
  public static void CreateProviderProvider(string name, Func<ITextureProvider> func){
    if(_setProvider is not {} fn) throw new Exception("Mod interop not set up yet");
    fn(name,(s)=>func());
  }

  //Do not call this yourself.
  public static Action<string, Func<string,object>> _setProvider;
}



[ModExportName("MaterialIop")]
public static class MaterialIopExp{
  public static void _setProvider(string str, Func<string,object> fn){

  }
  public static bool hasActiveLayer()=>MaterialPipe.hasActiveLayer;
}
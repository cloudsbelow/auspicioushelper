


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomloadEntity(nameof(Search))]
public class ChannelReflecter:Entity{
  static void Search(EntityData d){
    var ident = d.Attr("path");
    Finder.watch(ident,x=>FoundEntity.addIdent(x,ident));
  }
  string ident;
  string channel;
  float ifNull;
  bool logPossible;
  List<string> refl;
  public ChannelReflecter(EntityData d,Vector2 o):base(d.Position+o){
    ident = d.Attr("identifier");
    channel = d.Attr("channel");
    logPossible = d.Bool("logAccessible");
    ifNull = d.Float("valueIfNull",0);
    refl = Util.listparseflat(d.Attr("access",""));
  }
  public override void Update() {
    base.Update();
    if(FoundEntity.find(ident) is not {} e) goto notFound;
    object o=e;
    foreach(var s in refl){
      try{
        o = Util.ReflectGet(o,s,true);
      } catch(Exception){
        o=null;
      }
      if(o==null) goto notFound;
    }
    if(logPossible && o.GetType() is {} t){
      BindingFlags flags = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy;
      DebugConsole.Write($"Type at entity {e.GetType()}: {string.Join('.',refl)} => {o.GetType()}");
      foreach(var f in t.GetFields(flags)) if(!f.IsStatic) {
        DebugConsole.Write($"Field: {f.Name} ({f?.FieldType})");
      }
      foreach(var p in t.GetProperties(flags)) if(!p.GetGetMethod()?.IsStatic??false) {
        DebugConsole.Write($"property: {p.Name} ({p.PropertyType})");
      }
      foreach(var m in t.GetMethods(flags)) if(!m.IsStatic && m.GetParameters().Length==0){
        DebugConsole.Write($"method: {m.Name} ({m.ReturnType})");
      }
      logPossible=false;
    }
    try{
      ChannelState.SetChannel(channel,Convert.ToDouble(o));
      return;
    }catch(Exception ex){
      DebugConsole.Write($"Exception occurred when converting {o} to number:",ex);
    }
    notFound:
      ChannelState.SetChannel(channel,ifNull);
  }
}
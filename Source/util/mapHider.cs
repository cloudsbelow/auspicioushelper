



using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Celeste.Mod.Helpers;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

public static class MapHider{
  static Dictionary<string,bool> cache = new();
  public static bool isHiding = false;
  static List<Tuple<string,Regex>> rules=null;
  static bool check(string assetstr){
    if(!isHiding) return false;
    assetstr=assetstr.ToLower();
    if(cache.TryGetValue(assetstr, out var val)) return val;
    bool flag = false;
    if(rules!=null) foreach(var rule in rules){
      if(rule.Item2.Match(assetstr).Success){
        DebugConsole.Write($"Hiding {assetstr} due to rule {rule.Item1}");
        flag=true;
        break;
      }
    }
    cache[assetstr] = flag;
    return flag;
  }
  public static void setHide(){
    isHiding = true;
    cache.Clear();
    if(auspicioushelperModule.Settings.userHideRules == null) return;
    rules = new List<Tuple<string,Regex>>();
    int i=-1;
    foreach(var rule in auspicioushelperModule.Settings.userHideRules){
      i++;
      if(rule == "") continue;
      try{
        rules.Add(new Tuple<string, Regex>(i.ToString(),new Regex(rule, RegexOptions.IgnoreCase)));
        DebugConsole.Write($"Registered hiding rule {i.ToString()} as {rule}");
      }catch(Exception ex){
        DebugConsole.Write($"your rule {i} was bad - error message {ex}");
      }
    }
    AssetReloadHelper.ReloadAllMaps();
  }
  public static void setUnhide(){
    if(!isHiding) return;
    isHiding=false;
    cache.Clear();
    AssetReloadHelper.ReloadAllMaps();
  }
  public static bool hasReloaded=false;
  public static void handleReload(){
    bool doHide = !hasReloaded;
    hasReloaded = true;
    if(doHide && auspicioushelperModule.Settings.HideHelperMaps)setHide();
  }

  [OnLoad.ILHook(typeof(AreaData),nameof(AreaData.Load))]
  static void LoadHook(ILContext ctx){
    ILCursor c = new(ctx);
    ILLabel targ = null;
    if(c.TryGotoNextBestFit(MoveType.After,
      itr=>itr.MatchBr(out targ),
      itr=>itr.MatchLdloc(9),
      itr=>itr.MatchCallvirt(typeof(IEnumerator<ModAsset>),"get_Current"),
      itr=>itr.MatchStloc(10), itr=>itr.MatchLdloc(10),
      itr=>itr.MatchLdfld<ModAsset>(nameof(ModAsset.PathVirtual)),
      itr=>itr.MatchLdcI4(5), itr=>itr.MatchCallvirt<string>(nameof(string.Substring)),
      itr=>itr.MatchStloc(11)
    )){
      c.EmitLdloc(11);
      c.EmitDelegate(check);
      c.EmitBrtrue(targ);
    } else DebugConsole.Write("Failed to apply map hiding hook");
  }
}
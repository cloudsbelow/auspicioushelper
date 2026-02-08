



using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateTemplate")]
public class TemplateTemplate:Template{
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReset)]
  static Dictionary<string,string> replace = new();
  class WithReplace:IDisposable{
    List<(string,string)> old;
    public WithReplace(Dictionary<string,string> values, bool dontOverride = false){
      old=new(values.Count);
      foreach(var (k,v) in values){
        bool flag = replace.TryGetValue(k,out var o);
        if(flag && dontOverride) continue;
        old.Add(new(k,flag?o:null));
        replace[k]=v;
      }
    }
    void IDisposable.Dispose(){
      foreach(var (k,v) in old){
        if(v==null) replace.Remove(k);
        else replace[k]=v;
      }
    }
  }
  static List<(string,string)> found = new();
  static Regex pattern = new(@"\{\s*(\w+)\s*\}",RegexOptions.Compiled);
  public static EntityData withReplace(EntityData old){
    if(replace.Count==0 || old.Values==null) return old;
    found.Clear();
    foreach(var (k,v) in old.Values) if(v?.ToString() is {}s){
      var ms = pattern.Matches(s);
      if(ms.Count==0) continue;
      string n = "";
      int start=0;
      foreach(Match m in ms){
        n+=s.Substring(start,m.Index-start);
        start = m.Index;
        if(replace.TryGetValue(m.Groups[1].Value, out string r)){
          n+=r;
          start+=m.Length;
        }
      }
      n+=s.Substring(start);
      found.Add(new(k,n));
    }
    if(found.Count==0) return old;
    Dictionary<string,object> nvals = new(old.Values.Count);
    foreach(var (k,v) in old.Values) nvals[k]=v;
    foreach(var (k,n) in found) nvals[k]=n;
    var nd = Util.shallowCopy(old);
    nd.Values=nvals;
    return nd;
  }
  Dictionary<string,string> substitutes = new();
  [ResetEvents.NullOn(ResetEvents.RunTimes.OnEnter)]
  static ulong counter = 1;
  static readonly string baseChars="0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
  public static char randomChar = '\u00B6';
  string randomSub(){
    string str = "";
    ulong i=counter++;
    while(i!=0){
      str+=baseChars[(int)(i%(ulong)baseChars.Length)];
      i = i/(ulong)baseChars.Length;
    }
    return randomChar+str.Length.ToString()+str;
  }
  public TemplateTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateTemplate(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    var str = Util.listparseflat(d.Attr("replacements",""));
    foreach(var item in str){
      var spl = item.Split(':');
      var key = spl[0].Trim();
      if(spl.Length>1){
        var value = spl[1].Trim();
        if(value.StartsWith('"')) value = Util.stripEnclosure(value);
        if(value.StartsWith('@')) value = ChannelState.readChannel(value.Substring(1)).ToString();
        if(value.StartsWith('\\')) value = value.Substring(1);
        substitutes[key]=value;
      } else substitutes[key]=randomSub();
    }
  }
  public override void addTo(Scene scene) {
    using(new WithReplace(substitutes)) base.addTo(scene);
  }
  public static void correctChain(Util.MultiDisposable disp, Template t){
    while(true){
      var n = t.parent?.GetFromTree<TemplateTemplate>();
      if((t=n)==null) return;
      disp.Add(new WithReplace(n.substitutes, true));
    }
  }
}
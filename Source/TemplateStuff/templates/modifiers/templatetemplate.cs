



using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public class TemplateTemplate:Template{
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReset)]
  static Dictionary<string,string> replace = new();
  public class WithReplace:IDisposable{
    List<(string,string)> old;
    public WithReplace(Dictionary<string,string> values){
      old=new(values.Count);
      foreach(var (k,v) in values){
        old.Add(new(k,replace.TryGetValue(k,out var o)?o:null));
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
  Regex pattern = new(@"\{(\w+)\}",RegexOptions.Compiled);
  EntityData withReplace(EntityData old){
    if(replace.Count==0) return old;
    found.Clear();
    foreach(var (k,v) in old.Values) if(v?.ToString() is {}s){
      var ms = pattern.Matches(s);
      if(ms.Count==0) continue;
      string n = "";
      int start=0;
      foreach(Match m in ms){
        n+=s.Substring(start,m.Index);
        if(replace.TryGetValue(m.Groups[0].Value, out string r)){
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
  public TemplateTemplate(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateTemplate(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){

  }
}
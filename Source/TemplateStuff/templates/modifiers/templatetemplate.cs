



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
  static uint counter = 1;
  const string baseChars="0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  static int getCharIdx(char c) =>c switch {
    <='9'=>c-'0',
    _=>c-'A'+10
  };
  public const char randomChar = '\u00B6';
  static string toEncoding(uint num){
    string enc = "";
    while(num!=0){
      enc+=baseChars[(int)(num&31)];
      num>>=5;
    }
    return randomChar+(baseChars[enc.Length]+enc);
  }
  static bool tryDecode(string s, int idx, out uint num){
    int len = getCharIdx(s[idx+1]);
    num = 0;
    if(idx+len+1>=s.Length) return false;
    for(int j=len-1; j>=0; j--){
      num<<=5;
      num+=(uint)getCharIdx(s[idx+j+2]);
    }
    return true;
  }
  List<uint> owned = new();
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnReload)]
  [Import.SpeedrunToolIop.Static]
  static Dictionary<uint,TemplateTemplate> owns = new();

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
      } else {
        uint num = counter++;
        owned.Add(num);
        substitutes[key] = toEncoding(num);
      }
    }
  }
  public override void addTo(Scene scene) {
    foreach(var o in owned) owns.Add(o,this);
    using(new WithReplace(substitutes)) base.addTo(scene);
  }
  public static void correctChain(Util.MultiDisposable disp, Template t){
    while(true){
      var n = t.parent?.GetFromTree<TemplateTemplate>();
      if((t=n)==null) return;
      disp.Add(new WithReplace(n.substitutes, true));
    }
  }

  public enum Loc{
    Flag,Counter,Channel
  }
  HashSet<(string,Loc)> ownedStrings = new(); 
  public override void Removed(Scene scene) {
    Session session = (scene as Level).Session;
    base.Removed(scene);
    foreach(var o in owned) if(owns.GetValueOrDefault(o) is {} other && other!=this){
      DebugConsole.WriteFailure($"Weirdness at index {o}",true);
    }
    foreach(var o in owned) owns.Remove(o);
    ChannelState.lockCross = true;
    foreach(var (s,l) in ownedStrings) switch(l){
      case Loc.Flag: session.SetFlag(s,false); break;
      case Loc.Counter: session.Counters.RemoveAll(x=>x.Key==s); break;
      case Loc.Channel: ChannelState.ForceRemove(s); break;
    }
    ChannelState.lockCross = false;
  }
  public static void addBlame(string s, Loc l){
    for(int i=0; i<s.Length-1; i++) if(s[i]==randomChar){
      if(tryDecode(s,i,out var n) && owns.TryGetValue(n, out var t)){
        t.ownedStrings.Add((s,l));
      } else DebugConsole.Write($"Encoding in {l} {s} at index {i} did not resolve ({n})");
    }
  }
}
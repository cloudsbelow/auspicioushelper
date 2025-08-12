


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Celeste.Mod.auspicioushelper;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
public static class ChannelState{
  enum Ops{
    none, not, lnot, xor, and, or, add, sub, mult, div, mrecip, mod, safemod, 
    min, max, ge, le, gt, lt, eq, ne,rshift, lshift, shiftr, shiftl
  }
  struct Modifier{
    int y;
    Ops op;
    Regex prefixSuffix = new Regex("^\\s*(-|[^-\\d]+)([-\\d]*)\\s*");
    public Modifier(string s, out bool success){
      Match m = prefixSuffix.Match(s);
      int.TryParse(m.Groups[2].ToString(),out y);
      success = true;
      switch(m.Groups[1].ToString()){
        case "~":op=Ops.not; break;
        case "!":op=Ops.lnot; break;
        case "^":op=Ops.xor; break;
        case "&":op=Ops.and; break;
        case "|":op=Ops.or; break;
        case "+":op=Ops.add; break;
        case "-":op=Ops.sub; break;
        case "*":op=Ops.mult; break;
        case "/":op=Ops.div; break;
        case "recip": case "d": case "x/":op=Ops.mrecip; break;
        case "%":op=Ops.mod; break;
        case "%s": case "r": op=Ops.safemod; break;
        case "min":op=Ops.min; break;
        case "max":op=Ops.max; break;
        case ">":op=Ops.gt; break;
        case "<":op=Ops.lt; break;
        case ">=":op=Ops.ge; break;
        case "<=":op=Ops.le; break;
        case "==":op=Ops.eq; break;
        case "!=":op=Ops.ne; break;
        case "<<":op=Ops.lshift; break;
        case ">>":op=Ops.rshift; break;
        case "x<<":op=Ops.shiftl; break;
        case "x>>":op=Ops.shiftr; break;
        default: success = false; break;
      }
      if(!success && s.Length>0){
        DebugConsole.WriteFailure($"Improper modifier {s} - parsed as op {m} and val {y}");
      }
    }
    public int apply(int x){
      switch(op){
        case Ops.not: return ~x;
        case Ops.lnot: return x==0?1:0;
        case Ops.xor: return x^y;
        case Ops.and: return x&y;
        case Ops.or: return x|y;
        case Ops.add: return x+y;
        case Ops.sub: return x-y;
        case Ops.mult: return x*y;
        case Ops.div: return x/y;
        case Ops.mrecip: return y/x;
        case Ops.mod: return x%y;
        case Ops.safemod: return ((x%y)+y)%y;
        case Ops.min: return Math.Min(x,y);
        case Ops.max: return Math.Max(x,y);
        case Ops.ge: return x>=y?1:0;
        case Ops.le: return x<=y?1:0;
        case Ops.gt: return x>y?1:0;
        case Ops.lt: return x<y?1:0;
        case Ops.eq: return x==y?1:0;
        case Ops.ne: return x!=y?1:0;
        case Ops.lshift: return x<<y;
        case Ops.rshift: return x>>y;
        case Ops.shiftl: return y<<x;
        case Ops.shiftr: return y>>x;
        default: return x;
      }
    }
  }
  class ModifierDesc{
    public string outname;
    List<Modifier> ops = new List<Modifier>();
    public ModifierDesc(string nch){
      outname = nch;
      int idx=0;
      for(;idx<nch.Length;idx++) if(nch[idx]=='[')break;
      string stuff = nch.Substring(idx+1, nch.Length-idx-2);
      foreach(string sub in stuff.Split(",")){
        var m = new Modifier(sub, out var success);
        if(success)ops.Add(m);
      }
    }
    public int apply(int val){
      foreach(var op in ops) val = op.apply(val);
      return val;
    }
  }

  static Dictionary<string, List<ModifierDesc>> modifiers = new Dictionary<string, List<ModifierDesc>>();
  private static Dictionary<string, int> channelStates = new Dictionary<string, int>();
  private static Dictionary<string, List<IChannelUser>> watching = new Dictionary<string, List<IChannelUser>>();
  public static int readChannel(string ch){
    if(channelStates.TryGetValue(ch, out var v)) return v;
    else return addModifier(ch);
  }
  static void SetChannelRaw(string ch, int state){
    if(readChannel(ch) == state) return;
    channelStates[ch] = state;
    if (watching.TryGetValue(ch, out var list)) {
      foreach(IChannelUser b in list){
        b.setChVal(state);
      }
    }
  }
  public static void SetChannel(string ch, int state, bool fromInterop=false){
    int idx=0;
    for(;idx<ch.Length;idx++) if(ch[idx]=='[')break;
    if(idx!=ch.Length) return;
    SetChannelRaw(ch,state);
    if(!fromInterop){
      if(ch.Length>0&&ch[0]=='$')(Engine.Instance.scene as Level)?.Session.SetFlag(ch.Substring(1),state!=0);
      if(ch.Length>0&&ch[0]=='#')(Engine.Instance.scene as Level)?.Session.SetCounter(ch.Substring(1),state);
    }
    if(modifiers.TryGetValue(ch, out var ms)){
      foreach(var m in ms){
        SetChannelRaw(m.outname,m.apply(state));
      }
    }
  }
  public static void unwatchNow(IChannelUser b){
    if (watching.TryGetValue(b.channel, out var list)) {
      list.Remove(b);
    }
  }
  public static int watch(IChannelUser b){
    if(b.channel == null) return 0;
    if (!watching.TryGetValue(b.channel, out var list)) {
      list = new List<IChannelUser>();
      watching[b.channel] = list;
    }
    list.Add(b);
    return readChannel(b.channel);
  }
  static void clearModifiers(HashSet<string> except = null){
    Dictionary<string, List<ModifierDesc>> nlist = new Dictionary<string, List<ModifierDesc>>();
    foreach(var pair in modifiers){
      List<ModifierDesc> keep = new List<ModifierDesc>();
      foreach(var mod in pair.Value){
        if(except == null || !except.Contains(mod.outname))channelStates.Remove(mod.outname);
        else keep.Add(mod);
      }
      if(keep.Count>0) nlist[pair.Key] = keep;
    }
    HashSet<string> toRemove = new();
    foreach(var pair in channelStates){
      if(except!=null && except.Contains(pair.Key)) continue;
      int idx=0;
      for(;idx<pair.Key.Length;idx++) if(pair.Key[idx]=='[')break;
      string clean = pair.Key.Substring(0,idx);
      if(clean != pair.Key) toRemove.Add(pair.Key);
    }
    foreach(var s in toRemove) channelStates.Remove(s);
    modifiers = nlist;
  }
  public static void unwatchAll(){
    watching.Clear();
    clearModifiers();
  }
  static int addModifier(string ch){
    int idx=0;
    for(;idx<ch.Length;idx++) if(ch[idx]=='[')break;
    string clean = ch.Substring(0,idx);

    if(ch.Length>0 && clean!=ch && ch[ch.Length-1] == ']'){
      if(!modifiers.TryGetValue(clean,out var mods)){
        modifiers.Add(clean, mods = new List<ModifierDesc>());
      }
      ModifierDesc mod = new ModifierDesc(ch);
      mods.Add(mod);
      return channelStates[ch] = mod.apply(readChannel(clean));
    } else if(ch.Length>0 && (ch[0]=='@'||ch[0]=='#')){
      if(ch[0]=='#') channelStates[ch]=(Engine.Instance.scene as Level)?.Session.GetCounter(ch.Substring(1))??0;
      if(ch[0]=='$') channelStates[ch]=((Engine.Instance.scene as Level)?.Session.GetFlag(ch.Substring(1))??false)?1:0;
    } else {
      channelStates.TryAdd(ch,0);
    }
    return channelStates[ch];
  }
  public static void unwatchTemporary(){
    clearModifiers();
    List<string> toRemove = new List<string>();
    foreach(var pair in watching){
      var newlist = new List<IChannelUser>();
      foreach(IChannelUser e in pair.Value){
        if(e is Entity en && en.TagCheck(Tags.Persistent)){
          newlist.Add(e);
        }
      }
      if(newlist.Count>0){
        watching[pair.Key] = newlist;
        addModifier(pair.Key);
      }
      else toRemove.Add(pair.Key);
    }
    foreach(var ch in toRemove) watching.Remove(ch);
  }
  public static void clearChannels(string prefix = ""){
    int idx=0;
    for(;idx<prefix.Length;idx++) if(prefix[idx]=='[')break;
    prefix = prefix.Substring(0,idx);
    List<string> toremove = new();
    foreach(var pair in channelStates){
      if(prefix == "" || pair.Key.StartsWith(prefix)) toremove.Add(pair.Key);
    }
    foreach(string s in toremove){
      channelStates.Remove(s);
      modifiers.Remove(s);
    }
  }
  public static Dictionary<string,int> save(){
    Dictionary<string,int> s=new();
    foreach(var pair in channelStates){
      int idx=0;
      if(pair.Key.Length>=1 && (pair.Key[0]=='$'||pair.Key[0]=='$'))continue;
      for(;idx<pair.Key.Length;idx++) if(pair.Key[idx]=='[')break;
      string clean = pair.Key.Substring(0,idx);
      if(pair.Key == clean) s.Add(pair.Key,pair.Value);
    }
    return s;
  }
  public static void load(Dictionary<string,int> s){
    clearChannels();
    unwatchAll();
    foreach(var pair in s) channelStates[pair.Key] = pair.Value;
  }
  static void Hook(On.Celeste.Session.orig_SetFlag orig, Session s, string f, bool v){
    orig(s,f,v);
    if(channelStates.ContainsKey('$'+f)) SetChannel('$'+f,v?1:0,true);
  }
  static bool Hook(On.Celeste.Session.orig_GetFlag orig, Session s, string f){
    if(f.Length==0 || f[0]!='@' || s.Flags.Contains(f)) return orig(s,f);
    return readChannel(f.Substring(1))!=0;
  }
  static void Hook(On.Celeste.Session.orig_SetCounter orig, Session s, string f, int n){
    orig(s,f,n);
    if(channelStates.ContainsKey('#'+f)) SetChannel('#'+f,n,true);
  }
  static int Hook(On.Celeste.Session.orig_GetCounter orig, Session s, string f){
    if(f.Length==0 || f[0]!='@') return orig(s,f);
    foreach(var c in s.Counters) if(c.Key==f) return c.Value;
    return readChannel(f.Substring(1));
  }
  internal static HookManager hooks = new HookManager(()=>{
    On.Celeste.Session.GetFlag+=Hook;
    On.Celeste.Session.GetCounter+=Hook;
    On.Celeste.Session.SetFlag+=Hook;
    On.Celeste.Session.SetCounter+=Hook;
  },()=>{
    On.Celeste.Session.GetFlag-=Hook;
    On.Celeste.Session.GetCounter-=Hook;
    On.Celeste.Session.SetFlag-=Hook;
    On.Celeste.Session.SetCounter-=Hook;
  });
  public static void writeAll(){
    DebugConsole.Write("");
    DebugConsole.Write("===CHANNEL STATE===");
    foreach(var pair in channelStates){
      DebugConsole.Write($"{pair.Key} {pair.Value}");
    }
    DebugConsole.Write("===================");
  } 
}
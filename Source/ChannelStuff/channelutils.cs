


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Celeste.Mod.auspicioushelper;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
public static class ChannelState{
  struct Modifier{
      enum Ops{
      none, not, lnot, xor, and, or, add, sub, mult, div, mrecip, mod, safemod, 
      min, max, ge, le, gt, lt, eq, ne,rshift, lshift, shiftr, shiftl
    }
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
    public int outval;
    public ModifierDesc(string ch){
      outname = ch;
      for(int i=ch.Length-1; i>=0; i--) if(ch[i]=='['){
        string b = ch.Substring(0,i);
        string stuff = ch.Substring(i+1,ch.Length-i-2);
        foreach(string sub in stuff.Split(",")){
          var m = new Modifier(sub, out var success);
          if(success)ops.Add(m);
        }
        if(!deps.TryGetValue(b, out var dep)) deps.Add(b,dep = new());
        dep.mods.Add(this);
        SetChannelRaw(outname, apply(readChannel(b)));
        return;
      }
      DebugConsole.WriteFailure($"Channel {ch} ends in ] but contains no [",true);
    }
    public int apply(int val){
      foreach(var op in ops) val = op.apply(val);
      return outval = val;
    }
    public void Update(int nval){
      int oldval = outval;
      outval = apply(nval);
      if(oldval!=outval)SetChannelRaw(outname,outval);
    }
    public void Remove()=>ChannelState.ForceRemove(outname);
  }
  struct CalcAccessor{
    public InlineCalc calc;
    public int index;
  }
  class InlineCalc{
    enum Ops{
      and, or, sum, invalid
    }
    public string to;
    List<string> from = new();
    List<int> vals = new();
    public int outval;
    Func<int, int, int> pred;
    static Func<int, int, int> andPred = (a,b)=>(a!=0&&b!=0)?1:0;
    static Func<int, int, int> sumPred = (a,b)=>a+b;
    static Func<int, int, int> orPred = (a,b)=>(a!=0||b!=0)?1:0;
    int seedval;
    static Regex termReg = new Regex(@"^[^\(]*",RegexOptions.Compiled);
    public InlineCalc(string expr){
      to=expr;
      string term = termReg.Match(expr).Value;
      if(!Enum.TryParse<Ops>(term, out var op)){
        DebugConsole.WriteFailure($"Invalid function {op} in {expr}");
        op = Ops.and;
      } 
      pred = op switch {Ops.and=>andPred, Ops.or=>orPred, Ops.sum=>sumPred, _=>andPred};
      seedval = op switch {Ops.and=>1, _=>0};
      from = Util.listparseflat(expr.Substring(term.Length),true,false);
      int i=0;
      foreach(var ch in from){
        vals.Add(readChannel(ch));
        if(!deps.TryGetValue(ch, out var dep)) deps.Add(ch,dep = new());
        dep.calcs.Add(new(){calc=this,index=i++});
      }
      outval = vals.Aggregate(seedval,pred);
      SetChannelRaw(to,outval);
    }
    public void Update(int idx, int val){
      vals[idx]=val;
      int old = outval;
      outval = vals.Aggregate(seedval,pred);
      if(old!=outval) SetChannelRaw(to,outval);
    }
    public void Remove(){
      channelStates.Remove(to);
      foreach(var ch in from){
        if(deps.TryGetValue(ch, out var dep)){
          List<CalcAccessor> nlist = new();
          foreach(var d in dep.calcs) if(d.calc!=this)nlist.Add(d);
          dep.calcs = nlist;
        }
      }
      ChannelState.ForceRemove(to);
    }
  }


  private static Dictionary<string, int> channelStates = new Dictionary<string, int>();
  private static Dictionary<string, List<IChannelUser>> watching = new Dictionary<string, List<IChannelUser>>();
  class Deps{
    public List<ModifierDesc> mods = new();
    public List<CalcAccessor> calcs = new();
    public void Update(int nstate){
      foreach(var mod in mods) mod.Update(nstate);
      foreach(var c in calcs) c.calc.Update(c.index,nstate);
    }
  }
  private static Dictionary<string, Deps> deps = new();

  public static int readChannel(string ch){
    ch=Util.removeWhitespace(ch);
    if(channelStates.TryGetValue(ch, out var v)) return v;
    else return addModifier(ch);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool checkClean(string ch){
    int idx=0;
    for(;idx<ch.Length;idx++) if(ch[idx]=='[') return false;
    return true;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static string getClean(string ch){
    int idx=0;
    for(;idx<ch.Length;idx++) if(ch[idx]=='[')break;
    return ch.Substring(0,idx);
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
    ch=Util.removeWhitespace(ch);
    if(!checkClean(ch)) return;
    SetChannelRaw(ch,state);
    if(!fromInterop && ch.Length>0){
      if(ch[0]=='$')(Engine.Instance.scene as Level)?.Session.SetFlag(ch.Substring(1),state!=0);
      if(ch[0]=='#')(Engine.Instance.scene as Level)?.Session.SetCounter(ch.Substring(1),state);
    }
    if(deps.TryGetValue(ch, out var ms))ms.Update(state);
  }
  public static void unwatchNow(IChannelUser b){
    if (watching.TryGetValue(Util.removeWhitespace(b.channel), out var list)) {
      list.Remove(b);
    }
  }
  public static int watch(IChannelUser b){
    if(b.channel == null) return 0;
    string ch = Util.removeWhitespace(b.channel);
    if (!watching.TryGetValue(ch, out var list)) {
      list = new List<IChannelUser>();
      watching[ch] = list;
    }
    list.Add(b);
    return readChannel(b.channel);
  }
  static void clearModifiers(){
    HashSet<string> toRemove = new();
    foreach(var pair in channelStates){
      if(!checkClean(pair.Key)) toRemove.Add(pair.Key);
    }
    foreach(var s in toRemove) channelStates.Remove(s);
    deps.Clear();
  }
  public static void unwatchAll(){
    watching.Clear();
    clearModifiers();
  }
  [MethodImpl(MethodImplOptions.NoInlining)]
  static int addModifier(string ch){
    if(!checkClean(ch)){
      if(ch[^1]==']') return new ModifierDesc(ch).outval;
      if(ch[^1]==')') return new InlineCalc(ch).outval;
      DebugConsole.WriteFailure($"{ch} contains '(' or '[' but doesn't end with one!",true);
    } else if(ch.Length>0 && (ch[0]=='$'||ch[0]=='#')){
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
        Entity en = (e as Entity)??((e as ChannelTracker)?.Entity);
        if(en!=null && (en.TagCheck(Tags.Persistent) || en.TagCheck(Tags.Global))){
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
  public static void ForceRemove(string ch){

  }
  public static void clearChannels(string prefix = ""){
    prefix = Util.removeWhitespace(prefix);
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
    foreach(var pair in s){
      channelStates[pair.Key] = pair.Value;
    }
  }
  static void Hook(On.Celeste.Session.orig_SetFlag orig, Session s, string f, bool v){
    orig(s,f,v);
    if(channelStates.ContainsKey('$'+f)) SetChannel('$'+f,v?1:0,true);
  }
  static bool Hook(On.Celeste.Session.orig_GetFlag orig, Session s, string f){
    if(string.IsNullOrEmpty(f) || f[0]!='@' || (s?.Flags?.Contains(f)??false)) return orig(s,f);
    return readChannel(f.Substring(1))!=0;
  }
  static void Hook(On.Celeste.Session.orig_SetCounter orig, Session s, string f, int n){
    orig(s,f,n);
    if(channelStates.ContainsKey('#'+f)) SetChannel('#'+f,n,true);
  }
  static int Hook(On.Celeste.Session.orig_GetCounter orig, Session s, string f){
    if(string.IsNullOrEmpty(f) || f[0]!='@') return orig(s,f);
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
  [Command("ausp_setChannel","Set a channel")]
  public static void SetChCommand(string channel, int value){
    int oldval = readChannel(channel);
    SetChannel(channel, value);
    Engine.Commands.Log($"Set channel {channel} from {oldval} to {value}");
  }
  [Command("ausp_readChannel","Read a channel")]
  public static void ReadChCommand(string channel){
    Engine.Commands.Log($"Channel {channel} has value {readChannel(channel)}");
  }
  [Command("ausp_dumpChannels","Print all channels")]
  public static void DumpChannels(){
    Engine.Commands.Log("");
    Engine.Commands.Log("===CHANNEL STATE===");
    foreach(var pair in channelStates){
      Engine.Commands.Log($"{pair.Key} {pair.Value}");
    }
    Engine.Commands.Log("===================");
  }
}
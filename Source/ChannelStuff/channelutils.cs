


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
      min, max, ge, le, gt, lt, eq, ne,rshift, lshift, shiftr, shiftl, abs
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
        case "abs":op=Ops.abs; break;
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
        case Ops.abs: return Math.Abs(x);
        default: return x;
      }
    }
  }
  class ModifierDesc{
    public string outname;
    List<Modifier> ops = new List<Modifier>();
    public int outval;
    string from;
    public ModifierDesc(string ch){
      outname = ch;
      for(int i=ch.Length-1; i>=0; i--) if(ch[i]=='['){
        from = ch.Substring(0,i);
        string stuff = ch.Substring(i+1,ch.Length-i-2);
        foreach(string sub in stuff.Split(",")){
          var m = new Modifier(sub, out var success);
          if(success)ops.Add(m);
        }
        if(!deps.TryGetValue(from, out var dep)) deps.Add(from,dep = new());
        dep.mods.Add(this);
        channelStates.Add(outname, apply(_readChannel(from)));
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
    public void Remove(){
      if(deps.TryGetValue(from, out var dep))dep.mods.Remove(this);
      ForceRemove(outname);
    }
  }
  struct CalcAccessor{
    public InlineCalc calc;
    public int index;
  }
  class InlineCalc{
    enum Ops{
      and, or, sum, xor, prod, invalid
    }
    public string to;
    List<string> from = new();
    List<int> vals = new();
    public int outval;
    Func<int, int, int> pred;
    static Func<int, int, int> andPred = (a,b)=>(a!=0&&b!=0)?1:0;
    static Func<int, int, int> sumPred = (a,b)=>a+b;
    static Func<int, int, int> orPred = (a,b)=>(a!=0||b!=0)?1:0;
    static Func<int, int, int> xorPred = (a,b)=>a^b;
    static Func<int, int, int> prodPred = (a,b)=>a*b;
    int seedval;
    static Regex termReg = new Regex(@"^[^\(]*",RegexOptions.Compiled);
    public InlineCalc(string expr){
      to=expr;
      string term = termReg.Match(expr).Value;
      if(!Enum.TryParse<Ops>(term, out var op)){
        DebugConsole.WriteFailure($"Invalid function {op} in {expr}");
        op = Ops.and;
      } 
      pred = op switch {Ops.and=>andPred, Ops.or=>orPred, Ops.sum=>sumPred, Ops.xor=>xorPred, Ops.prod=>prodPred, _=>andPred};
      seedval = op switch {Ops.and=>1, Ops.prod=>1, _=>0};
      from = Util.listparseflat(expr.Substring(term.Length),true,false);
      int i=0;
      foreach(var ch in from){
        vals.Add(_readChannel(ch));
        if(!deps.TryGetValue(ch, out var dep)) deps.Add(ch,dep = new());
        dep.calcs.Add(new(){calc=this,index=i++});
      }
      outval = vals.Aggregate(seedval,pred);
      channelStates.Add(to,outval);
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
      ForceRemove(to);
    }
  }


  class Deps{
    public List<ModifierDesc> mods = new();
    public List<CalcAccessor> calcs = new();
    public void Update(int nstate){
      foreach(var mod in mods) mod.Update(nstate);
      foreach(var c in calcs) c.calc.Update(c.index,nstate);
    }
    public void Remove(){
      var l1 = mods;
      var l2 = calcs;
      mods=new();
      calcs=new();
      foreach(var li in l1) li.Remove();
      foreach(var li in l2) li.calc.Remove();
    }
  }
  [Import.SpeedrunToolIop.Static]
  private static Dictionary<string, Deps> deps = new();
  [Import.SpeedrunToolIop.Static]
  private static Dictionary<string, int> channelStates = new Dictionary<string, int>();
  [Import.SpeedrunToolIop.Static]
  private static Dictionary<string, ChannelTracker.ChannelTrackerList> watching = new();

  public static int readChannel(string ch)=>_readChannel(Util.removeWhitespace(ch));
  private static int _readChannel(string ch){
    if(channelStates.TryGetValue(ch, out var v)) return v;
    else return addModifier(ch);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool checkClean(string ch){
    int idx=0;
    for(;idx<ch.Length;idx++) if(ch[idx]=='[' || ch[idx]=='(') return false;
    return true;
  }
  static void SetChannelRaw(string ch, int state){
    if(_readChannel(ch)==state) return;
    channelStates[ch] = state;
    if (watching.TryGetValue(ch, out var list)) list.Apply(state);
    if(deps.TryGetValue(ch, out var ms))ms.Update(state);
  }
  public static void SetChannel(string ch, int state, bool fromInterop=false){
    ch=Util.removeWhitespace(ch);
    if(ch.Length == 0 || !checkClean(ch)) return;
    SetChannelRaw(ch,state);
    if(!fromInterop && ch.Length>0){
      if(ch[0]=='$')(Engine.Instance.scene as Level)?.Session.SetFlag(ch.Substring(1),state!=0);
      if(ch[0]=='#')(Engine.Instance.scene as Level)?.Session.SetCounter(ch.Substring(1),state);
    }
  }
  public static int watch(ChannelTracker b){
    if(b.channel == null) return 0;
    string ch = Util.removeWhitespace(b.channel);
    if (!watching.TryGetValue(ch, out var list)) {
      list = new();
      watching[ch] = list;
    }
    list.Add(b); 
    return _readChannel(ch);
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
      DebugConsole.Write($"{ch} contains '(' or '[' but doesn't end with one!");
      return 0;
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
      if(pair.Value.RemoveTemp())addModifier(pair.Key);
      else toRemove.Add(pair.Key);
    }
    foreach(var ch in toRemove) watching.Remove(ch);
  }
  static Queue<string> ToForceRemove = new();
  static bool removing;
  public static void ForceRemove(string ch = null){
    if(ch!=null)ToForceRemove.Enqueue(ch);
    if(removing) return;
    removing = true;
    while(ToForceRemove.Count>0){
      HashSet<Deps> d = new();
      while(ToForceRemove.TryDequeue(out var res)){
        if(deps.TryGetValue(res, out var dep)){
          d.Add(dep);
          deps.Remove(res);
        }
        channelStates.Remove(res);
        watching.Remove(res);
      } 
      foreach(var dep in d) dep.Remove();
    }
    removing = false;
  }
  public static void clearChannels(string prefix = ""){
    prefix = Util.removeWhitespace(prefix);
    if(string.IsNullOrEmpty(prefix)){
      unwatchAll();
      deps.Clear();
      channelStates.Clear();
    }
    foreach(var pair in channelStates){
      if(pair.Key.StartsWith(prefix)) ToForceRemove.Enqueue(pair.Key);
    }
    ForceRemove(null);
  }

  public class AdvancedSetter{
    Dictionary<string, Tuple<string, int>> toDo = new();
    public AdvancedSetter(string str){
      foreach(var v in Util.kvparseflat(str)){
        if(v.Value.Length == 0) DebugConsole.WriteFailure($"No set parameter for {v.Key}",true);
        Tuple<string,int> n=null;
        if(v.Value[0]=='@') n = new(v.Value.Substring(1),0);
        else if(int.TryParse(v.Value, out var ival)) n=new(null,ival);
        else DebugConsole.WriteFailure("No operation defined for thing");
        if(!toDo.TryAdd(v.Key,n)) DebugConsole.WriteFailure("Duplicate key; forbidden");
      }
    }
    public void Apply(){
      foreach(var v in toDo) SetChannel(v.Key,v.Value.Item1 is {} str? readChannel(str) : v.Value.Item2);
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
  [Command("ausp_clearChannel","Remove a channel and all dependencies")]
  public static void ClearChCommand(string channel){
    ForceRemove(channel);
    Engine.Commands.Log($"Cleared {channel}");
  }
  [Command("auspDebug_numWatched", "Print number of watched things per channel")]
  public static void CountDebCmd(){
    Engine.Commands.Log("");
    Engine.Commands.Log("===CHANNEL COUNTS===");
    foreach(var pair in watching){
      Engine.Commands.Log($"{pair.Key} {pair.Value.Count}");
    }
    Engine.Commands.Log("===================");
  }
}
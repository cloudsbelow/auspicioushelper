


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.auspicioushelper.Import;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
public static class ChannelState{
  internal struct Modifier{
      enum Ops{
      none, not, lnot, xor, and, or, add, sub, mult, idiv, fdiv, mrecip, mod, safemod, 
      min, max, ge, le, gt, lt, eq, ne,rshift, lshift, shiftr, shiftl, abs
    }
    double y;
    Ops op;
    Regex prefixSuffix = new Regex(@"^\s*(-|[^-\d]+)([-\d\.]*)\s*");
    public Modifier(string s, out bool success){
      Match m = prefixSuffix.Match(s);
      double.TryParse(m.Groups[2].ToString(),out y);
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
        case "/":op=m.Groups[2].ToString().Contains('.')?Ops.fdiv:Ops.idiv; break;
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
    public double apply(double x){
      switch(op){
        case Ops.not: return ~(int)x;
        case Ops.lnot: return x==0?1:0;
        case Ops.xor: return (int)x^(int)y;
        case Ops.and: return (int)x&(int)y;
        case Ops.or: return (int)x|(int)y;
        case Ops.add: return x+y;
        case Ops.sub: return x-y;
        case Ops.mult: return x*y;
        case Ops.fdiv: return x/y;
        case Ops.idiv: return (int)x/(int)y;
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
        case Ops.lshift: return (int)x<<(int)y;
        case Ops.rshift: return (int)x>>(int)y;
        case Ops.shiftl: return (int)y<<(int)x;
        case Ops.shiftr: return (int)y>>(int)x;
        case Ops.abs: return Math.Abs(x);
        default: return x;
      }
    }
  }
  internal class ModifierDesc{
    public string outname;
    List<Modifier> ops = new List<Modifier>();
    public double outval;
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
        var val = _getVal(from);
        (val.mods??=new()).Add(this);
        apply(val.val);
        return;
      }
      DebugConsole.WriteFailure($"Channel {ch} ends in ] but contains no [",true);
    }
    public double apply(double val){
      foreach(var op in ops) val = op.apply(val);
      return outval = val;
    }
    public void Update(double nval){
      double oldval = outval;
      outval = apply(nval);
      if(oldval!=outval)SetChannelRaw(outname,outval);
    }
    public void Remove(){
      if(state.TryGetValue(from, out var dep) && dep.mods is {} m) m.Remove(this);
      ForceRemove(outname);
    }
  }
  internal struct CalcAccessor{
    public InlineCalc calc;
    public int index;
  }
  internal class InlineCalc{
    enum Ops{
      and, or, sum, xor, prod,max,min, invalid
    }
    public string to;
    List<string> from = new();
    double[] vals;
    public double outval;
    Func<double, double, double> pred = null;
    Func<double[], double> func = null;
    double seedval;
    static Regex termReg = new Regex(@"^[^\(]*",RegexOptions.Compiled);
    public InlineCalc(string expr){
      to=expr;
      string term = termReg.Match(expr).Value;
      if(term==""){
        try{
          func = channelmath.Parser.ParseToFunc(expr.Substring(1,expr.Length-2),out from);
        }catch(Exception e){
          Logger.Error(nameof(auspicioushelper),"Making channel failed for reason "+e.Message);
          DebugConsole.Write("Failed to parse custom channel expression",expr.Substring(1,expr.Length-2),e);
        }
      } else {
        if(!Enum.TryParse<Ops>(term, out var op)){
          DebugConsole.WriteFailure($"Invalid function {op} in {expr}");
          op = Ops.and;
        } 
        pred = op switch {
          Ops.and=> static (a,b)=>(a!=0&&b!=0)?1:0, 
          Ops.or=> static (a,b)=>(a!=0||b!=0)?1:0, 
          Ops.sum=> static (a,b)=>a+b,
          Ops.xor=> static (a,b)=>(int)a^(int)b, 
          Ops.prod=> static (a,b)=>a*b, 
          Ops.max=>Math.Max,
          Ops.min=>Math.Min,
          _=>(a,b)=>(a!=0&&b!=0)?1:0 //and is default
        };
        seedval = op switch {
          Ops.and=>1, Ops.prod=>1, 
          Ops.max=>float.NegativeInfinity,
          Ops.min=>float.PositiveInfinity,
          _=>0
        };
        from = Util.listparseflat(expr.Substring(term.Length),true,false);
      }
      int i=0;
      vals = new double[from.Count];
      foreach(var ch in from){
        var val = _getVal(ch);
        vals[i] = val.val;
        (val.calcs??=new()).Add(new(){calc=this,index=i++});
      }
      calcValue();
    }
    void calcValue(){
      if(func==null) outval = vals.Aggregate(seedval,pred);
      else outval = func(vals);
    }
    public void Update(int idx, double val){
      vals[idx]=val;
      double old = outval;
      calcValue();
      if(old!=outval) SetChannelRaw(to,outval);
    }
    public void Remove(){
      foreach(var ch in from) if(state.TryGetValue(ch, out var dep) && dep.calcs is {} c){
        c.RemoveAll(x=>x.calc==this);
      }
      ForceRemove(to);
    }
  }

  internal class ChannelVal{
    public double val {get; private set;}
    public List<ModifierDesc> mods = null;
    public List<CalcAccessor> calcs = null;
    public ChannelTracker.ChannelTrackerList watching = null;
    public void Update(double nstate){
      if(double.IsNaN(nstate)) nstate=0;
      val = nstate;
      if(mods!=null) foreach(var mod in mods) mod.Update(nstate);
      if(calcs!=null) foreach(var c in calcs) c.calc.Update(c.index,nstate);
      if(watching!=null) watching.Apply(nstate);
    }
    public void Remove(bool silent){
      val = double.NaN;
      var l1 = mods;
      var l2 = calcs;
      mods=null;
      calcs=null;
      if(silent) return;
      foreach(var li in l1) li.Remove();
      foreach(var li in l2) li.calc.Remove();
    }
    public ChannelVal(double value)=>this.val = value;
  }
  static ChannelVal AddVal(string ch, double ival){
    ChannelVal val = new(ival);
    state.Add(ch,val);
    return val;
  }
  [Import.SpeedrunToolIop.Static]
  [ResetEvents.ClearOn(ResetEvents.RunTimes.OnExit,ResetEvents.RunTimes.OnReload)]
  private static Dictionary<string, ChannelVal> state = new ();

  public static double readChannel(string ch)=>_readChannel(Util.removeWhitespace(ch));
  internal static double _readChannel(string ch){
    if(state.TryGetValue(ch, out var v)) return v.val;
    else return addModifier(ch).val;
  }
  private static ChannelVal _getVal(string ch){
    if(state.TryGetValue(ch, out var v)) return v;
    else return addModifier(ch);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool checkClean(string ch){
    int idx=0;
    for(;idx<ch.Length;idx++) if(ch[idx]=='[' || ch[idx]=='(') return false;
    return true;
  }
  static void SetChannelRaw(string ch, double nval){
    var val = _getVal(ch);
    if(val.val==nval) return;
    val.Update(nval);
  }
  public static void SetChannel(string ch, double nval, bool fromInterop=false){
    ch=Util.removeWhitespace(ch);
    if(ch.Length == 0 || !checkClean(ch)) return;
    SetChannelRaw(ch,nval);
    if(!fromInterop && ch.Length>0 && !lockCross){
      if(ch[0]=='$')(Engine.Instance.scene as Level)?.Session.SetFlag(ch.Substring(1),nval!=0);
      if(ch[0]=='#')(Engine.Instance.scene as Level)?.Session.SetCounter(ch.Substring(1),(int)nval);
      if(ch[0]=='?')(Engine.Instance.scene as Level)?.Session.SetSlider(ch.Substring(1),(float)nval);
    }
  }
  public static double watch(ChannelTracker b){
    if(b.channel == null) return 0;
    string ch = Util.removeWhitespace(b.channel);
    var val = _getVal(ch);
    (val.watching??=new()).Add(b);
    return val.val;
  }
  [MethodImpl(MethodImplOptions.NoInlining)]
  static ChannelVal addModifier(string ch){
    ChannelVal ret=null;
    if(!checkClean(ch)){
      if(ch[^1]==']') return AddVal(ch, new ModifierDesc(ch).outval);
      if(ch[^1]==')') return AddVal(ch, new InlineCalc(ch).outval);
      DebugConsole.Write($"{ch} contains '(' or '[' but doesn't end with one! 0 will be returned.");
      return new ChannelVal(0);
    } else if(ch.Length>0 && (ch[0]=='$'||ch[0]=='#'||ch[0]=='?')){
      if(ch[0]=='#') ret = AddVal(ch, (Engine.Instance.scene as Level)?.Session.GetCounter(ch.Substring(1))??0);
      if(ch[0]=='$') ret = AddVal(ch, ((Engine.Instance.scene as Level)?.Session.GetFlag(ch.Substring(1))??false)?1:0);
      if(ch[0]=='?') ret = AddVal(ch, (Engine.Instance.scene as Level)?.Session.GetSlider(ch.Substring(1))??0);
    } else ret = AddVal(ch,0);
    TemplateTemplate.addBlame(ch,TemplateTemplate.Loc.Channel);
    return ret;
  }
  static List<ChannelTracker> UnwatchGetPersistant(){
    List<ChannelTracker> ret = new();
    foreach(var (k,v) in state){
      if(v.watching?.RemoveTemp()??false) foreach(var ct in v.watching.getList()) ret.Add(ct);
      else v.watching=null;
    }
    return ret;
  }
  static void RemoveUncleanAndDeps(){
    HashSet<string> toRemove = new();
    foreach(var (k,v) in state){
      if(!checkClean(k) || v.val==0){
        toRemove.Add(k);
        v.Remove(true);
      }else{
        v.calcs = null;
        v.mods = null;
      }
    }
    foreach(var ch in toRemove) state.Remove(ch);
  }
  public static void unwatchTemporary(bool keepPersist){
    var li = UnwatchGetPersistant();
    RemoveUncleanAndDeps();
    foreach(var ct in li) if(keepPersist || ct.Entity.TagCheck(Tags.Global)) watch(ct);
  }
  static Queue<string> ToForceRemove = new();
  static bool removing;
  public static void ForceRemove(string ch = null){
    if(ch!=null)ToForceRemove.Enqueue(ch);
    if(removing) return;
    removing = true;
    while(ToForceRemove.TryDequeue(out var res)){
      if(state.TryGetValue(res, out var val)){
        state.Remove(res);
        val.Remove(false);
      }
    } 
    removing = false;
  }
  public static void ClearAll(){
    foreach(var (k,v) in state) v.Remove(true);
    state.Clear();
  }
  public static void clearChannels(string prefix = ""){
    prefix = Util.removeWhitespace(prefix);
    if(string.IsNullOrEmpty(prefix)){
      var v = UnwatchGetPersistant();
      ClearAll();
      foreach(var ct in v){
        var nv = watch(ct);
        if(ct.value != nv) ct.setChVal(nv);
      }
    }
    foreach(var (ch,_) in state){
      if(ch.StartsWith(prefix)) ToForceRemove.Enqueue(ch);
    }
    ForceRemove(null);
  }

  public class AdvancedSetter{
    Dictionary<string, Tuple<string, double>> toDo = new();
    public AdvancedSetter(string str){
      foreach(var v in Util.kvparseflat(str)){
        Tuple<string,double> n=null;
        if(string.IsNullOrWhiteSpace(v.Value)) n = new(null,1);
        else if(v.Value[0]=='@') n = new(v.Value.Substring(1),0);
        else if(double.TryParse(v.Value, out var ival)) n=new(null,ival);
        else n = new(v.Value,0);
        if(!toDo.TryAdd(v.Key,n)) DebugConsole.WriteFailure("Duplicate key; forbidden");
      }
    }
    public void Apply(){
      foreach(var v in toDo){
        SetChannel(v.Key,v.Value.Item1 is {} str? readChannel(str) : v.Value.Item2);
      }
    }
  }

  internal class ChannelReader{
    public string ch;
    public ChannelVal cache = null;
    public ChannelReader(string channel){
      ch=channel.RemovePrefix("@");
      if(ch!=null) cache=new(double.NaN);
    }
  }
  internal class ChannelReaderFloat:ChannelReader{
    float val;
    public ChannelReaderFloat(string parse):base(
      (parse.Length==0 || float.TryParse(parse, out var _))?null:parse
    )=>val = float.TryParse(parse, out var p)?p:default;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator float(ChannelReaderFloat o){
      if(o.ch==null) return o.val;
      var c = o.cache;
      if(double.IsNaN(c.val)) c = o.cache = _getVal(o.ch);
      return (float) c.val;
    }
  }
  internal class ChannelReaderInt:ChannelReader{
    int val;
    public ChannelReaderInt(string parse):base(
      (parse.Length==0 || int.TryParse(parse, out var _))?null:parse
    )=>val = int.TryParse(parse, out var p)?p:default;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ChannelReaderInt o){
      if(o.ch==null) return o.val;
      var c = o.cache;
      if(double.IsNaN(c.val)) c = o.cache = _getVal(o.ch);
      return (int) Math.Floor(c.val);
    }
  }
  internal class ChannelReaderBool:ChannelReader{
    bool val;
    public ChannelReaderBool(string parse):base(
      (parse.Length==0 || int.TryParse(parse, out var _))?null:parse
    )=>val = bool.TryParse(parse, out var p)?p:default;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(ChannelReaderBool o){
      if(o.ch==null) return o.val;
      var c = o.cache;
      if(double.IsNaN(c.val)) c = o.cache = _getVal(o.ch);
      return c.val==0;
    }
  }










  public static Dictionary<string,double> save(){
    Dictionary<string,double> s=new();
    foreach(var (ch,v) in state){
      if(ch.Length>=1 && (ch[0]=='$'||ch[0]=='#'||ch[0]=='?'))continue;
      if(ch.Contains(TemplateTemplate.randomChar)||!checkClean(ch)) continue;
      if(v.val!=0) s.Add(ch,v.val);
    }
    return s;
  }
  public static void load(Dictionary<string,double> s){
    ClearAll();
    foreach(var (k,v) in s){
      state[k] = new(v);
    }
  }
  internal static bool lockCross = false; 
  [OnLoad.OnHook(typeof(Session),nameof(Session.SetFlag))]
  static void Hook(On.Celeste.Session.orig_SetFlag orig, Session s, string f, bool v){
    orig(s,f,v);
    if(v) TemplateTemplate.addBlame(f,TemplateTemplate.Loc.Flag);
    if(!lockCross && state.ContainsKey('$'+f)) SetChannel('$'+f,v?1:0,true);
  }
  [OnLoad.OnHook(typeof(Session),nameof(Session.GetFlag))]
  static bool Hook(On.Celeste.Session.orig_GetFlag orig, Session s, string f){
    return orig(s,f) || (!string.IsNullOrEmpty(f) && f[0]=='@' && readChannel(f.Substring(1))!=0);
  }
  [OnLoad.EverestEvent(typeof(Everest.Events.Session),nameof(Everest.Events.Session.OnSliderChanged))]
  static void SliderChange(Session s, Session.Slider l, float? nval){
    if(!lockCross && state.ContainsKey('?'+l.Name)) SetChannel('?'+l.Name, nval??0,true);
  }
  [OnLoad.OnHook(typeof(Session),nameof(Session.SetCounter))]
  static void Hook(On.Celeste.Session.orig_SetCounter orig, Session s, string f, int n){
    orig(s,f,n);
    TemplateTemplate.addBlame(f,TemplateTemplate.Loc.Counter);
    if(!lockCross && state.ContainsKey('#'+f)) SetChannel('#'+f,n,true);
  }
  [OnLoad.OnHook(typeof(Session),nameof(Session.GetCounter))]
  static int Hook(On.Celeste.Session.orig_GetCounter orig, Session s, string f){
    if(string.IsNullOrEmpty(f) || f[0]!='@') return orig(s,f);
    foreach(var c in s.Counters) if(c.Key==f) return c.Value;
    return (int)readChannel(f.Substring(1));
  }

  [Command("ausp_setChannel","Set a channel")]
  public static void SetChCommand(string channel, int value){
    double oldval = readChannel(channel);
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
    foreach(var pair in state){
      Engine.Commands.Log($"{pair.Key} {pair.Value.val}");
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
    foreach(var (k,v) in state) if(v.watching!=null){
      Engine.Commands.Log($"{k} {v.watching.Count}");
    }
    Engine.Commands.Log("===================");
  }

  static bool onlyClean=false;
  static string setChannelTextfield="";
  static double setChannelValuefield = 1;
  public static void RenderChannelTab(){
    ImGui.checkbox("only clean", ref onlyClean);
    ImGui.begintable("channels",2);
    ImGui.makecolumn("channel");
    ImGui.makecolumn("value");
    ImGui.tableheader();
    foreach(var pair in state){
      if(onlyClean && !checkClean(pair.Key)) continue;
      ImGui.tablenextrow();
      ImGui.tablesetcol(0);
      ImGui.text(pair.Key);
      ImGui.tablesetcol(1);
      ImGui.text(pair.Value.val.ToString());
    }
    ImGui.endtable();
    ImGui.inputText("channel to modify", ref setChannelTextfield, 512);
    ImGui.inputdouble("value", ref setChannelValuefield);
    ImGui.sameline();
    if(ImGui.button("set")){
      SetChannel(setChannelTextfield, setChannelValuefield);
    }
    ImGui.sameline();
    if(ImGui.button("watch")){
      readChannel(setChannelTextfield);
    }
  }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using AsmResolver;

namespace Celeste.Mod.auspicioushelper.channelmath;

public static class Parser{
  static Expression asInt(Expression e)=>Expression.Convert(e,typeof(int));
  static Expression asDouble(Expression e)=>Expression.Convert(e,typeof(double));
  static Expression asBool(Expression e)=>Expression.Convert(e,typeof(bool));
  class Descr{
    public Regex reg;
    public string mid;
    public string[] ndelims; 
    public Func<List<Expression>,List<(string,int)>,Expression> parse;
    public Descr(string mid, Func<List<Expression>,List<(string,int)>,Expression> fn, params string[] newDelim){
      this.mid=mid;
      parse = fn;
      this.ndelims = newDelim;
    }
  }
  const string TOK = @"#\d+";
  static Type[] withNum(int num)=>(new bool[num]).Map(x=>typeof(double)).ToArray();
  const BindingFlags bf = BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  static double Pi()=>Math.PI;
  static double Saturate(double d)=>Math.Clamp(d,0,1);
  static double Mix(double fac, double a, double b)=>fac*a+(1-fac)*b;
  static Dictionary<(int,string),MethodInfo> parenOps = new(){
    {new(0,"pi"), typeof(Parser).GetMethod(nameof(Pi),bf)},
    {new(1,"floor"), typeof(Math).GetMethod(nameof(Math.Floor),withNum(1))},
    {new(1,"ceil"), typeof(Math).GetMethod(nameof(Math.Ceiling),withNum(1))},
    {new(1,"round"), typeof(Math).GetMethod(nameof(Math.Round),withNum(1))},
    {new(1,"exp"), typeof(Math).GetMethod(nameof(Math.Exp),withNum(1))},
    {new(1,"ln"), typeof(Math).GetMethod(nameof(Math.Log),withNum(1))},
    {new(1,"log"), typeof(Math).GetMethod(nameof(Math.Log2),withNum(1))},
    {new(1,"sqrt"), typeof(Math).GetMethod(nameof(Math.Sqrt),withNum(1))},
    {new(1,"saturate"), typeof(Parser).GetMethod(nameof(Saturate),bf)},
    {new(2,"max"), typeof(Math).GetMethod(nameof(Math.Max),withNum(2))},
    {new(2,"min"), typeof(Math).GetMethod(nameof(Math.Min),withNum(2))},
    {new(2,"pow"), typeof(Math).GetMethod(nameof(Math.Pow),withNum(2))},
    {new(3,"clamp"), typeof(Math).GetMethod(nameof(Math.Clamp),withNum(3))},
    {new(3,"mix"), typeof(Parser).GetMethod(nameof(Mix),bf)},
  };
  static readonly List<Descr> ops = new(){
    new Descr("(?:\\d+\\.\\d*)|(?:(0x|0b)?\\d+)|(?:\\.\\d*)",(s,l)=>{
      string str = l[0].Item1;
      if(str.StartsWith("0x")) return Expression.Constant(Convert.ToUInt32(str.Substring(2),16),typeof(double));
      if(str.StartsWith("0b")) return Expression.Constant(Convert.ToUInt32(str.Substring(2),2),typeof(double));
      return Expression.Constant(double.Parse(str));
    }), 
    new Descr($"(?:[a-zA-Z_][\\w]*)?\\(({TOK}(?:,{TOK})*)?\\)",(s,l)=>{
      int num = l.Count/2;
      string name = l[0].Item1.Substring(0,l[0].Item1.Length-1).ToLower();
      DebugConsole.Write("Looking for func",name,num);
      if(num==1 && name=="") return s[l[1].Item2];
      if(parenOps.TryGetValue(new(num,name),out var method)){
        var par = new Expression[num];
        for(int i=0; i<num; i++) par[i]=s[l[i*2+1].Item2];
        return Expression.Call(method,par);
      }
      throw new Exception($"Could not find method {name} that accepts {num} arguments.");
    }),
    new Descr($"[\\!\\~]{TOK}",(s,l)=>l[0].Item1 switch {
      "!"=>Expression.Not(asBool(s[l[1].Item2])),
      "~"=>Expression.Not(asInt(s[l[1].Item2])),
      _=> throw new Exception("bye")
    },"\\!","\\~"),
    new Descr($"({TOK}(?:\\*|\\/|\\/\\/|\\%|\\%\\%))+{TOK}",(s,l)=>{
      Expression cur = s[l[0].Item2];
      for(int i=1; i<l.Count; i+=2){
        cur = l[i].Item1 switch {
          "*"=>Expression.Multiply(cur,s[l[i+1].Item2]),
          "%"=>Expression.Modulo(cur,s[l[i+1].Item2]),
          "%%"=>asDouble(Expression.Modulo(asInt(cur),asInt(s[l[i+1].Item2]))),
          "/"=>Expression.Divide(cur,s[l[i+1].Item2]),
          "//"=>asDouble(Expression.Divide(asInt(cur),asInt(s[l[i+1].Item2]))),
          _=> throw new Exception("bye")
        };
      }
      return cur;
    },"\\*","\\/","\\%","\\/\\/"),
    new Descr($"{TOK}?([\\+\\-]{TOK})+",(s,l)=>{
      bool hs = l[0].Item1!=null;
      Expression cur = hs?Expression.Constant(0):s[l[0].Item2];
      for(int i=hs?0:1; i<l.Count; i+=2){
        cur = l[i].Item1 switch {
          "+"=>Expression.Add(cur,s[l[i+1].Item2]),
          "-"=>Expression.Subtract(cur,s[l[i+1].Item2]),
          _=> throw new Exception("bye")
        };
      }
      return cur;
    },"\\+","\\-"),
    new Descr($"{TOK}(\\<\\=|\\>\\=|\\<|\\>){TOK}",(s,l)=>l[1].Item1 switch {
      "<="=>asDouble(Expression.LessThanOrEqual(s[l[0].Item2],s[l[2].Item2])),
      ">="=>asDouble(Expression.GreaterThanOrEqual(s[l[0].Item2],s[l[2].Item2])),
      "<"=>asDouble(Expression.LessThan(s[l[0].Item2],s[l[2].Item2])),
      ">"=>asDouble(Expression.GreaterThan(s[l[0].Item2],s[l[2].Item2])),
      _=>throw new Exception("bye")
    },"\\<\\=","\\>\\=","\\<","\\>"),
    new Descr($"{TOK}(\\<\\<|\\>\\>){TOK}",(s,l)=>l[1].Item1 switch{
      "<<"=>asDouble(Expression.LeftShift(asInt(s[l[0].Item2]),asInt(s[l[2].Item2]))),
      ">>"=>asDouble(Expression.RightShift(asInt(s[l[0].Item2]),asInt(s[l[2].Item2]))),
      _=>throw new Exception("bye")
    },"\\<\\<","\\>\\>"),
    new Descr($"{TOK}(\\=\\=|\\!\\=){TOK}",(s,l)=>l[1].Item1 switch {
      "=="=>asDouble(Expression.Equal(s[l[0].Item2],s[l[2].Item2])),
      "!="=>asDouble(Expression.NotEqual(s[l[0].Item2],s[l[2].Item2])),
      _=>throw new Exception("bye")
    },"\\=\\=","\\!\\="),
    new Descr($"{TOK}\\&{TOK}",(s,l)=>asDouble(Expression.And(asInt(s[l[0].Item2]),asInt(s[l[2].Item2]))),"\\&"),
    new Descr($"{TOK}\\^{TOK}",(s,l)=>asDouble(Expression.ExclusiveOr(asInt(s[l[0].Item2]),asInt(s[l[2].Item2]))),"\\^"),
    new Descr($"{TOK}\\|{TOK}",(s,l)=>asDouble(Expression.Or(asInt(s[l[0].Item2]),asInt(s[l[2].Item2]))),"\\|"),
    new Descr($"{TOK}\\&\\&{TOK}",(s,l)=>asDouble(Expression.AndAlso(asBool(s[l[0].Item2]),asBool(s[l[2].Item2]))),"\\&\\&"),
    new Descr($"{TOK}\\|\\|{TOK}",(s,l)=>asDouble(Expression.OrElse(asBool(s[l[0].Item2]),asBool(s[l[2].Item2]))),"\\|\\|"),
  };
  static Parser(){
    string delims = "|,";
    for(int i=ops.Count-1; i>=0; i--){
      ops[i].reg = new Regex($"(^|(?<=\\({delims}))({ops[i].mid})($|(?=\\){delims}))",RegexOptions.Compiled);
      //DebugConsole.Write("",i,$"(^|(?<=\\({delims}))({ops[i].mid})($|(?=\\){delims}))");
      foreach(var d in ops[i].ndelims) delims+="|"+d;
    }
  }
  static Regex beginStringCh = new(@"^(?:[$#?]?"+
    $"[\\w{TemplateTemplate.randomChar}]*[A-Za-z{TemplateTemplate.randomChar}][\\w{TemplateTemplate.randomChar}]*"+
    @"(?:\[[^\]]*\])*)|^(?:\[[^\]]*\])+",RegexOptions.Compiled);
  static Regex pureStringStart = new($"^[\\w{TemplateTemplate.randomChar}\\.]*",RegexOptions.Compiled);
  static List<string> extractChannels(string expr, out string parsed){
    List<string> found = new();
    string cur;
    parsed="";
    int idx=0;
    Stack<char> unescaped = new();
    while(idx<expr.Length){
      var m = beginStringCh.Match(expr.Substring(idx));
      if(m.Success){
        cur = m.Groups[0].Value;
        idx+= cur.Length;
        if(idx<expr.Length && expr[idx]=='('){
          parsed+=cur;
          continue;
        }
      } else if(expr[idx]=='@'){
        m = pureStringStart.Match(expr.Substring(idx+1));
        idx+= 1+m.Length;
        cur = m.Groups[0].Value;
        while(idx<expr.Length){
          char c = expr[idx];
          if(Util.escape.TryGetValue(c, out char ue)) unescaped.Push(ue);
          else if(unescaped.Count>0){
            if(unescaped.Peek()==c) unescaped.Pop();
          } else break;
          idx++;
          cur+=c;
        }
        if(unescaped.Count>0) throw new Exception("Unbalanced braces in channel expression");
      } else {
        parsed+=expr[idx];
        idx++;
        continue;
      }
      int i=0;
      cur = Util.removeWhitespace(cur);
      for(; i<found.Count; i++) if(cur==found[i]) break;
      if(i==found.Count) found.Add(cur);
      parsed+="#"+i.ToString();
    }
    return found;
  }
  //goddddd i sorta wana do like proper constant collapsing but whatever that was so last yesterday
  static Regex doneRe = new Regex($"^{TOK}$",RegexOptions.Compiled);
  static Regex symparseRe = new Regex(@"#(\d+)|([^#]+)",RegexOptions.Compiled);
  public static Func<double[],double> ParseToFunc(string input, out List<string> channels){
    channels = extractChannels(input, out string expr);
    DebugConsole.Write($"parsed ({string.Join(" ",channels)})", expr);
    var inparam = Expression.Parameter(typeof(double[]));
    var symbols = channels.Map((x,i)=>(Expression)Expression.ArrayIndex(inparam,Expression.Constant(i)));
    Dictionary<string,int> syms = new();
    List<(string,int)> par = new();
    for(int iter=0; iter<256; iter++){
      int t=0; 
      Match m=null;
      for(; t<ops.Count; t++){
        m = ops[t].reg.Match(expr);
        if(m.Success)break;
      }
      if(!m.Success){
        if(doneRe.Match(expr).Success){
          // DebugConsole.Write("Parse succeeded");
          // for(int i=0; i<ch.Count; i++) DebugConsole.Write("#",i,ch[i]);
          // foreach(var (k,v) in syms) DebugConsole.Write("#",v,k);
          var last = symbols[int.Parse(expr.Substring(1))];
          var lambda = Expression.Lambda<Func<double[],double>>(last,inparam);
          return lambda.Compile();
        } else throw new Exception($"Could not parse. Failed at {expr}");
      }
      var nf=""; 
      int lidx=0;
      do {
        if(!syms.TryGetValue(m.Groups[0].Value, out var sym)){
          var r = symparseRe.Match(m.Groups[0].Value);
          do{
            if(r.Groups[1].Success) par.Add(new(null,int.Parse(r.Groups[1].Value)));
            else par.Add(new(r.Groups[2].Value,-1));
          } while((r=r.NextMatch()).Success);
          syms.Add(m.Groups[0].Value,sym=symbols.Count);
          symbols.Add(ops[t].parse(symbols,par));
          par.Clear();
        }
        nf += expr.Substring(lidx,m.Index-lidx)+"#"+sym;
        lidx = m.Index+m.Length;
      }while((m = m.NextMatch()).Success);
      expr=nf+expr.Substring(lidx);
    } 
    throw new Exception("Could not parse "+input);
  }
}
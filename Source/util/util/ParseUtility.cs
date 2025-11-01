


using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Celeste.Editor;
using Monocle;
using Microsoft.Xna.Framework;
using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.auspicioushelper;
public static partial class Util{

  public static Color hexToColor(string hex){
    hex = hex.TrimStart('#');
    uint rgba = uint.Parse(hex, NumberStyles.HexNumber);
    int shift = hex.Length>4?8:4;
    uint mask = hex.Length>4?0xffu:0xfu;
    float mult = hex.Length>4?1f/255f:1f/15f;
    if(hex.Length %4 != 0){
      rgba= (rgba<<shift)+mask;
    }
    return new Color(
      (float)((rgba>>(shift*3))&mask)*mult, 
      (float)((rgba>>(shift*2))&mask)*mult, 
      (float)((rgba>>shift)&mask)*mult, 
      (float)(rgba&mask)*mult);
  }
  public static Vector4 hexToColorVec(string hex){
    hex = hex.TrimStart('#');
    uint rgba = uint.Parse(hex, NumberStyles.HexNumber);
    int shift = hex.Length>4?8:4;
    uint mask = hex.Length>4?0xffu:0xfu;
    float mult = hex.Length>4?1f/255f:1f/15f;
    if(hex.Length %4 != 0){
      rgba= (rgba<<shift)+mask;
    }
    return new Vector4(
      (float)((rgba>>(shift*3))&mask)*mult, 
      (float)((rgba>>(shift*2))&mask)*mult, 
      (float)((rgba>>shift)&mask)*mult, 
      (float)(rgba&mask)*mult);
  }
  public static Vector4 hexToOklabVec(string hex)=>hexToColorVec(hex).SrgbToOklab();
  public static bool tryGetStr(this EntityData d, string key, out string str){
    str = d.Attr(key,"");
    return !string.IsNullOrWhiteSpace(str);
  }
  public static string StringOrNull(this EntityData d, string key){
    return d.tryGetStr(key, out var res)?res:null;
  }
  static public EntityData cloneWithForcepos(this EntityData a, Vector2? forcepos){
    if(forcepos is not Vector2 v) return a;
    EntityData e = Util.shallowCopy(a);
    e.Position = v;
    return e;
  }
  static public EntityData cloneWithForceposOffset(this EntityData a, Vector2 forcepos){
    Vector2 offset = a.Position-forcepos;
    EntityData e = shallowCopy(a);
    if(e.Nodes!=null){
      e.Nodes = (Vector2[])e.Nodes.Clone();
      for(int i=0; i<e.Nodes.Length; i++) e.Nodes[i]=e.Nodes[i]-offset; 
    }
    e.Position = forcepos;
    return e;
  }
  public static string removeWhitespace(string s){
    if(s==null) return null;
    Span<char> buf = stackalloc char[s.Length];
    int j=0;
    foreach(char c in s){
      if(!char.IsWhiteSpace(c))buf[j++]=c;
    }
    return new string(buf[..j]);
  }

  public static int bsearchLast(float[] arr, float val){
    int left = 0; 
    int right = arr.Length;
    while(right-left>1){
      int middle = (left+right)/2;
      if(arr[middle]>val) right = middle;
      else left = middle;
    }
    return left;
  }
  public static int bsearchLast(double[] arr, double val){
    int left = 0; 
    int right = arr.Length;
    while(right-left>1){
      int middle = (left+right)/2;
      if(arr[middle]>val) right = middle;
      else left = middle;
    }
    return left;
  }
  public static int bsearchFirst(float[] arr, float val){
    int left = -1; 
    int right = arr.Length-1;
    while(right-left>1){
      int middle = (left+right+1)/2;
      if(arr[middle]>=val){
        right = middle;
      } else {
        left = middle;
      }
    }
    return right;
  }
  public static float remap(float t, float low, float high){
    t=t-low;
    high = high-low;
    return t/high;
  }
  public static double remap(double t, double low, double high){
    t=t-low;
    high = high-low;
    return t/high;
  }
  public static string concatPaths(string a, string b){
    if(string.IsNullOrWhiteSpace(a)) return b.Trim();
    if(string.IsNullOrWhiteSpace(b)) return a.Trim();
    a=a.Trim();
    b=b.Trim();
    bool ae=a[^1]=='/';
    bool be=b[0]=='/';
    if(ae != be) return a+b;
    if(ae && be) return a+b.Substring(1);
    return a+"/"+b;
  }
  static Dictionary<char,char> escape = new Dictionary<char, char>{
    {'{','}'}, {'[',']'}, {'(',')'},
  };
  public static Dictionary<string,string> kvparseflat(string str, bool strip=false, bool stripout=false){
    if(strip) str=stripEnclosure(str);
    Stack<char> unescaped = new Stack<char>();
    var o = new Dictionary<string,string>();
    string k="";
    string v="";
    int idx=0;
    bool escapeNext = false;
    parsekey:
      if(idx>=str.Length) return o;
      if(str[idx] == ':'){
        idx++;goto parsevalue;
      }
      else{
        k+=str[idx]; idx++; goto parsekey;
      }

    parsevalue:
      if((idx >= str.Length||str[idx] == ',') && unescaped.Count ==0){
        idx++; goto fent;
      }
      if(idx >= str.Length){
        DebugConsole.WriteFailure("PARSE ERROR: "+str);
        return null;
      }
      if(escape.TryGetValue(str[idx], out var esc)){
        unescaped.Push(esc);
        v+=str[idx]; idx++; goto parsevalue;
      }
      if(unescaped.Count>0 && unescaped.Peek()==str[idx]){
        unescaped.Pop(); 
        v+=str[idx]; idx++; goto parsevalue;
      }
      if(str[idx]=='"'){
        v+=str[idx]; idx++; goto parsestring;
      }
      v+=str[idx]; idx++; goto parsevalue;

    parsestring:
      if(idx == str.Length){
        DebugConsole.WriteFailure("PARSE ERROR: "+str);
      }
      if(escapeNext){
        escapeNext = false; 
        v+=str[idx]; idx++; goto parsestring;
      }
      if(str[idx] == '"'){
        v+=str[idx]; idx++; goto parsevalue;
      }
      v+=str[idx]; idx++; goto parsestring;

    fent:
      bool flag;
      if(stripout) flag=o.TryAdd(k.Trim(),stripEnclosure(v.Trim()));
      else flag = o.TryAdd(k.Trim(),v.Trim());
      if(!flag) DebugConsole.Write($"Parsed dictionary has two identical keys {k}:\n {str}");
      k=""; v="";
      goto parsekey;
  }
  public static List<string> listparseflat(string str,bool strip=false,bool stripout=false){
    if(string.IsNullOrWhiteSpace(str)) return new();
    if(strip) str=stripEnclosure(str);
    Stack<char> unescaped = new Stack<char>();
    var o = new List<string>();
    string v="";
    int idx=0;
    bool escapeNext = false;
    parsevalue:
      if((idx >= str.Length||str[idx] == ',') && unescaped.Count ==0){
        idx++; goto fent;
      }
      if(idx >= str.Length){
        DebugConsole.WriteFailure("PARSE ERROR: "+str);
        return null;
      }
      if(escape.TryGetValue(str[idx], out var esc)){
        unescaped.Push(esc);
        v+=str[idx]; idx++; goto parsevalue;
      }
      if(unescaped.Count>0 && unescaped.Peek()==str[idx]){
        unescaped.Pop(); 
        v+=str[idx]; idx++; goto parsevalue;
      }
      if(str[idx]=='"'){
        v+=str[idx]; idx++; goto parsestring;
      }
      v+=str[idx]; idx++; goto parsevalue;

    parsestring:
      if(idx == str.Length){
        DebugConsole.WriteFailure("PARSE ERROR: "+str);
      }
      if(escapeNext){
        escapeNext = false; 
        v+=str[idx]; idx++; goto parsestring;
      }
      if(str[idx] == '"'){
        v+=str[idx]; idx++; goto parsevalue;
      }
      v+=str[idx]; idx++; goto parsestring;

    fent:
      if(stripout) o.Add(stripEnclosure(v.Trim()));
      else o.Add(v.Trim());
      if(idx>=str.Length) return o;
      v="";
      goto parsevalue;
  }
  public static VirtualMap<char> toCharmap(string s, int padding=0){
    Regex regex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
    string[] arr = regex.Split(s);
    VirtualMap<char> vm = new(arr.Max(x=>x.Length)+2*padding,arr.Length+2*padding);
    for(int row=0; row<arr.Length; row++){
      for(int col=0; col<arr[row].Length; col++){
        vm[col+padding,row+padding] = arr[row][col];
      }
    }
    return vm;
  }
  public static string stripEnclosure(string str){
    if(str == "") return "";
    if(str[0] == '\"' && str[str.Length-1] == '\"') return str.Substring(1,str.Length-2);
    if(escape.TryGetValue(str[0],out var esc)){
      if(str[str.Length-1]==esc)return str.Substring(1,str.Length-2);
      else {
        DebugConsole.WriteFailure("Enclosing characters not symmetric: "+str);
        return str;
      }
    }
    return str;
  }
  public static float[] csparseflat(string str){
    if(string.IsNullOrWhiteSpace(str)) return [];
    return str.Split(",").Select(s=>{
      float.TryParse(s, out var l);
      return l;
    }).ToArray();
  }
  public static float[] csparseflat(string str, params float[] defaults){
    if(string.IsNullOrWhiteSpace(str)) return defaults;
    float[] res = new float[defaults.Length];
    var l = str.Split(",");
    for(int i=0; i<defaults.Length; i++){
      res[i]=(i<l.Length && float.TryParse(l[i], out float f))?f:defaults[i];
    }
    return res;
  }
  public static int[] ciparseflat(string str){
    if(string.IsNullOrWhiteSpace(str)) return [];
    return str.Split(",").Select(s=>{
      int.TryParse(s, out var l);
      return l;
    }).ToArray();
  }
  public static float[] toArray(Vector2 x)=>new float[]{x.X,x.Y};
  public static float[] toArray(Vector3 x)=>new float[]{x.X,x.Y,x.Z};
  public static float[] toArray(Vector4 x)=>new float[]{x.X,x.Y,x.Z,x.W};
  public static string nullIfEmpty(string s)=>string.IsNullOrWhiteSpace(s)?null:s;

  public static string sideBySide(List<string> strs, string seperator = " "){
    List<string[]> sp = strs.Select(s=>s.Split('\n')).ToList();
    List<int> widths = sp.Select(l=>l.Max(s=>s.Length)).ToList();
    int lines = sp.Max(l=>l.Length);
    string res = "";
    for(int i=0; i<lines; i++){
      for(int j=0; j<sp.Count; j++){
        if(i<sp[j].Length){
          res+=sp[j][i]+new string(' ', widths[j]-sp[j][i].Length)+seperator;
        } else {
          res+=new string(' ',widths[j])+seperator;
        }
      }
      res+= '\n';
    }
    return res;
  }
  public static FloatRect levelBounds(Scene s){
    if(s is Level l){
      return new FloatRect(l.Bounds.Left,l.Bounds.Top,l.Bounds.Right-l.Bounds.Left,l.Bounds.Bottom-l.Bounds.Top);
    }
    return FloatRect.empty;
  }
  public static void RemovePred<T>(HashSet<T> set, Func<T,bool> pred){
    List<T> tr = new();
    foreach(T a in set){
      if(pred(a)) tr.Add(a);
    }
    foreach(T a in tr) set.Remove(a);
  }
  
  public static string ToArr<T>(T[] arr){
    string res = "[";
    for(int i=0; i<arr.Length; i++){
      if(i!=0) res+=", ";
      res+=arr[i].ToString();
    }
    return res+"]";
  }
  public static string TrimSt(Player p, int idx){
    string s = p.StateMachine.GetStateName(idx).ToLower();
    if(s.StartsWith("st"))s=s.Substring(2);
    return s;
  }
  public static string CurrentSt(Player p){
    return TrimSt(p,p.StateMachine.state);
  }
  public class DictWrapper{
    Dictionary<string,string> w;
    public DictWrapper(Dictionary<string,string> wrapped){
      w=wrapped;
    }
    public float Float(string key, float fallback)=>w.TryGetValue(key, out var str) && float.TryParse(str, out var f)? f:fallback;
    public int Int(string key, int fallback)=>w.TryGetValue(key, out var str) && int.TryParse(str, out var f)? f:fallback;
    public float Float(IEnumerable<string> key, float fallback){
      foreach(var k in key){
        if(w.TryGetValue(k, out var str) && float.TryParse(str, out var f)) return f;
      }
      return fallback;
    }
    public bool TryFloat(IEnumerable<string> key, out float result){
      foreach(var k in key){
        if(w.TryGetValue(k, out var str) && float.TryParse(str, out result)) return true;
      }
      result=default;
      return false;
    }
    public int Int(IEnumerable<string> key, int fallback){
      foreach(var k in key){
        if(w.TryGetValue(k, out var str) && int.TryParse(str, out var i)) return i;
      }
      return fallback;
    }
    public bool TryInt(IEnumerable<string> key, out int result){
      foreach(var k in key){
        if(w.TryGetValue(k, out var str) && int.TryParse(str, out result)) return true;
      }
      result=default;
      return false;
    }
  }
  public static string AsClean(this string instr){
    string str = "";
    foreach(char c in instr) if(!char.IsWhiteSpace(c))str+=c;
    return str.ToLower();
  }
}
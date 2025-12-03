using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;
public static class DebugConsole {
  private static Thread consoleThread;
  private static readonly BlockingCollection<string> messageQueue = new BlockingCollection<string>();
  public static bool open=false;

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool AllocConsole();

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool FreeConsole();

  public static void Open() {
    if (consoleThread != null || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

    if (!AllocConsole()) {
      Logger.Log("auspicious","Failed to allocate a console.");
      return;
    }

    consoleThread = new Thread(() => {
      Console.Title = "Debug Consolew"; // This line works only if the console is attached
      StreamWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
      writer.WriteLine("Console thread started!");
      while (true) {
        string message = messageQueue.Take();
        if (message == null) break;
        if(open)try{writer.WriteLine(message);}catch(Exception){}
      }
    }) {
      IsBackground = true
    };

    consoleThread.Start();
    open = true;
  }
  public static void Write(string message) {
    switch(auspicioushelperModule.Settings.DebugConsoleMode){
      case auspicioushelperModuleSettings.DebugMode.WindowsConsole:
        if(!open) return;
        if (consoleThread == null) throw new InvalidOperationException("Debug console not open.");
        try{
          messageQueue.Add(message);
        }catch(Exception){}
        break;
      case auspicioushelperModuleSettings.DebugMode.CommandLog:
        Engine.Commands?.Log(message);
        break;
      case auspicioushelperModuleSettings.DebugMode.LogTxtPollute:
        if(!dontWrite) Logger.Info("AuspiciousDebug",message);
        break;
    }
  }
  public static void Write(string message, object o){
    Write(message+" "+(o==null?"NULL":o.ToString()));
  }
  public static void nl(int n){
    for(int i=0; i<n; i++) Write("");
  }
  public static void Write(string message, params object[] os){
    string str = message+" ";
    foreach(var o in os){
      str+=(o==null?"NULL":o.ToString())+" ";
    }
    Write(str);
  }
  public static void Write(char[,] arr){
    string res = "";
    for(int i=0; i<arr.GetLength(0); i++){
      if(i != 0) res+='\n';
      for(int j=0; j<arr.GetLength(1); j++){
        res+=arr[i,j];
      }
    }
    Write(res);
  }
  public static void Write<T>(T[] things, string label=""){
    string str = label+": ";
    foreach(var t in things) str+=(t==null)?"NULL, ": t.ToString()+", "; 
    Write(str);
  }
  public static T Pass<T>(string s, T inf){
    Write($"{s} {inf}");
    return inf;
  }
  public static void DumpIl(ILCursor c, int n1=20, int? n2 = null){
    int h; int l;
    if(n2 is int j){
      h=j; l=n1;
    } else {
      h=n1; l=-10;
    }
    for(int i=l; i<h; i++){
      try{
        if(i==0) DebugConsole.Write("===========");
        DebugConsole.Write(c.Instrs[c.Index+i].ToString());
      }catch(Exception){
        try{
          var instr = c.Instrs[c.Index+i];
          var op = "";
          try {op=instr.Operand.ToString();} catch(Exception){op="Unreadable";}
          Write($"{instr.OpCode} {instr.Operand}");
        } catch(Exception){
          DebugConsole.Write("Cannot");
        }
      }
    }
  }
  public static void Close() {
    if(!open) return;
    if (consoleThread == null) return;
    messageQueue.Add(null); // Signal the thread to exit (doesn't work??)
    FreeConsole();
    consoleThread = null;
    open = false;
  }
  public class PassingException:Exception{
    public PassingException(string s):base(s){}
  }
  static bool dontWrite=false;
  public static void WriteFailure(string s, bool alwayserror=false){
    using(new Util.AutoRestore<bool>(ref dontWrite, true))Write(s);
    Logger.Error("auspicioushelper",s);
    if(alwayserror||(auspicioushelperModule.Settings?.CrashOnFail??false)){
      throw new PassingException(s);
    }
  }
  public static void LogFullStackTrace(){
    var trace = new StackTrace(true); 
    nl(2);
    Write(trace.ToString());
    nl(2);
  }



  [Command("auspdebug_postcard", "make a postcard")]
  public static void MakePostcard(string str){
    if(string.IsNullOrEmpty(str)) str="Postcard!";
    LevelEnter.ErrorMessage = $"{str}";
    Engine.Scene = new LevelEnter((Engine.Scene as Level)?.Session,false);
  }
}

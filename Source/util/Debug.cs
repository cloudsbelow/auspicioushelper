using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
    if(!open) return;
    if (consoleThread == null) throw new InvalidOperationException("Debug console not open.");
    try{
      messageQueue.Add(message);
    }catch(Exception){

    }
  }
  public static void Write(string message, object o){
    Write(message+" "+(o==null?"NULL":o.ToString()));
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
        DebugConsole.Write("cannot");
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
  public static void WriteFailure(string s, bool alwayserror=false){
    Write(s);
    Logger.Error("auspicioushelper",s);
    if(alwayserror||(auspicioushelperModule.Settings?.CrashOnFail??false)){
      throw new Exception(s);
    }
  }
}

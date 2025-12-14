

using System;
using System.Linq;
using System.Reflection;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper.Import;

public static class ImGui{
  static public Type imgui;
  public static Func<string, int, bool> begintable;
  public static Action endtable;
  public static Action<string> makecolumn;
  public static Action tableheader;
  public static Action tablenextrow;
  public static Func<int, bool> tablesetcol;
  public static Action<string> text;
  public static Action sameline;

  public delegate bool InputTextDelegate(string name, ref string value, uint length);
  public static InputTextDelegate inputText;
  public delegate bool InputIntDelegate(string name, ref int value);
  public static InputIntDelegate inputint;
  public delegate bool InputDoubleDelegate(string name, ref double value);
  public static InputDoubleDelegate inputdouble;
  public delegate bool CheckboxDelegate(string name, ref bool pressed);
  public static CheckboxDelegate checkbox;
  public static Func<string, bool> button;

  static MethodInfo RegisterTab;
  public static void load(){
    var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x=>x.GetName().Name=="ImGui.NET");
    var a = (imgui = asm?.GetType("ImGuiNET.ImGui"));
    if(a==null) return;
    var bf = BindingFlags.Static|BindingFlags.Public;
    DebugConsole.Write("imgui", a);
    try{
      //foreach(MethodInfo m in a.GetMethods()) DebugConsole.Write(m.ToString());
      begintable = (Func<string, int, bool>) a.GetMethod("BeginTable", bf, [typeof(string), typeof(int)]).CreateDelegate(typeof(Func<string, int, bool>));
      endtable = (Action) a.GetMethod("EndTable", bf, []).CreateDelegate(typeof(Action));
      makecolumn = (Action<string>) a.GetMethod("TableSetupColumn", bf, [typeof(string)]).CreateDelegate(typeof(Action<string>));
      tableheader = (Action) a.GetMethod("TableHeadersRow", bf, []).CreateDelegate(typeof(Action));
      tablenextrow = (Action) a.GetMethod("TableNextRow", bf, []).CreateDelegate(typeof(Action));
      tablesetcol = (Func<int,bool>) a.GetMethod("TableSetColumnIndex", bf, [typeof(int)]).CreateDelegate(typeof(Func<int,bool>));
      text = (Action<string>) a.GetMethod("Text", bf, [typeof(string)]).CreateDelegate(typeof(Action<string>));
      sameline = (Action) a.GetMethod("SameLine", bf, []).CreateDelegate(typeof(Action));

      inputText =(InputTextDelegate) a.GetMethod("InputText",bf,[typeof(string),typeof(string).MakeByRefType(),typeof(uint)]).CreateDelegate(typeof(InputTextDelegate));
      inputint =(InputIntDelegate) a.GetMethod("InputInt",bf,[typeof(string),typeof(int).MakeByRefType()]).CreateDelegate(typeof(InputIntDelegate));
      inputdouble =(InputDoubleDelegate) a.GetMethod("InputDouble",bf,[typeof(string),typeof(double).MakeByRefType()]).CreateDelegate(typeof(InputDoubleDelegate));
      checkbox = (CheckboxDelegate) a.GetMethod("Checkbox",bf,[typeof(string),typeof(bool).MakeByRefType()]).CreateDelegate(typeof(CheckboxDelegate));
      button = (Func<string,bool>) a.GetMethod("Button", bf, [typeof(string)]).CreateDelegate(typeof(Func<string,bool>));

      RegisterTab = Util.getModdedType("MappingUtils","Celeste.Mod.MappingUtils.Api.Tabs")?.GetMethod("RegisterTab");
      BuildTabHelper("channels",ChannelState.RenderChannelTab, ()=>Engine.Instance.scene is Level);
      DebugConsole.Write("Built channels tab");
    } catch (Exception ex){DebugConsole.Write(ex.ToString());}
  }

  public static void BuildTabHelper(string tabName, Action renderImGui, Func<bool> canBeVisible,
      Action onOpen=null, Action onClose=null) {
    if(RegisterTab == null) return;
    DebugConsole.Write($"Added tab {tabName} to mapping utils");
    RegisterTab.Invoke(null, ["auspicioushelper", tabName, renderImGui, canBeVisible, onOpen??(static ()=>{}), onClose??(static ()=>{})]);
  }
}
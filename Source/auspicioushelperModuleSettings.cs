

using System;
using System.Collections.Generic;
using Celeste.Mod.UI;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class auspicioushelperModuleSettings : EverestModuleSettings {
  public enum DebugMode{
    None, WindowsConsole, CommandLog, LogTxtPollute
  }
  private DebugMode _DebugConsoleMode = DebugMode.None;
  [SettingSubText("Where debug information should be sent to")]
  public DebugMode DebugConsoleMode {
    get=>_DebugConsoleMode;
    set{
      if(value == DebugMode.WindowsConsole) DebugConsole.Open();
      else if(DebugConsole.open) DebugConsole.Close();
      _DebugConsoleMode = value;
    }
  }

  public bool Evil {get;set;}=false;
  public void CreateEvilEntry(TextMenu menu, bool ingame){
    if(!ingame) return;
    var item = new TextMenu.Button("Pack template rooms to log").Pressed(()=>{
      Audio.Play(SFX.ui_main_savefile_rename_start);
      EvilPackedTemplateRoom.PackTemplatesEvil();
    });
    menu.Add(item);
    item.AddDescription(menu,"Log.txt's template rooms to string format for use in evil packed rooms entity");
  }

  private bool _crashOnFail = false;
  [SettingSubText("Crash the game when you have invalid templates. For mappers.")]
  public bool CrashOnFail{
    get=>_crashOnFail;
    set=>_crashOnFail=value;
  }

  private bool _hideHelperMaps = false;
  [SettingSubText("Choose rules for hiding maps below")]
  public bool HideHelperMaps {
    get=>_hideHelperMaps;
    set{
      _hideHelperMaps = value;
      if(!MapHider.hasReloaded) return;
      if(value){
        if(userHideRules.Count>0 && !MapHider.isHiding) MapHider.setHide();
      }
      else if(MapHider.isHiding) MapHider.setUnhide();
    }
  }
  public List<string> userHideRules {get; set;} = new(["auspicioushelper/t(es)?"]);
  [SettingIgnore]
  [SettingSubText("Regex rules to match maps to hide")]
  public HideruleMenu HideRules {get;set;} = new();
  [SettingSubMenu]
  public class HideruleMenu{
    [SettingIgnore]
    public bool Dummy {get;set;}
    List<string> rules;
    TextMenu.Item makePressHandler(string s, int i){
      string val = s;
      TextMenu.Item entry = new TextMenu.Button(i.ToString()+": "+s).Pressed(()=>{
        Audio.Play(SFX.ui_main_savefile_rename_start);
        (Engine.Instance.scene as Overworld)?.Goto<OuiModOptionString>().Init<OuiModOptions>(
          s, (string value)=> val=value, (bool confirm)=>{
            if(confirm){
              rules[i] = val;
              if(auspicioushelperModule.Settings._hideHelperMaps)MapHider.setHide();
            }
            else val = rules[i];
          }, 100, 0
        );
      });
      return entry;
    }
    public void CreateDummyEntry(TextMenuExt.SubMenu menu, bool ingame){
      rules = auspicioushelperModule.Settings.userHideRules;
      rules.RemoveAll(string.IsNullOrEmpty);
      int i = 0;
      foreach(string s in rules){
        menu.Add(makePressHandler(s,i++));
      }
      rules.Add("");
      menu.Add(makePressHandler("",i));
    }
  }
}
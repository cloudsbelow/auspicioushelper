

using System;
using System.Collections.Generic;
using Celeste.Mod.UI;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class auspicioushelperModuleSettings : EverestModuleSettings {
  private bool _usingDebugConsole = false;
  public bool UseDebugConsole {
    get=>_usingDebugConsole;
    set{
      _usingDebugConsole = value;
      if(value) DebugConsole.Open();
      else DebugConsole.Close();
    }}
  private bool _tryQuietShader = false;
  [SettingSubText("Use compression-friendly shaders when available (not reccomended or intended)")]
  public bool UseQuietShader{
    get=>_tryQuietShader;
    set{
      _tryQuietShader = value;
    }
  }

  public bool Evil {get;set;}=false;
  public void CreateEvilEntry(TextMenu menu, bool ingame){
    if(!ingame) return;
    menu.Add(new TextMenu.Button("Pack template rooms to log").Pressed(()=>{
      Audio.Play(SFX.ui_main_savefile_rename_start);
      EvilPackedTemplateRoom.PackTemplatesEvil();
    }).AddDescription(menu,"Log.txt's template rooms to string format for use in evil packed rooms entity"));
  }

  private bool _crashOnFail = false;
  [SettingSubText("Crash the game when you have invalid templates. For mappers.")]
  public bool CrashOnFail{
    get=>_crashOnFail;
    set=>_crashOnFail=value;
  }

  /*private bool _hideHelperMaps = false;
  [SettingSubText("Choose rules for hiding maps below")]
  public bool HideHelperMaps {
    get=>_hideHelperMaps;
    set{
      _hideHelperMaps = value;
      if(value){
        if(hideRulesList.Count>0) MapHider.hideListed();
      }
      else if(MapHider.isHiding) MapHider.revealListed();
    }
  }*/
  public List<string> hideRulesList {get; set;} = new(["/t$"]);
  /*[SettingIgnore]
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
          s, (string value)=>{
            val = value;
          },(bool confirm)=>{
            if(confirm){
              rules[i] = val;
              MapHider.uncache();
              if(auspicioushelperModule.Settings._hideHelperMaps){
                MapHider.revealListed();
                MapHider.hideListed();
              }
            }
            else val = rules[i];
          }, 100, 0
        );
      });
      return entry;
    }
    public void CreateDummyEntry(TextMenuExt.SubMenu menu, bool ingame){
      rules = auspicioushelperModule.Settings.hideRulesList;
      rules.RemoveAll(string.IsNullOrEmpty);
      int i = 0;
      foreach(string s in rules){
        menu.Add(makePressHandler(s,i++));
      }
      rules.Add("");
      menu.Add(makePressHandler("",i));
    }
  }*/
}
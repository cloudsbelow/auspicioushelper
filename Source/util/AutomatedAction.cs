


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.auspicioushelper;

public class ScheduledAction{
  public HashSet<ActionList> enteredInto = new HashSet<ActionList>();
  public Func<bool> func;
  public string label;
  public static Dictionary<string,int> labels = new Dictionary<string, int>();
  public ScheduledAction(Func<bool> func, string label=""){
    if(labels.TryGetValue(label, out var l)){
      labels[label] = ++l;
      label = label+l.ToString();
    } else {
      labels[label]=0;
    }
    this.func=func;
    this.label=label;
  }
  public bool run(){
    bool b = func();
    if(b) remove();
    return b;
  }
  public void remove(){
    foreach(ActionList a in enteredInto){
      a.Remove(this);
    }
    enteredInto.Clear();
  }
}

public class ActionList{
  private HashSet<ScheduledAction> funcs=new HashSet<ScheduledAction>();
  private List<ScheduledAction> toRemove = new List<ScheduledAction>();
  public bool locked=false;
  public ActionList(){

  }
  public void enroll(ScheduledAction l){
    if(runimmediately){
      if(l.run()) return;
    }
    l.enteredInto.Add(this);
    funcs.Add(l);
  }
  public void run(){
    locked = true;
    foreach(var s in funcs){
      try {
        s.run();
      } catch(Exception ex) {
        DebugConsole.Write("Error occured in scheduled action "+s+": \n"+ex.ToString());
      }
    }
    foreach(var s in toRemove) funcs.Remove(s);
    locked=false;
  }
  public void Remove(ScheduledAction s){
    if(locked){
      toRemove.Add(s);
      return;
    }
    funcs.Remove(s);
  }
  bool runimmediately;
  public void setStatus(bool runimmediately){
    this.runimmediately=runimmediately;
  }
}

//use this for any hooks that we detatch on a roomly basis
public interface INeedHooks{
  static HookManager hooks;
  public static virtual void setupHooks(){
    hooks.enable();
  }
  public virtual void setupHooksInstance(){
    hooks.enable();
  }
}

public class HookManager{
  Action setup;
  Action unsetup=null;
  Func<bool> condunsetup=null;
  public bool active {get; private set;}
  ActionList autoclean;
  ScheduledAction s;
  static List<HookManager> allhooks=new List<HookManager>();
  public HookManager(Action setup, Action unsetup, ActionList autoclean=null, string label=""){
    this.setup = setup; this.unsetup = unsetup; this.autoclean=autoclean;
    if(autoclean != null) s=new ScheduledAction(simpleDisable, "Hook Manager "+label);
    allhooks.Add(this);
  }
  public HookManager(Action setup, Func<bool> unsetup, ActionList autoclean =null, string label=""){
    this.setup = setup; this.condunsetup = unsetup; this.autoclean=autoclean;
    if(autoclean != null) s=new ScheduledAction(condDisable, "Hook Manager "+label);
    allhooks.Add(this);
  }
  public HookManager enable(){
    if(active) return this;
    active = true;
    setup();
    if(autoclean != null) autoclean.enroll(s);
    return this;
  }
  public bool simpleDisable(){
    if(!active) return true;
    active = false;
    unsetup();
    return true;
  }
  public bool condDisable(){
    if(!active) return true;
    active=!condunsetup();
    return active;
  }
  public bool disable(){
    if(condunsetup != null) return active = !condDisable();
    else return simpleDisable();
  }
  public static void disableAll(){
    foreach(var h in allhooks){
      h.disable();
    }
  }
}
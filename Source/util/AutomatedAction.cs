


using System;
using System.Collections.Generic;

namespace Celeste.Mod.auspicioushelper;

public class ScheduledAction:List<ActionList>{
  public Func<bool> func;
  public Action act;
  public string label;
  public ScheduledAction(Func<bool> func, string label=""){
    this.label=label;
    this.func=func;
  }
  public ScheduledAction(Action act, string label=""){
    this.label = label;
    this.act = act;
  }
  public void run(){
    if(func is {} fn){
      if(fn()) remove();
    } else {
      act();
      if(this is not PersistantAction) remove();
    }
  }
  public void remove(){
    foreach(ActionList a in this) a.Remove(this);
    Clear();
  }
}
public class PersistantAction:ScheduledAction{
  public PersistantAction(Action a, string label=null):base(a, "Persistent action: "+label){}
}

public class ActionList(string label){
  List<ScheduledAction> funcs = new();
  HashSet<ScheduledAction> toRemove = new();
  public bool locked=false;
  public void enroll(ScheduledAction l){
    l.Add(this);
    funcs.Add(l);
  }
  public void enroll(Action a, string label="", bool persistent=false){
    enroll(persistent? new PersistantAction(a, label) : new ScheduledAction(a,label));
  }
  public void run(){
    locked = true;
    foreach(var s in funcs){
      try {
        s.run();
      } catch(Exception ex) {
        DebugConsole.WriteFailure($"Error occured in {label} with label {s.label}: \n"+ex.ToString());
        if(ex is DebugConsole.PassingException p) throw p;
      }
    }
    funcs.RemoveAll(toRemove.Contains);
    toRemove.Clear();
    locked=false;
  }
  public void Remove(ScheduledAction s){
    if(locked) toRemove.Add(s); 
    else funcs.Remove(s);
  }
}

public class HookManager{
  Action setup, unsetup;
  ResetEvents.Times cleanWhen;
  ScheduledAction s;
  static List<HookManager> allhooks=new List<HookManager>();
  public HookManager(Action setup, Action unsetup, ResetEvents.Times disableTime = ResetEvents.Times.None, string label=""){
    this.setup = setup; this.unsetup = unsetup; this.cleanWhen=disableTime;
    allhooks.Add(this);
  }
  public HookManager(Action setup,  ResetEvents.Times disableTime, string label=""):
    this(setup, null, disableTime, label){}
  public HookManager enable(){
    if(s!=null) return this;
    setup();
    s = new ScheduledAction(disable);
    foreach(var r in ResetEvents.getLists(cleanWhen)) r.enroll(s);
    return this;
  }
  public void disable(){
    if(s==null) return;
    unsetup();
    s=null;
  }
  public static ActionList cleanupActions = new("Unload Cleanup");
  public static void disableAll(){
    foreach(var h in allhooks) h.disable();
    cleanupActions.run();
  }
}



using System;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateDashhitModifier")]
[Tracked]
public class TemplateDashhitModifier:Template, ITemplateTriggerable{
  enum Dir{
    Left, Right, Up, Down
  }
  [Flags]
  enum Result{
    Normal=1, Bounce=2, Rebound=4, Bumper=8,
    Trigger=16,
    NormalTrigger=Normal|Trigger, BounceTrigger=Bounce|Trigger, ReboundTrigger=Rebound|Trigger, BumperTrigger=Bumper|Trigger,
    BouceTrigger=BounceTrigger //typo compat
  }
  static Dir getDir(Vector2 v){
    if(v.X!=0) return v.X>0?Dir.Right:Dir.Left;
    return v.Y>0?Dir.Down:Dir.Up;
  }
  static int dirToInt(Dir d){
    return d switch{
      Dir.Left=>0, Dir.Right=>1, Dir.Up=>2, Dir.Down=>3, _=>throw new System.Exception("bonkj")
    };
  }
  Result[] res = new Result[4];
  int refillDashesOnTrigger = 0;
  bool alwaysRefill = false;
  bool refillStamina = false;
  public void OnTrigger(TriggerInfo s) {
    //DebugConsole.Write("dashed", s, s?.entity, refillDashesOnTrigger);
    if(s.shouldTrigger && (refillDashesOnTrigger>0||refillStamina)){
      if(((s.entity as Player)??(alwaysRefill?UpdateHook.cachedPlayer:null)) is Player p){
        p.Dashes = Math.Max(p.Dashes,refillDashesOnTrigger);
        if(refillStamina) p.RefillStamina();
      }
    }
    parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(s);
  }
  public class DashhitInfo:TriggerInfo{
    Dir dir;
    public DashhitInfo(Player p, Vector2 d, Template t):base(){
      entity = p; 
      dir=getDir(d);
      parent = t;
    }
    public override string category=>"dashHit/"+dir.ToString();
  }
  string skipCh;
  bool skip=false;
  bool alwaysLetThrough=true;
  string echannel;
  ChannelTracker etracker;
  static bool thing=false;
  bool checkEntanglement(TemplateDashhitModifier other){
    if(string.IsNullOrWhiteSpace(echannel) || other.skip) return false;
    return etracker!=null? etracker.value==other.etracker?.value : echannel==other.echannel;
  }
  public TemplateDashhitModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateDashhitModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    OnDashCollide = (Player p, Vector2 dir)=>{
      if(!skip){
        if(!string.IsNullOrWhiteSpace(echannel) && !thing) using(Util.WithRestore(ref thing, true)){
          foreach(TemplateDashhitModifier o in Scene.Tracker.GetEntities<TemplateDashhitModifier>()){
            if(checkEntanglement(o)) o.OnDashCollide(p,dir);
          }
        }
        Result d = res[dirToInt(getDir(dir))];
        if(d.HasFlag(Result.Trigger)) OnTrigger(new DashhitInfo(p,dir,this));
        if(alwaysLetThrough && !d.HasFlag(Result.Normal)) (this as ITemplateChild).propagateDashhit(p,dir);
        if(d.HasFlag(Result.Bumper)) {
          p.ExplodeLaunch(p.Center+dir*6,false,false);
          Audio.Play("event:/game/09_core/pinballbumper_hit", p.Position);
          return DashCollisionResults.Ignore;
        }
        if(d.HasFlag(Result.Rebound)) return DashCollisionResults.Rebound;
        if(d.HasFlag(Result.Bounce)) return DashCollisionResults.Bounce;
      }
      return (this as ITemplateChild).propagateDashhit(p,dir);
    };
    skipCh = d.Attr("skipChannel");
    res[dirToInt(Dir.Left)] = d.Enum("Left",Result.Normal);
    res[dirToInt(Dir.Right)] = d.Enum("Right",Result.Normal);
    res[dirToInt(Dir.Up)] = d.Enum("Up",Result.Normal);
    res[dirToInt(Dir.Down)] = d.Enum("Down",Result.Normal);
    string dstr = d.Attr("refillOptions");
    refillStamina = dstr.Contains('s');
    alwaysRefill = dstr.Contains('p');
    var match = Regex.Match(dstr, @"\d+");
    if (match.Success) int.TryParse(match.Value,out refillDashesOnTrigger);
    alwaysLetThrough = d.Bool("alwaysPropegate",false);
    echannel = d.Attr("entanglementId","");
    if(echannel.StartsWith('@')) Add(etracker = new(echannel.Substring(1)));
  }
  public override void addTo(Scene scene) {
    base.addTo(scene);
    if(!string.IsNullOrWhiteSpace(skipCh)) Add(new ChannelTracker(skipCh,(double val)=>skip=val!=0,true));
  }
}
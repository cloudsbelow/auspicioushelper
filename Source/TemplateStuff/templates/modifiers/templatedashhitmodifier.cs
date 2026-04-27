


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
    Normal=1, Bounce=2, Rebound=4, Bumper=8, Reflect=16,Ignore=32,
    Trigger=64, Pass = -1,Block = -2, 
    NormalTrigger=Normal|Trigger, BounceTrigger=Bounce|Trigger, ReboundTrigger=Rebound|Trigger, 
    BumperTrigger=Bumper|Trigger, ReflectTrigger=Reflect|Trigger, IgnoreTrigger=Ignore|Trigger,
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
    if(!TriggerInfo.TestPass(s,this)) return;
    if(refillDashesOnTrigger>0||refillStamina){
      if(((s?.entity as Player)??(alwaysRefill?UpdateHook.cachedPlayer:null)) is Player p){
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
  ChannelState.BoolCh skip;
  bool alwaysLetThrough=true;
  string echannel;
  ChannelTracker etracker;
  static bool thing=false;
  string sfx;
  bool checkEntanglement(TemplateDashhitModifier other){
    if(string.IsNullOrWhiteSpace(echannel) || other.skip) return false;
    return etracker!=null? etracker.value==other.etracker?.value : echannel==other.echannel;
  }
  public TemplateDashhitModifier(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateDashhitModifier(EntityData d, Vector2 offset, int depthoffset)
  :base(d,offset+d.Position,depthoffset){
    OnDashCollide = (Player p, Vector2 dir)=>{
      Result d = res[dirToInt(getDir(dir))];
      if(!skip){
        if(d == Result.Block) return DashCollisionResults.NormalOverride;
        if(d == Result.Pass) return parent?.GetFromTree<TemplateDashhitModifier>(Propagation.DashHit)?.OnDashCollide(p,dir)??DashCollisionResults.NormalOverride;

        if(!string.IsNullOrWhiteSpace(echannel) && !thing) using(Util.WithRestore(ref thing, true)){
          foreach(TemplateDashhitModifier o in Scene.Tracker.GetEntities<TemplateDashhitModifier>()){
            if(checkEntanglement(o)) o.OnDashCollide(p,dir);
          }
        }
        if(d.HasFlag(Result.Trigger)) OnTrigger(new DashhitInfo(p,dir,this));
        if(!d.HasFlag(Result.Normal)){
          var old = p.Collider;
          p.Collider=p.hurtbox;
          foreach(PlayerCollider c in p.Scene.Tracker.GetComponents<PlayerCollider>()) if(c.Check(p) && p.Dead) break;
          p.Collider=old;
          if(alwaysLetThrough) (this as ITemplateChild).propagateDashhit(p,dir);
        }
        Audio.Play(sfx, p.Position);
        if(d.HasFlag(Result.Bumper)) {
          p.ExplodeLaunch(p.Center+dir*6,false,false);
          return DashCollisionResults.Ignore;
        }
        if(d.HasFlag(Result.Rebound)) return DashCollisionResults.Rebound;
        if(d.HasFlag(Result.Bounce)) return DashCollisionResults.Bounce;
        if(d.HasFlag(Result.Ignore)) return DashCollisionResults.Ignore;
        if(d.HasFlag(Result.Reflect)){
          p.Speed-=2*dir*dir*p.Speed;
          p.DashDir-=2*dir*dir*p.DashDir;
          if(p.StateMachine.State==Player.StDash)p.StateMachine.State=Player.StNormal;
          return DashCollisionResults.Ignore;
        }
      }
      return (this as ITemplateChild).propagateDashhit(p,dir);
    };
    skip = d.ChannelBool("skipChannel",false);
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
    sfx = d.Attr("sfx","event:/game/06_reflection/crushblock_activate");
  }
}
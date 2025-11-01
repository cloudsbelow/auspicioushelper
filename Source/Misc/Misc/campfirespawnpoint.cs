


using System;
using System.Collections;
using System.Threading;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/CampfireRespawn")] 
public class CampfireThing:Entity{
  public class RespawnData{
    public string level;
    public Vector2 loc;
    public Vector2 floc;
  }
  class AppearCutscene:CutsceneEntity{
    Sprite sprite;
    Player p;
    CampfireThing c;
    bool fromRespawn=false;
    public AppearCutscene(Player p, CampfireThing c){
      this.p=p;
      this.c=c;
      Position = p.Position+(int)p.Facing*8*Vector2.UnitX;
      Add(sprite = GFX.SpriteBank.Create("campfire"));
    }
    public AppearCutscene(Vector2 loc){
      Position = loc;
      fromRespawn=true;
      Add(sprite = GFX.SpriteBank.Create("campfire"));
    }
    public override void OnBegin(Level level) {
      DebugConsole.Write("StartingCutscene");
      if(!fromRespawn)c.playingCutscene=true;
      Add(new Coroutine(fromRespawn?RespawnSeq(level):Seq(level)));
    }
    public override void OnEnd(Level level) {
      if(!fromRespawn){
        c.playingCutscene=false;
        level.Session.RespawnPoint=p.Position;
        auspicioushelperModule.Session.respDat = new(){
          level=level.Session.Level, loc=p.Position, floc=Position
        };
        auspicioushelperModule.Session.save();
        p.DummyAutoAnimate=true;
        p.StateMachine.State=Player.StNormal;
        if(c.channel!=null) ChannelState.SetChannel(c.channel,ChannelState.readChannel(c.channel)-1);
      }
      if(WasSkipped) Add(new Coroutine(EndSeq()));
    }
    IEnumerator RespawnSeq(Level l){
      EndCutscene(l,false);
      sprite.Play("burnDream");
      yield return 1f;
      yield return EndSeq();
    }
    IEnumerator Seq(Level l){
      p.StateMachine.State=Player.StDummy;
      p.DummyAutoAnimate=false;
      l.Displacement.AddBurst(Position, 0.8f, 8f, 48f,0.5f);
      Audio.Play("event:/char/badeline/disappear", Position);
      yield return 0.3f;
      p.Sprite.Play("duck");
      yield return 0.2f;
      Audio.Play("event:/game/02_old_site/sequence_badeline_intro", Position);
      Audio.Play("event:/env/local/campfire_start", Position);
      sprite.Play("start");
    
      yield return 0.5f;
      p.Sprite.Play("idle");
      yield return 1f;
      EndCutscene(l,false);
      yield return EndSeq();
    }
    IEnumerator EndSeq(){
      for(float a=1; a>0; a-=Engine.DeltaTime){
        sprite.Color=new Color(a,a,a,a);
        yield return null;
      }
      Remove(sprite);
      RemoveSelf();
    }
  }

  float duckTime;
  string channel;
  bool disableNormal=true;
  public CampfireThing(EntityData d, Vector2 o):base(d.Position+o){
    duckTime = d.Float("duckTime",2);
    channel = d.tryGetStr("channel",out var s)?s:null;
    disableNormal = d.Bool("disableNormal",true);
  }
  bool playingCutscene = false;
  float timer;
  public override void Awake(Scene scene) {
    if(disableNormal && auspicioushelperModule.Session.respDat==null){
      Session s = (scene as Level).Session;
      Vector2 orig = s.LevelData.Spawns.ClosestTo(scene.Tracker.GetEntity<Player>().Position);
      DebugConsole.Write("session lev",s.Level,s.LevelData.Name);
      auspicioushelperModule.Session.respDat = new(){
        loc = orig, floc = orig+8*Vector2.UnitX, level = s.Level
      };
    }
  }
  public override void Update() {
    base.Update();
    if(UpdateHook.cachedPlayer is {} p && p.OnSafeGround && p.Ducking && Input.MoveY.Value==1 && !playingCutscene){
      if(duckTime>=0 && timer>=duckTime){
        if(channel==null || ChannelState.readChannel(channel)>0){
          Scene.Add(new AppearCutscene(UpdateHook.cachedPlayer,this));
        } 
      }
      timer+=Engine.DeltaTime;
    } else timer=0;
  }
  static void Hook(On.Celeste.Level.orig_Reload orig, Level s){
    if(auspicioushelperModule.Session?.respDat is {} r){
      s.Session.Level=r.level; 
      s.Session.RespawnPoint=r.loc;
      orig(s);
      var ev = Audio.Play("event:/char/badeline/appear",r.floc);
      ev.setVolume(0.5f);
      s.Add(new AppearCutscene(r.floc));
    } else orig(s);
  }
  [OnLoad]
  public static HookManager hooks = new(()=>{
    On.Celeste.Level.Reload+=Hook;
  },()=>{
    On.Celeste.Level.Reload-=Hook;
  });
}
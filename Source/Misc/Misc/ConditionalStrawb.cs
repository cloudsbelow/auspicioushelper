


using System;
using System.Collections;
using System.Linq;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/ConditionalStrawbTracked")]
[RegisterStrawberry(true, true)]
public class ConditionalStrawbTracked:ConditionalStrawb{
  public ConditionalStrawbTracked(EntityData data, Vector2 offset, EntityID id):base(data,offset,id){}
}
[CustomEntity("auspicioushelper/ConditionalStrawb")]
[RegisterStrawberry(false, true)]
public class ConditionalStrawbUntracked:ConditionalStrawb{
  public ConditionalStrawbUntracked(EntityData data, Vector2 offset, EntityID id):base(data,offset,id){}
}


[Tracked(true)]
public class ConditionalStrawb:Entity, IStrawberry{
  public string idstr;
  public Follower follower;
  public EntityID id;
  public bool isGhost;
  public bool wingedFollower;
  public bool winged;
  Vector2? otherpos;
  public float ispeed;
  public float maxspeed;
  public float acceleration;
  public bool golden;
  public enum Strawb {
    hidden, idling, following, flying, gone, collected
  }
  public Strawb state;
  public Sprite sprite;
  public EntityData data;
  public bool deathless;
  public bool persistOnDie;
  VertexLight light;
  Tween lightTween;
  public ConditionalStrawb(EntityData data, Vector2 offset, EntityID id):base(data.Position+offset){
    idstr = data.Attr("strawberry_id");
    this.id=id;
    isGhost = SaveData.Instance.CheckStrawberry(id);
    base.Collider = new Hitbox(14f, 14f, -7f, -7f);
    Add(new PlayerCollider(OnPlayer));
    Add(new MirrorReflection());
    Add(light = new VertexLight(Color.White, 1f, 16, 24));
    light.Visible=false;
    Add(lightTween = light.CreatePulseTween());

    Add(follower = new Follower(id, null, OnLoseLeader));
    string spritestr = data.Attr("sprites","auspicioushelper_conditionalstrawb");
    if(string.IsNullOrWhiteSpace(spritestr)) spritestr = "auspicioushelper_conditionalstrawb";
    Add(sprite = GFX.SpriteBank.Create(spritestr));
    follower.FollowDelay = 0.3f;
    otherpos=data.Nodes.Count() == 0?null:data.Nodes[0]+offset;

    wingedFollower= data.Bool("wingedfollower",true);
    winged = data.Bool("winged",true);
    ispeed=data.Float("ispeed",1.0f);
    maxspeed=data.Float("maxspeed",240.0f);
    acceleration=data.Float("acceleration",240f);
    this.data=data;

    state = Strawb.hidden;
    deathless=data.Bool("deathless",false);
    persistOnDie = data.Bool("persist_on_death",false);
  }
  public void handleAppearance(EntityData e){
    if(!e.Bool("appear_on_ch",false)){
      Appear();
      return;
    }
    int v=e.Int("appear_chvalue",0);
    string ch = e.Attr("appear_channel","");
    bool c=ChannelState.readChannel(ch)==v;
    if(c || e.Bool("appear_roomenter_only",true)){
      if(c) Appear();
    } else Add(new ChannelTracker(ch,(int val)=>{
      if(val==v && state==Strawb.hidden)Appear();
    }));
  }
  public void Appear(){
    state=Strawb.idling;
    Collidable = true;
    sprite.Visible=true;
    light.Visible=true;
    lightTween.Start();
    Add(new DashListener(OnDash));
    if(data.Bool("fly_on_ch",false)){
      string ch = data.Attr("fly_channel","");
      int v = data.Int("fly_value",1);
      if(ChannelState.readChannel(ch) == v) Fly();
      Add(new ChannelTracker(ch,(int val)=>{
        if(val == v) Fly();
      }));
    }
  }
  public override void Added(Scene scene){
    base.Added(scene);
    Collidable=false;
    sprite.Visible=false;
    if(isGhost) sprite.Color = new Color(100,100,255,144);
    if(state == Strawb.following){
      Appear();
      Collidable=false;
      state=Strawb.following;
    } else {
      handleAppearance(data);
    }
  }
  public void Fly(){
    Add(new Coroutine(Fly(otherpos??new Vector2(Position.X,Position.Y-1000000))));
  }
  public void OnDash(Vector2 dir){
    if(winged && state==Strawb.idling && (((otherpos is Vector2 o) &&Vector2.Distance(o,Position)>1.0f)||otherpos==null)){
      Fly();
    }
    if(wingedFollower && state == Strawb.following){
      Fly();
    }
  }
  public void OnPlayer(Player p){
    state=Strawb.following;
    Collidable=false;
    Audio.Play(isGhost ? "event:/game/general/strawberry_blue_touch" : "event:/game/general/strawberry_touch", Position);
    p.Leader.GainFollower(follower);
    if(persistOnDie){
      Session session = SceneAs<Level>().Session;
      session.DoNotLoad.Add(id);
      auspicioushelperModule.Session.PersistentFollowers.Add(new auspicioushelperModuleSession.EntityDataId(data,id));
    }
  }
  public void OnCollect(){
    if(state != Strawb.following) return;
    Player p = follower.Leader.Entity as Player;
    int idx = p.StrawberryCollectIndex;
    p.StrawberryCollectIndex++;
    p.StrawberryCollectResetTimer = 2.5f;
    detatch(true);
    SaveData.Instance.AddStrawberry(id, golden);
    state=Strawb.collected;
    Session session = (base.Scene as Level).Session;
    session.DoNotLoad.Add(id);
    session.Strawberries.Add(id);
    Add(new Coroutine(CollectRoutine(idx)));
  }
  public IEnumerator CollectRoutine(int idx){
    Tag = Tags.TransitionUpdate;
    Audio.Play("event:/game/general/strawberry_get", Position, "colour", 1, "count", idx);
    sprite.Play("collect");
    while (sprite.Animating){
        yield return null;
    }
    Scene.Add(new StrawberryPoints(Position, isGhost, idx, false));
    RemoveSelf();
  }
  public void OnLoseLeader(){
    if(state==Strawb.collected) return;

  }
  public void detatch(bool doNotRestore=false){
    follower.Leader.Followers.Remove(follower);
    follower.OnLoseLeaderUtil();
    follower.Leader=null;
    if(persistOnDie){
      Session session = (base.Scene as Level).Session;
      if(!doNotRestore)session.DoNotLoad.Remove(id);
      foreach(var e in auspicioushelperModule.Session.PersistentFollowers){
        if(e.data.ID == data.ID){
          auspicioushelperModule.Session.PersistentFollowers.Remove(e);
          break;
        }
      }
    }
  }
  public IEnumerator Fly(Vector2 target){
    if(state==Strawb.following)detatch();
    state = Strawb.flying;
    yield return 0.1f;
    Audio.Play("event:/game/general/strawberry_laugh", Position);
    yield return 0.2f;
    if (!follower.HasLeader){
      Audio.Play("event:/game/general/strawberry_flyaway", Position);
    }
    float speed=ispeed;
    while(true){
      Position = Calc.Approach(Position,target,speed*Engine.DeltaTime);
      if(speed<maxspeed){
        speed=Calc.Approach(speed, maxspeed, acceleration*Engine.DeltaTime);
      }
      if(Vector2.Distance(Position,target)< 0.1f){
        break;
      }
      if(Position.Y<(float)SceneAs<Level>().Bounds.Top -16 ||
         Position.Y>(float)SceneAs<Level>().Bounds.Bottom +16 ||
         Position.X<(float)SceneAs<Level>().Bounds.Left -16 ||
         Position.X>(float)SceneAs<Level>().Bounds.Right +16){
        RemoveSelf();
        state=Strawb.gone;
        yield break;
      }
      yield return null;
    }
    state=Strawb.idling;
    Collidable = true;
  }
  public static void handleDie(Player p){
    foreach(Follower f in p.Leader.Followers){
      if(f.Entity is ConditionalStrawb s && s.deathless){
        Engine.Scene = new LevelExit(LevelExit.Mode.GoldenBerryRestart, p.level.Session){
          GoldenStrawberryEntryLevel = s.id.Level
        };
        return;
      }
    }
  }
  public static void restoreFollowerLikeJesus(Player p, auspicioushelperModuleSession.EntityDataId e){
      var ent = new ConditionalStrawb(e.data, new Vector2(0,0), e.id);
      p.Leader.GainFollower(ent.follower);
      ent.state=Strawb.following;
      ent.Position = p.Position;
      Engine.Instance.scene.Add(ent);
  }
  public static void playerCtorHook(On.Celeste.Player.orig_ctor orig, Player p, Vector2 pos, PlayerSpriteMode s){
    orig(p,pos,s);
    //DebugConsole.Write("here in strawb");
    foreach(var e in auspicioushelperModule.Session.PersistentFollowers){
      if(e.data.Name == "auspicioushelper/ConditionalStrawb") restoreFollowerLikeJesus(p,e);
    }
  }
  public static HookManager hooks = new HookManager(()=>{
    On.Celeste.Player.ctor += playerCtorHook;
  }, void ()=>{
    On.Celeste.Player.ctor -= playerCtorHook;
  });
}
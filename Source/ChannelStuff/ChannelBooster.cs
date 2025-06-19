using System;
using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod.Entities;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Monocle;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.auspicioushelper.Wrappers;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.auspicioushelper;


[Tracked]
[CustomEntity("auspicioushelper/ChannelBooster")]
public class ChannelBooster : ChannelBaseEntity, IMaterialEnt, ISimpleEnt, IBooster{
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0) Visible=vis>0;
    if(col!=0) Collidable=col>0;
    if(act!=0) Active=act>0;
  }
  public Vector2 toffset {get;set;}
  public Template parent {get;set;}
  public void setOffset(Vector2 offset){
    toffset = Position-offset;
  }
  public void relposTo(Vector2 pos, Vector2 liftspeed){
    Vector2 npos = pos+toffset;
    if(outline!=null) outline.Position = npos;
    Vector2 poffset=insideplayer!=null?insideplayer.ExactPosition-Position:Vector2.Zero;
    if(!BoostingPlayer && sprite!=null && sprite.CurrentAnimationID != "pop") Position = npos;
    if(insideplayer!=null){
      insideplayer.MoveTowardsX(npos.X+poffset.X,100);
      insideplayer.MoveTowardsY(npos.Y+poffset.Y,100);
      insideplayer.boostTarget = npos;
      if(givels)insideplayer.LiftSpeed = liftspeed;
    }
  }
  public bool hasRiders<T>() where T:Actor{
    return typeof(T)==typeof(Player) && insideplayer!=null;
  }
  public void destroy(bool particles){
    RemoveSelf();
    outline.RemoveSelf();
  }
  public static ParticleType P_Burst = new ParticleType{
    Source = GFX.Game["particles/blob"],
    Color = Calc.HexToColor("2c956e"),
    FadeMode = ParticleType.FadeModes.None,
    LifeMin = 0.5f,
    LifeMax = 0.8f,
    Size = 0.7f,
    SizeRange = 0.25f,
    ScaleOut = true,
    Direction = 4.712389f,
    DirectionRange = 0.17453292f,
    SpeedMin = 10f,
    SpeedMax = 20f,
    SpeedMultiplier = 0.01f,
    Acceleration = new Vector2(0f, 90f)
  };
  public static ParticleType P_Appear =new ParticleType{
    Size = 1f,
    Color = Calc.HexToColor("4ACFC6"),
    DirectionRange = (float)Math.PI / 30f,
    LifeMin = 0.6f,
    LifeMax = 1f,
    SpeedMin = 40f,
    SpeedMax = 50f,
    SpeedMultiplier = 0.25f,
    FadeMode = ParticleType.FadeModes.Late
  };
  public static readonly Vector2 playerOffset = new Vector2(0f, -2f);
  private Sprite sprite;
  private Sprite innersprite;
  private Entity outline;
  private BloomPoint bloom;
  //private VertexLight light;
  private Coroutine dashRoutine;
  private DashListener dashListener;
  private ParticleType particleType;
  private float respawnTimer;
  private float cannotUseTimer;
  public bool BoostingPlayer { get; private set; }

  public enum BoosterType {
    normal,
    reversed,
    none,
  }
  public static string getSpriteString(BoosterType t, EntityData d, int i){
    string s = i==0?"state0_customsprite":"state1_customsprite";
    if(!string.IsNullOrWhiteSpace(d.Attr(s,""))) return d.Attr(s);
    return t switch{
      BoosterType.reversed => "auspicioushelper_whiteboosterbasic",
      BoosterType.none => "auspicioushelper_noneboosterbasic",
      _=>"auspicioushelper_blackboosterbasic"
    };
  }
  public static Color getMaterialColor(BoosterType t){
    //state[currentState]==BoosterType.normal?:
    return t switch{
      BoosterType.reversed => new Color(18,180,255,255),
      BoosterType.none => new Color(17,110,140,255),
      _=>new Color(16,0,50,255)
    };
  }
  public BoosterType[] state = new BoosterType[2];
  public Sprite[] bgsprites = new Sprite[2];
  public int currentState;
  public bool dirty;
  public bool selfswitching;
  public int id;
  public static int idctr = 0;
  public Color iinnerColor;
  bool trigger;
  bool givels;



  public ChannelBooster(EntityData data, Vector2 offset, EntityID _id): base(data.Position+offset){
    base.Depth = -8500;
    base.Collider = new Circle(10f, 0f, 2f);
    Add(new PlayerCollider(OnPlayer));
    //Add(light = new VertexLight(Color.White, 1f, 16, 32));
    Add(bloom = new BloomPoint(0.1f, 16f));
    Add(dashRoutine = new Coroutine(removeOnComplete: false));
    Add(dashListener = new DashListener());
    Add(new MirrorReflection());
    dashListener.OnDash = OnPlayerDashed;
    particleType = P_Burst;
    
    state[0] = data.Attr("state0","normal") switch {
      "reversed"=>BoosterType.reversed,
      "none"=>BoosterType.none,
      _=>BoosterType.normal
    };
    state[1] = data.Attr("state1","normal") switch {
      "reversed"=>BoosterType.reversed,
      "none"=>BoosterType.none,
      _=>BoosterType.normal
    };
    for(int i=0; i<2; i++){
      Add(bgsprites[i] = GFX.SpriteBank.Create(getSpriteString(state[i],data,i)));
    }
    Add(innersprite = GFX.SpriteBank.Create("auspicioushelper_genericboostermat"));
    innersprite.Visible = false;
    channel = data.Attr("channel","");
    selfswitching = data.Bool("self_activating", false);
    id=idctr++;
    trigger = data.Bool("triggerTemplates",false);
    givels = data.Bool("give_liftspeed",false);
  }
  public override void Added(Scene scene)
  {
    base.Added(scene);
    layerA?.addEnt(this);
    Image image = new Image(GFX.Game["objects/booster/outline"]);
    image.CenterOrigin();
    image.Color = Color.White * 0.75f;
    outline = new Entity(Position);
    outline.Depth = 8999;
    outline.Visible = false;
    outline.Add(image);
    outline.Add(new MirrorReflection());
    scene.Add(outline);
    
    setChVal(ChannelState.readChannel(channel));
  }
  Player insideplayer;
  private void OnPlayer(Player player){
    if (respawnTimer <= 0f && cannotUseTimer <= 0f && !BoostingPlayer)
    {
      insideplayer=player;
      cannotUseTimer = 0.45f;
      IBooster.startBoostPlayer(player, this);
      Audio.Play("event:/game/04_cliffside/greenbooster_enter", Position);
      sprite.Play("inside");
      if(trigger) parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(null);
      //sprite.FlipX = player.Facing == Facings.Left;
    }
  }
  public void PlayerBoosted(Player player, Vector2 direction){
    Audio.Play("event:/game/04_cliffside/greenbooster_dash", Position);
    BoostingPlayer = true;
    base.Tag = (int)Tags.Persistent | (int)Tags.TransitionUpdate;
    sprite.Play("spin");
    //sprite.FlipX = player.Facing == Facings.Left;
    outline.Visible = true;
    dashRoutine.Replace(BoostRoutine(player, direction));
    if(state[currentState] == BoosterType.reversed){
      player.DashDir*=-1; 
      player.Speed*=-1;
    }
    if(givels){
      if(parent != null && parent.gatheredLiftspeed != Vector2.Zero){
        player.Speed+=parent.gatheredLiftspeed;
        player.LiftSpeed = parent.gatheredLiftspeed;
      } else if(parent != null && player.liftSpeedTimer>0){
        player.Speed+=player.LiftBoost;
      }
    }
    if(selfswitching){
      ChannelState.SetChannel(channel, 1-currentState);
    }
    insideplayer=null;
  }

  private IEnumerator BoostRoutine(Player player, Vector2 dir){
    float angle = (-dir).Angle();
    while ((player.StateMachine.State == 2 || player.StateMachine.State == 5) && BoostingPlayer){
      sprite.RenderPosition = player.Center + playerOffset;
      if (base.Scene.OnInterval(0.02f)){
        (base.Scene as Level).ParticlesBG.Emit(particleType, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f), angle);
      }
      yield return null;
    }
    PlayerReleased();
    if (player.StateMachine.State == 4){
      sprite.Visible = false;
    }
    while (SceneAs<Level>().Transitioning){
      yield return null;
    }
    base.Tag = 0;
  }
  public void OnPlayerDashed(Vector2 direction){
    if (BoostingPlayer){
        BoostingPlayer = false;
    }
  }
  public void PlayerReleased(){
    Audio.Play("event:/game/04_cliffside/greenbooster_end", sprite.RenderPosition);
    sprite.Play("pop");
    cannotUseTimer = 0f;
    respawnTimer = 1f;
    BoostingPlayer = false;
    insideplayer = null;
  }
  public void PlayerDied(){
    if (BoostingPlayer){
      PlayerReleased();
      dashRoutine.Active = false;
      base.Tag = 0;
    }
    insideplayer=null;
  }
  public void Respawn(bool remanifest, bool change){
    
    sprite.Position = Vector2.Zero;
    innersprite.Position = Vector2.Zero;
    sprite.Play("loop", restart: true);
    sprite.Visible = true;
    outline.Visible = false;
    if(parent!=null) Position = parent.virtLoc+toffset;
    if(remanifest){
      Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
      ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
      for (int i = 0; i < 360; i += 30){
        particlesBG.Emit(P_Appear, 1, base.Center, Vector2.One * 2f, (float)i * ((float)Math.PI / 180f));
      }
    }
  }
  public override void Update(){
    base.Update();
    if (cannotUseTimer > 0f){
      cannotUseTimer -= Engine.DeltaTime;
    }
    if (respawnTimer > 0f){
      respawnTimer -= Engine.DeltaTime;
      if (respawnTimer <= 0f){
        Respawn(true, false);
      }
    }
    if (!dashRoutine.Active && respawnTimer <= 0f){
      Vector2 target = Vector2.Zero;
      Player entity = base.Scene.Tracker.GetEntity<Player>();
      if (entity != null && CollideCheck(entity)){
        target = entity.Center + playerOffset - Position;
      }
      sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
    }
    if (sprite.CurrentAnimationID == "inside" && !BoostingPlayer && !CollideCheck<Player>()){
      sprite.Play("loop");
    }
    if(dirty && !BoostingPlayer) setChVal(ChannelState.readChannel(channel));
  }
  public override void setChVal(int val){
    if(dirty = BoostingPlayer) return;
    currentState = val & 1;
    
    bgsprites[1-currentState].Visible=false;
    sprite = bgsprites[currentState];
    cannotUseTimer = 0;
    if(state[currentState] == BoosterType.none){
      sprite.Visible = false;
      outline.Visible = true;
      Collidable=false;
    } else {
      sprite.Visible=true;
      Collidable=true;
      Respawn(respawnTimer>0, true);
    }
    innersprite.Color = getMaterialColor(state[currentState]);
    iinnerColor = getMaterialColor(state[1-currentState]);
    respawnTimer = 0;
  }
  public override void Render(){
    //Draw.Circle(Position+new Vector2(10,10), 15, Color.White, 1);
    Vector2 position = sprite.Position;
    sprite.Position = position.Floor();
    base.Render();
    sprite.Position = position;
  }
  public void renderMaterial(IMaterialLayer l, Camera c){
    if(respawnTimer<=0 && sprite.Visible){
      innersprite.Position = sprite.Position.Floor();
      innersprite.Render();
    }
    // Vector2 pos = Position-new Vector2(2,2);
    // if(state[currentState]!=BoosterType.none)pos+=sprite.Position;
    // pos=pos.Floor();
    // sb.Draw(Draw.Pixel.Texture.Texture_Safe,new Rectangle(
    //   (int)pos.X,(int)pos.Y, 4,4
    // ),Draw.Pixel.ClipRect,iinnerColor);
  }
}


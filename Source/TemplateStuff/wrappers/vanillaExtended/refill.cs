


using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

// public class RefillW:Refill,ISimpleEnt {
//   public Template parent {get;set;}
//   public Template.Propagation prop=>Template.Propagation.None;
//   public float respawnTime = 2.5f;
//   public bool triggering;
//   public RefillW(EntityData d, Vector2 offset):base(d,offset){
//     hooks.enable();
//     respawnTime = d.Float("respawnTimer",2.5f);
//     if(respawnTime!=2.5f) Get<PlayerCollider>().OnCollide = CustomOnPlayer;
//   }
//   public Vector2 toffset {get;set;}
//   public void setOffset(Vector2 ppos){
//     toffset = Position-ppos;
//     Depth+=parent.depthoffset;
//   }
//   public void relposTo(Vector2 loc, Vector2 ls){
//     Position = toffset+loc;
//   }

//   bool selfCol = true;
//   bool parentCol = true;
//   public void parentChangeStat(int vis, int col, int act){
//     if(vis!=0)Visible = vis>0;
//     if(col!=0){
//       parentCol = col>0;
//       if(col>0)Collidable = selfCol;
//       else{
//         selfCol=Collidable;
//         Collidable = false;
//       }
//     }
//     if(act!=0) Active = act>0;
//   }
//   void CustomOnPlayer(Player p){
//     if (p.UseRefill(twoDashes)){
//       Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", Position);
//       Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
//       Collidable = false;
//       Add(new Coroutine(RefillRoutine(p)));
//       respawnTimer = respawnTime;
//     }
//   }

//   static void respawnHook(On.Celeste.Refill.orig_Respawn orig, Refill self){
//     if(self is RefillW rw){
//       rw.Collidable = false;
//       orig(rw);
//       rw.selfCol = true;
//       rw.Collidable = rw.parentCol;
//       rw.Depth+=rw.parent?.depthoffset??0;
//     } else orig(self);
//   }
//   static HookManager hooks = new HookManager(()=>{
//     On.Celeste.Refill.Respawn+=respawnHook;
//   }, void ()=>{
//     On.Celeste.Refill.Respawn-=respawnHook;
//   });
// }


// Extending the base refill caused bad behavior with some unspecified mods loaded
// Therefore we do a bit of ctrl+C, ctrl+V
[CustomEntity("auspicioushelper/CustomRefill")]
public class RefillW2 : Entity, ISimpleEnt, TemplateHoldable.IPickupChild{
  public Template parent {get;set;}
  public Vector2 toffset {get;set;}
  public Sprite sprite;
  public Sprite flash;
  public Image outline;
  public Wiggler wiggler;
  public BloomPoint bloom;
  public VertexLight light;
  public Level level;
  public SineWave sine;
  public bool twoDashes;
  public bool oneUse;
  public ParticleType p_shatter;  
  public ParticleType p_regen;
  public ParticleType p_glow;
  public float respawnTimer;
  public float respawnTime;
  public bool triggering;
  bool useOnPickup;
  bool useOnRelase;
  public RefillW2(Vector2 position, bool twoDashes, bool oneUse):base(position){
    base.Collider = new Hitbox(16f, 16f, -8f, -8f);
    Add(new PlayerCollider(OnPlayer));
    this.twoDashes = twoDashes;
    this.oneUse = oneUse;
    string text;

    text = twoDashes?"objects/refillTwo/":"objects/refill/";
    p_shatter = twoDashes?Refill.P_ShatterTwo:Refill.P_Shatter;
    p_regen = twoDashes?Refill.P_RegenTwo:Refill.P_Regen;
    p_glow = twoDashes?Refill.P_GlowTwo:Refill.P_Glow;

    Add(outline = new Image(GFX.Game[text + "outline"]));
    outline.CenterOrigin();
    outline.Visible = false;
    Add(sprite = new Sprite(GFX.Game, text + "idle"));
    sprite.AddLoop("idle", "", 0.1f);
    sprite.Play("idle");
    sprite.CenterOrigin();
    Add(flash = new Sprite(GFX.Game, text + "flash"));
    flash.Add("flash", "", 0.05f);
    flash.OnFinish = delegate {flash.Visible = false;};
    flash.CenterOrigin();

    Add(wiggler = Wiggler.Create(1f, 4f,(float v)=>{
      sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
    }));
    Add(new MirrorReflection());
    Add(bloom = new BloomPoint(0.8f, 16f));
    Add(light = new VertexLight(Color.White, 1f, 16, 48));
    Add(sine = new SineWave(0.6f, 0f));
    sine.Randomize();
    UpdateY();
    base.Depth = -100;
  }

  bool selfCol = true;
  bool parentCol = true;
  public void parentChangeStat(int vis, int col, int act){
    if(vis!=0)Visible = vis>0;
    if(col!=0){
      parentCol = col>0;
      Collidable = parentCol && selfCol;
    }
    if(act!=0) Active = act>0;
  }
  int reuseFull;
  int reuse;
  float angle = 0;
  float precession = 0;
  MTexture[] texs;

  public RefillW2(EntityData d, Vector2 o):this(d.Position + o, d.Bool("twoDash"), d.Bool("oneUse")){
    respawnTime = d.Float("respawnTimer",2.5f);
    triggering = d.Bool("triggering",false);
    useOnPickup = d.Bool("useOnPickup",false);
    useOnRelase = d.Bool("useOnRelease",false);
    reuseFull = reuse = Math.Min(d.Int("numRefresh",-1),6);
    if(reuse>=0) texs = [
      GFX.Game.GetAtlased("objects/auspicioushelper/refill/full_b1"),
      GFX.Game.GetAtlased("objects/auspicioushelper/refill/empty_b1")
    ];
    precession = Calc.Random.Range(0,MathF.PI*2);

  }
  public override void Added(Scene scene){
    base.Added(scene);
    level = SceneAs<Level>();
  }
  public override void Update(){
    base.Update();
    if(reuseFull>=0){
      angle+=Engine.DeltaTime*2.2f;
      precession+=Engine.DeltaTime*MathF.PI*2/20;
      Vector3 p = new Vector3(0.05f,1,-0.1f);
      p=p/p.Length();
      Vector3 v = new Vector3(0,1,0.3f);
      Vector3 res = v*MathF.Cos(precession) + 
        Util.Cross(p,v)*MathF.Sin(precession) + 
        p*Vector3.Dot(p,v)*(1-MathF.Cos(precession));
      UpdatePrecession(res);
    }
    if (respawnTimer > 0f){
      respawnTimer -= Engine.DeltaTime;
      if (respawnTimer <= 0f) Respawn();
    } else if (base.Scene.OnInterval(0.1f)) {
      level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
    }

    UpdateY();
    light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
    bloom.Alpha = light.Alpha * 0.8f;
    if (base.Scene.OnInterval(2f) && sprite.Visible){
      flash.Play("flash", restart: true);
      flash.Visible = true;
    }
  }
  public void Respawn(){
    if(reuseFull>=0 && reuse==0) return;
    if (!selfCol){
      reuse--;
      selfCol = true;
      sprite.Visible = true;
      outline.Visible = false;
      base.Depth = -100+(parent?.depthoffset??0);
      wiggler.Start();
      Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_return" : "event:/game/general/diamond_return", Position);
      level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
      Collidable = parentCol;
    }
  }
  public void UpdateY()
  {
    Sprite obj = flash;
    Sprite obj2 = sprite;
    float num = (bloom.Y = sine.Value * 2f);
    float y = (obj2.Y = num);
    obj.Y = y;
  }
  float orbitrad = 9;
  Vector2 cosComp;
  Vector2 sinComp;
  Vector2 angleToOrbit(float angle){
    Vector2 dir = cosComp*MathF.Cos(angle)+sinComp*MathF.Sin(angle);
    return (dir*orbitrad + Vector2.UnitY*sprite.Y/2 - Vector2.One*2.5f).Round();
  }
  void UpdatePrecession(Vector3 v = default){
    if(v==default) v = new Vector3(1,1,0);
    v=v/v.Length();
    float r = MathF.Sqrt(v.X*v.X+v.Y*v.Y);
    cosComp = new(-v.X*v.Z/r, v.Y*v.Z/r);
    sinComp = new(v.Y/r, -v.X/r);
  }
  public override void Render(){
    if(!MaterialPipe.clipBounds.CollidePointExpand(Int2.Round(Position),10+(int)orbitrad)) return;
    if(reuseFull>0){
      angle = angle%(MathF.PI*2);
      float incr = MathF.PI/reuseFull;
      float frac = angle%incr;
      int whole = (int) Math.Round((angle-frac)/incr);
      Vector2 rpos = Position.Round();
      
      bool cvis = sprite.Visible;
      int v = (reuseFull+1)/2;
      for(int i=0; i<reuseFull+1; i++){
        int idx1 = (i+whole)%reuseFull;
        int idx2 = (whole-i+reuseFull)%reuseFull;
        float angle1 = frac-incr*i;
        float angle2 = frac+incr*i;
        
        if(cvis)texs[reuse<=idx1?1:0].DrawOutline(rpos+angleToOrbit(angle1));
        texs[reuse<=idx1?1:0].Draw(rpos+angleToOrbit(angle1));
        if(i==v){
          if (cvis)sprite.DrawOutline();
          base.Render();
        }
        if(i!=reuseFull && i!=0){
          if(cvis)texs[reuse<=idx2?1:0].DrawOutline(rpos+angleToOrbit(angle2));
          texs[reuse<=idx2?1:0].Draw(rpos+angleToOrbit(angle2));
        }
      }
    } else {
      if (sprite.Visible)sprite.DrawOutline();
      base.Render();
    }
  }
  public void OnPlayer(Player player)
  {
    if (player.UseRefill(twoDashes))
    {
      Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", Position);
      Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
      Collidable = (selfCol = false);
      Add(new Coroutine(RefillRoutine(player)));
      respawnTimer = respawnTime;
      if(triggering)parent?.GetFromTree<ITemplateTriggerable>()?.OnTrigger(new TriggerInfo.EntInfo("refill",this));
    }
  }
  void TemplateHoldable.IPickupChild.OnPickup(Player p){
    if(Collidable && useOnPickup && p!=null) OnPlayer(p);
  }
  void TemplateHoldable.IPickupChild.OnRelease(Player p, Vector2 force) {
    if(Collidable && useOnRelase && p!=null) OnPlayer(p);
  }
  public IEnumerator RefillRoutine(Player player){
    Celeste.Freeze(0.05f);
    yield return null;
    level.Shake();
    Sprite obj = sprite;
    Sprite obj2 = flash;
    bool visible = false;
    obj2.Visible = false;
    obj.Visible = visible;
    if (!oneUse) outline.Visible = true;

    Depth = 8999+(parent?.depthoffset??0);
    yield return 0.05f;
    float num = player.Speed.Angle();
    level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num - MathF.PI/ 2f);
    level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num + MathF.PI/ 2f);
    SlashFx.Burst(Position, num);
    if (oneUse) RemoveSelf();
  }
}

using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[Tracked]
[CustomEntity("auspicioushelper/SpriteAnimChain")]
public class SpriteAnimChain:Entity, IMaterialObject{
  public class ActiveSprite{
    public float addedTime;
    public uint h;
    public int tex;
    public ActiveSprite(float t, uint h, int texindex){
      addedTime = t;
      this.h=h;
      this.tex=texindex;
    }
  }
  public Queue<ActiveSprite> chain = new Queue<ActiveSprite>();
  public NoiseSamplerOS2_2DLoop ogen;

  public float dur;
  public float ct=0;
  public float addfreq;
  public float travelspeed;
  public List<Vector2> nodes = new List<Vector2>();
  Random addTimes = new Random();
  bool needsFill=true;
  public bool loop;
  public float tangm;
  List<MTexture> textures;

  public SpriteAnimChain(EntityData data, Vector2 offset):base(data.Position+offset){
    int endstack = data.Bool("stack_ends",false)?2:1;
    for(int i=0; i<endstack; i++) nodes.Add(data.Position+offset);
    
    foreach(Vector2 n in data.Nodes){
      nodes.Add(n+offset);
    }
    for(int i=0; i<endstack-1; i++) nodes.Add(data.Nodes[data.Nodes.Length-1]+offset);
    loop = data.Bool("loop",false);
    travelspeed = data.Float("seconds_per_node",1);
    dur = (nodes.Count-(loop?0:3))*travelspeed;
    Depth = data.Int("depth",0);
    addfreq = data.Float("addfreq",1);
    ogen = new NoiseSamplerOS2_2DLoop(6*data.Float("tangent_freq",1f),20);
    tangm = data.Float("tangent_magnitude",16);
    textures = GFX.Game.GetAtlasSubtextures(data.Attr("atlas_directory","particles/starfield/"));
    AddTag(Tags.TransitionUpdate);
  }
  public void addSprite(float time){
    chain.Enqueue(new ActiveSprite(time, ogen.getHandle(),(int)(addTimes.NextFloat()*textures.Count)));
  }
  public override void Update(){
    base.Update();
    ogen.update(Engine.DeltaTime);
    ct += Engine.DeltaTime;
    //DebugConsole.Write(ogen.sample(0).ToString());
    if(loop) return;

    float consumed = addTimes.NextFloat()*addfreq;
    while(consumed<Engine.DeltaTime){
      addSprite(ct+consumed-Engine.DeltaTime);
      consumed+=addTimes.NextFloat()*addfreq;
    }
    while(chain.Count>0 && ct-chain.Peek().addedTime>=dur){
      chain.Dequeue();
    }
  }
  public override void Added(Scene scene){
    needsFill=true;
    if(needsFill){
      needsFill=false;
      int n = (int)(dur/addfreq); 
      //lol both methods maintain the right probability only on a point-wise basis :)
      //both are not a proper random 
      for(int i=0; i<n; i++){
        addSprite(ct-dur*addTimes.NextFloat());
      }
    }
  }
  public void registerMaterials(){
    ChannelMaterialsA.layerA?.planDrawBG(this);
  }
  public void renderMaterial(IMaterialLayer l, Camera c){
    // well since this is initially for towercollab there's no need to do culling (forgive me I am so sorry like)
    int n = nodes.Count;
    foreach(ActiveSprite s in chain){
      float t=(ct-s.addedTime)/travelspeed;
      int k=(int) t;
      if(!loop && (k<0 || k>=n-3)) continue;
      t=t-k;
      float tt=t*t;
      float ttt=t*t*t;
      Vector2 accpos = Vector2.Zero;
      accpos+=(1-3*t+3*tt-1*ttt)*nodes[k%n];
      accpos+=(4-6*tt+3*ttt)*nodes[(k+1)%n];
      accpos+=(1+3*t+3*tt-3*ttt)*nodes[(k+2)%n];
      accpos+=(1*ttt)*nodes[(k+3)%n];
      accpos=accpos/6;
      Vector2 accderiv = Vector2.Zero;
      accderiv+=(-3+6*t-3*tt)*nodes[k%n];
      accderiv+=(-12*t+9*tt)*nodes[(k+1)%n];
      accderiv+=(3+6*t-9*tt)*nodes[(k+2)%n];
      accderiv+=(3*tt)*nodes[(k+3)%n];
      accderiv = accderiv.SafeNormalize();
      accpos+=new Vector2(-accderiv.Y,accderiv.X)*(ogen.sample(s.h)*tangm);
      textures[s.tex].DrawCentered(accpos);
      //sb.Draw(Draw.Pixel.Texture.Texture_Safe, new Rectangle((int)Math.Round(accpos.X), (int)Math.Round(accpos.Y), 5, 5),Draw.Pixel.ClipRect, Color.White);
    }
  }
}
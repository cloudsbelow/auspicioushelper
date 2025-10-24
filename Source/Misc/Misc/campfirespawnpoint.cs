


using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class CampfireTHing:Entity{
  Sprite sprite;
  VertexLight light;
  BloomPoint bloom;
  Wiggler wiggle;
  float brightness;
  float multiplier;
  public CampfireTHing(){
    
    Add(light = new VertexLight(new Vector2(0f, -6f), Color.PaleVioletRed, 1f, 32, 64));
    Add(bloom = new BloomPoint(new Vector2(0f, -6f), 1f, 32f));
    Add(wiggle = Wiggler.Create(0.2f, 4f, (float f) =>{
        light.Alpha = (bloom.Alpha = Math.Min(1f, brightness + f * 0.25f) * multiplier);
    }));
  }
  float multiplierTarget;
  float multiplierSpeed;
  IEnumerator sequence(Player p){
    Position=p.Position;
    p.StateMachine.State=Player.StDummy;
    Level l = Scene as Level;
    l.Displacement.AddBurst(Position,1,10,4);
    Add(sprite = new Sprite(GFX.Game,"objects/campfire"));
    yield return 0.3f;
    p.Sprite.Play("duck");
    yield return 0.2f;
    multiplierTarget=1;
    multiplierSpeed=2;
    Add(new Coroutine(flickerSeq()));
    yield return 0.5f;
    p.Sprite.Play("idle");
    yield return 1f;
    
    l.Session.RespawnPoint=p.Position;
    p.StateMachine.state=Player.StNormal;
    multiplierTarget=0;
    multiplierSpeed=1;
    for(float a=1; a>0; a-=Engine.DeltaTime){
      sprite.Color=new Color(1,1,1,a);
      yield return null;
    }
    Remove(sprite);
  }
  IEnumerator flickerSeq(){
    multiplier=brightness=0;
    while(multiplierTarget!=0 || multiplier!=0){
      multiplier = Calc.Approach(multiplier, multiplierTarget, Engine.DeltaTime * multiplierSpeed);
      if (base.Scene.OnInterval(0.25f)){
        brightness = 0.5f + Calc.Random.NextFloat(0.5f);
        wiggle.Start();
      }
      yield return null;
    }
  }
  public override void Update() {
      base.Update();
      
  }
}
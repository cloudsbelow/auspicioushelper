


using System;
using Celeste.Mod.auspicioushelper;
using IL.Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

//take noise in a circle
public class NoiseSamplerOS2_2DLoop{
  uint handlectr;
  float radius;
  float period;
  float xpos;
  float ypos;
  float theta;  public NoiseSamplerOS2_2DLoop(float radius, float period, uint iseed=0){
    this.radius=radius;
    this.period=period;
    this.handlectr=iseed;
  }
  public uint getHandle(){
    return ++handlectr;
  }
  public void update(float dt){
    theta=(theta+dt/period)%(2*3.1415926f);
    xpos=radius*MathF.Cos(theta);
    ypos=radius*MathF.Sin(theta);
  }
  public float sample(uint handle){
    return OpenSimplex2S.Noise2_ImproveX(handle, xpos,ypos);
  }
}
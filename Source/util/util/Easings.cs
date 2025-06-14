

using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.auspicioushelper;

public static partial class Util{
  public enum Easings {
    Linear, SineIn,SineOut,SineInOut,QuadIn,QuadOut,CubeIn,CubeOut,Smoothstep,QuartIn,QuartOut,QuintIn,QuintOut
  }
  //"Linear","SineIn","SineOut","SineInOut","QuadIn","QuadOut","CubeIn","CubeOut","Smoothstep","QuartIn","QuartOut","QuintIn","QuintOut"

  public static float Approach(float val, float target, float amount, out int sign){
    if(val == target){
      sign = 0; return val;
    }
    if(val>target){
      sign = -MathF.Sign(amount);
      return MathF.Max(target, val-amount);
    }
    sign = MathF.Sign(amount);
    return MathF.Min(target, val+amount);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Linear(float val)=>val;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Linear(float val, out float derivative){
    derivative = 1;
    return val;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Spike(float val)=>MathF.Max(0,1-Math.Abs(2*val-1));
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Spike(float val, out float derivative){
    derivative = val>0.5f?-2:2;
    return Spike(val);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuadIn(float val)=>val*val;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuadIn(float val, out float derivative){
    derivative = 2*val;
    return QuadIn(val);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuadOut(float val)=>1-QuadIn(1-val);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuadOut(float val, out float derivative)=>1-QuadIn(1-val, out derivative);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float CubeIn(float val)=>val*val*val;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float CubeIn(float val, out float derivative){
    float temp = val*val;
    derivative = 3*temp;
    return val*temp;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float CubeOut(float val)=>1-CubeIn(1-val);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float CubeOut(float val, out float derivative)=>1-CubeIn(1-val, out derivative);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuartIn(float val)=>val*val*val*val;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuartIn(float val, out float derivative){
    float temp = val*val;
    derivative = temp*val*4;
    return temp*temp;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuartOut(float val)=>1-QuartIn(1-val);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuartOut(float val, out float derivative)=>1-QuartIn(1-val, out derivative);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuintIn(float val)=>val*val*val*val*val;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuintIn(float val, out float derivative){
    float temp = val*val;
    float temp2 = temp*temp;
    derivative = temp2*5;
    return temp2*val;
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuintOut(float val)=>1-QuintIn(1-val);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float QuintOut(float val, out float derivative)=>1-QuintIn(1-val, out derivative);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float SineIn(float val)=>-MathF.Cos(MathF.PI*val/2)+1;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float SineIn(float val, out float derivative){
    derivative = MathF.PI*MathF.Sin(MathF.PI*val/2)/2;
    return SineIn(val);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float SineOut(float val)=>1-SineIn(1-val);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float SineOut(float val, out float derivative)=>1-SineIn(1-val, out derivative);
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float SineInOut(float val)=>-MathF.Cos(MathF.PI*val)/2+0.5f;
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float SineInOut(float val, out float derivative){
    derivative = MathF.PI*MathF.Sin(MathF.PI*val)/2;
    return SineInOut(val);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Smoothstep(float val){
    val = Math.Clamp(val,0,1);
    return 2*val*val*(1.5f-val);
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Smoothstep(float val, out float derivative){
    derivative = MathF.Max(0,6*val*(1-val));
    return Smoothstep(val);
  }


  public static float ApplyEasing(Easings easing, float val){
    return easing switch {
      Easings.Linear    => Linear(val),
      Easings.SineIn    => SineIn(val),
      Easings.SineOut   => SineOut(val),
      Easings.SineInOut => SineInOut(val),
      Easings.QuadIn    => QuadIn(val),
      Easings.QuadOut   => QuadOut(val),
      Easings.CubeIn    => CubeIn(val),
      Easings.CubeOut   => CubeOut(val),
      Easings.Smoothstep => Smoothstep(val),
      Easings.QuartIn   => QuartIn(val),
      Easings.QuartOut  => QuartOut(val),
      Easings.QuintIn   => QuintIn(val),
      Easings.QuintOut  => QuintOut(val),
      _ => throw new ArgumentOutOfRangeException(nameof(easing), easing, "Unsupported easing type")
    };
  }
  public static float ApplyEasing(Easings easing, float val, out float derivative){
    return easing switch {
      Easings.Linear    => Linear(val,out derivative),
      Easings.SineIn    => SineIn(val,out derivative),
      Easings.SineOut   => SineOut(val,out derivative),
      Easings.SineInOut => SineInOut(val,out derivative),
      Easings.QuadIn    => QuadIn(val,out derivative),
      Easings.QuadOut   => QuadOut(val,out derivative),
      Easings.CubeIn    => CubeIn(val,out derivative),
      Easings.CubeOut   => CubeOut(val,out derivative),
      Easings.Smoothstep => Smoothstep(val,out derivative),
      Easings.QuartIn   => QuartIn(val,out derivative),
      Easings.QuartOut  => QuartOut(val,out derivative),
      Easings.QuintIn   => QuintIn(val,out derivative),
      Easings.QuintOut  => QuintOut(val,out derivative),
      _ => throw new ArgumentOutOfRangeException(nameof(easing), easing, "Unsupported easing type")
    };
  }
  public static float ApplyEasingClamped(Easings easing, float val){
    if(val<0) return 0;
    if(val>1) return 1;
    return ApplyEasing(easing,val);
  }
  public static float ApplyEasingClamped(Easings easing, float val, out float derivative){
    derivative = 0;
    if(val<0) return 0;
    if(val>1) return 1;
    return ApplyEasing(easing,val,out derivative);
  }
  const int MAXITER = 100;
  public static float getEasingPreimage(Easings easing, float target, float tolerence = 0.001f){
    if(target >= 1) return 1;
    if(target <= 0) return 0;
    float low = 0;
    float high = 1;
    float x=target;
    for(int i=0; i<MAXITER; i++){
      float y = ApplyEasing(easing, x, out var dx);
      float delta = y-target;
      if(Math.Abs(delta)<tolerence) return x;
      if(y>target) high=x;
      else low=x;
      if(MathF.Abs(dx)>0.01){
        x=x-delta/dx;
        if(x<low || x>high){
          x=(high+low)/2;
        }
      } else {
        x=(high+low)/2;
      }
    }
    DebugConsole.Write("Failed to find good match");
    return x;
  }
}
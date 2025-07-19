



using System;
using Celeste;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;
public struct Int2{
  public int x;
  public int y;
  public static readonly Int2 Zero = new(0,0);
  public static readonly Int2 One = new(1,1); 
  public Int2(int x, int y){
    this.x=x; this.y=y;
  }
  
  public static Int2 operator +(Int2 a, Int2 b) => new(a.x + b.x, a.y + b.y);
  public static Int2 operator +(Int2 a, int b) => new(a.x + b, a.y + b);
  public static Int2 operator -(Int2 a, Int2 b) => new(a.x - b.x, a.y - b.y);
  public static Int2 operator -(Int2 a, int b) => new(a.x - b, a.y - b);
  public static Int2 operator -(Int2 a) => new(-a.x, -a.y);
  public static Int2 operator *(Int2 a, Int2 b) => new(a.x * b.x, a.y * b.y);
  public static Int2 operator *(Int2 a, int s) => new(a.x * s, a.y * s);
  public static Int2 operator *(int s, Int2 a) => new(a.x * s, a.y * s);
  public static Int2 operator /(Int2 a, int s) => new(a.x / s, a.y / s);
  public static Int2 operator /(Int2 a, Int2 s) => new(a.x / s.x, a.y / s.y);
  public static bool operator ==(Int2 a, Int2 b) => a.x == b.x && a.y == b.y;
  public static bool operator ==(Vector2 a, Int2 b) => a.X == b.x && a.Y == b.y;
  public static bool operator ==(Int2 b, Vector2 a) => a.X == b.x && a.Y == b.y;
  public static bool operator !=(Int2 a, Int2 b) => !(a == b);
  public static bool operator !=(Vector2 a, Int2 b) => !(a == b);
  public static bool operator !=(Int2 a, Vector2 b) => !(a == b);
  public override bool Equals(object obj) => (obj is Int2 other && this == other)||(obj is Vector2 f && this==f);
  public override int GetHashCode() => HashCode.Combine(x, y);
  public override string ToString() => $"Int2{{{x}, {y}}}";
  public static Int2 Ceil(Vector2 o)=>new((int)Math.Ceiling(o.X),(int)Math.Ceiling(o.Y));
  public static Int2 Floor(Vector2 o)=>new((int)Math.Floor(o.X),(int)Math.Floor(o.Y));
  public static Int2 Round(Vector2 o)=>new((int)Math.Round(o.X),(int)Math.Round(o.Y));
  public static Int2 Trunc(Vector2 o)=>new((int)(o.X),(int)(o.Y));
  public static Int2 Max(Int2 a, Int2 b)=>new(Math.Max(a.x,b.x),Math.Max(a.y,b.y));
  public static Int2 Max(Int2 a, int b)=>new(Math.Max(a.x,b),Math.Max(a.y,b));
  public static Int2 Min(Int2 a, Int2 b)=>new(Math.Min(a.x,b.x),Math.Min(a.y,b.y));
  public static Int2 Min(Int2 a, int b)=>new(Math.Min(a.x,b),Math.Min(a.y,b));
  public Int2 Abs()=>new(Math.Abs(x),Math.Abs(y));
  public int MaxComp=>Math.Max(x,y);
  public int MinComp=>Math.Min(x,y);
  public static implicit operator Vector2(Int2 a)=>new Vector2(a.x,a.y);
  public static implicit operator Int2(Tuple<int,int> a)=>new Int2(a.Item1,a.Item2);
}

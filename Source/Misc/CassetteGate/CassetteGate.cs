



using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/CassetteGate")]
public class CassetteGate:Entity{
  public Solid s1;
  public Solid s2;
  int id;

  public bool open;
  bool horizontal;
  public float frac;
  int width;
  int height;
  Vector2 size;
  Vector2 major;
  Vector2 minor;
  Vector2 insideVec;
  float majorAxis=100;
  float minorAxis = 32;
  //average dotproduct unroller
  float blockWidth=>major.X*majorAxis+minor.X*minorAxis;
  float blockHeight=>major.Y*majorAxis+minor.Y*minorAxis;
  public static BasicMaterialLayer visuals = null;
  
  public CassetteGate(EntityData d, Vector2 pos):base(d.Position+pos){
    size = new Vector2(width = d.Width, height=d.Height);
    horizontal = d.Bool("horizontal",false);
    major = horizontal?Vector2.UnitX:Vector2.UnitY;
    minor = Vector2.One-major;

    id = d.ID;
    if(visuals == null){
      //visuals = new BasicMaterialLayer(-9001, auspicioushelperGFX.LoadEffect("CassetteGateShader"));
    }
    MaterialPipe.addLayer(visuals);
  }

  public override void Added(Scene s){
    base.Added(s);
    s.Add(s1 = new CassetteGateSolid(Position-major*majorAxis,blockWidth,blockHeight));
    //s.Add(s2 = new CassetteGateSolid(Position, blockWidth, blockHeight));
  }
  // bool PlayerClose(){
  //   if()
  // }
  // IEnumerator Sequence(){
  //   p = 
  //   while()
  // }
}
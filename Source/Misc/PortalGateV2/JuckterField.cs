



using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class JuckterField{
  public class JuckterCollider:ColliderList{
    
  }
  public class JuckterFake:Entity{
    Holdable Other;
    public override void Update() {
      if(Other.Scene==null) RemoveSelf();
    }
    public override void Render(){
      if(Other.Scene==null) return;

    }  
  }
}
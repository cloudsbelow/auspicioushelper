



using Microsoft.Xna.Framework;

namespace Celeste.Mod.auspicioushelper;

public class LinearSpline:Spline{
  public float[] segmentLengths;
  public LinearSpline(){
  }
  public void fromNodesAllRed(Vector2[] nodes){
    this.nodes = nodes;
    this.st = new float[this.nodes.Length];
    this.knotindices = new int[this.nodes.Length];
    for(int i=0; i<this.nodes.Length; i++){
      this.st[i]=i;
      this.knotindices[i]=i;
    }
    this.segments = this.nodes.Length; 
  }
  public override Vector2 getPos(float t){
    t=(t%segments+segments)%segments;
    int low = Util.bsearchLast(st,t);
    float frac = Util.remap(t, st[low], low<st.Length-1?st[low+1]:segments);
    return (1-frac)*nodes[low]+frac*nodes[(low+1)%nodes.Length];
  }
  public override Vector2 getPos(float t, out Vector2 derivative){
    t=(t%segments+segments)%segments;
    int low = Util.bsearchLast(st,t);
    int high = (low+1)%nodes.Length;
    float lowt = st[low];
    float hight = low<st.Length-1?st[low+1]:segments;
    float frac = Util.remap(t, lowt, hight);
    derivative = (nodes[high]-nodes[low])/(hight-lowt);
    return (1-frac)*nodes[low]+frac*nodes[high];
  }
  public override void fromNodes(Vector2[] innodes){
    base.fromNodes(innodes);
    for(int i=0; i<segments; i++){
      int start = knotindices[i];
      int end = knotindices[(i+1)%knotindices.Length];
      int j=start;
      float totlength = 0;
      while(j!=end){
        totlength+=(nodes[j]-nodes[(j+1)%nodes.Length]).Length();
        j=(j+1)%nodes.Length;
      }
      float ct = i;
      j=start;
      while(j!=end){
        st[j] = ct;
        ct+=(nodes[j]-nodes[(j+1)%nodes.Length]).Length()/totlength;
        j=(j+1)%nodes.Length;
      }
    }
    
  }
}
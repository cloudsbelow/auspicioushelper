


using System;
using System.Collections.Generic;
using Celeste.Editor;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public abstract class Spline{
  public static Dictionary<string, Spline> splines = new Dictionary<string, Spline>();
  public int segments;
  public Vector2[] nodes;
  public int[] knotindices;
  public float[] st;
  public virtual void fromNodes(Vector2[] nodes){
    List<int> k = new List<int>();
    List<Vector2> v = new List<Vector2>();
    List<float> s = new List<float>();
    k.Add(0);
    s.Add(0);
    v.Add(nodes[0]);
    int ladd = 0;
    Vector2 lpos = nodes[0];
    for(int i=1; i<nodes.Length; i++){
      if(nodes[i].X == lpos.X && nodes[i].Y==lpos.Y){
        if(ladd != v.Count-1){
          k.Add(ladd = v.Count-1);
          s[s.Count-1]=k.Count-1;
        }
      } else {
        v.Add(nodes[i]);
        s.Add(k.Count-1);
        lpos = nodes[i];
      }
    }
    this.nodes = v.ToArray();
    knotindices = k.ToArray();
    st = s.ToArray();
    segments = this.knotindices.Length;
  }
  public enum NType{
    absolute,
    derivative
  }
  public abstract Vector2 getPos(float t);
  public virtual Vector2 getPos(float t, Vector2 normalization, NType a=NType.absolute){
    return getPos(t)+normalization;
  }
  public virtual Vector2 getNormalization(float t, Vector2 pos, NType a=NType.absolute){
    return pos-getPos(t);
  }
  public virtual float getDist(float start, float end, float step=0.02f){
    float len=0;
    Vector2 lpos = getPos(start);
    while(start+step<=end){
      Vector2 npos = getPos(start+step);
      len+=(npos-lpos).Length();
      lpos=npos;
    }
    len+=(getPos(end)-lpos).Length();
    return len;
  }
  public virtual float getDt(float start, float dist, float step=0.02f){
    Vector2 lpos = getPos(start);
    if(Math.Sign(step)!=Math.Sign(dist)) step = -step;
    dist = Math.Abs(dist);
    float cdt = step;
    for(int i=0; i<10000; i++){ //this should be large enough please.
      Vector2 npos = getPos(start+cdt);
      float len = (npos-lpos).Length();
      if(len>dist){
        return cdt+step*(dist/len-1);
      }
      dist-=len;
      lpos=npos;
      cdt+=step;
    }
    throw new Exception("Bad spline");
  }
}

public class SplineAccessor{
  Spline spline;
  public float t;
  Vector2 normalization;
  public Vector2 pos;
  Spline.NType n;
  public SplineAccessor(Spline spline, Vector2 pos, float t=0, Spline.NType n=Spline.NType.absolute){
    this.spline=spline;
    this.normalization = spline.getNormalization(t,pos,n);
    this.n=n;
  }
  public Vector2 getPos(float t){
    return spline.getPos(t%spline.segments,normalization,n);
  }
  public bool towardsNext(float amount){
    if(t==0 && amount<0) t=spline.segments;
    float target = amount>0?(float)Math.Floor(t+1):(float)Math.Ceiling(t-1);
    t=Calc.Approach(t,target,Math.Abs(amount));
    if(t==target){
      t=t%spline.segments;
      pos=spline.getPos(t,normalization,n);
      return true;
    }
    pos=spline.getPos(t,normalization,n);
    return false;
  }
  public bool towardsNext(float amount, out Vector2 pos){
    var val = towardsNext(amount);
    pos=this.pos;
    return val;
  }
  public Vector2 towardsNext(float amount, out bool done){
    done = towardsNext(amount);
    return pos;
  }
  public bool towardsNextDist(float dist, float step=0.02f){
    float amount = spline.getDt(t,dist,step);
    return towardsNext(amount);
  }
  public bool towardsNextDist(float dist, out Vector2 pos, float step=0.02f){
    float amount = spline.getDt(t,dist,step);
    var val = towardsNext(amount);
    pos=this.pos;
    return val;
  }
  public Vector2 towardsNextDist(float dist, out bool done, float step=0.02f){
    float amount = spline.getDt(t,dist,step);
    done=towardsNext(amount);
    return pos;
  }
  public Vector2 move(float amount){
    t+=amount;
    pos = spline.getPos(t,normalization,n);
    return pos;
  }
  public Vector2 moveDist(float dist, float step=0.02f){
    t += spline.getDt(t,dist,step);
    pos = spline.getPos(t,normalization,n);
    return pos;
  }
  public int numsegs=>spline.segments;
}

[CustomEntity("auspicioushelper/Spline")]
public class SplineEntity:Entity{
  Spline spline;
  enum Types {
    simpleLinear,
    linear,
    catmull,
    catmullDenormalized,
  }
  Types type;
  public static Vector2[] entityInfoToNodes(Vector2 pos, Vector2[] enodes, Vector2 offset, bool lnn){
    Vector2[] nodes = new Vector2[enodes.Length+1+(lnn?1:0)];
    nodes[0]=pos+offset;
    for(int i=0; i<enodes.Length; i++){
      nodes[i+1] = enodes[i]+offset;
    }
    if(lnn)nodes[nodes.Length-1]=enodes[enodes.Length-1]+offset;
    return nodes;
  }
  public static Spline constructImpl(Vector2 firstpos, Vector2[] nodes, Vector2 offset, string type, bool lnn=false){
    switch(type){
      case "linear":
        LinearSpline sl =new LinearSpline();
        sl.fromNodes(entityInfoToNodes(firstpos,nodes,offset,true));
        return sl;
      case "simpleLinear":
        LinearSpline l = new LinearSpline();
        l.fromNodesAllRed(entityInfoToNodes(firstpos,nodes,offset,lnn));
        return l;
      default:
        return null;
    }
  }
  public SplineEntity(EntityData d, Vector2 offset):base(d.Position+offset){
    type = d.Attr("spline_type","normal") switch {
      "linear"=>Types.linear,
      "basic"=>Types.simpleLinear,
      "catmull"=>Types.catmull,
      "catmull_denormalized"=>Types.catmullDenormalized,
      _=>Types.simpleLinear,
    };
    Vector2[] nodes = new Vector2[d.Nodes.Length+1+(d.Bool("last_node_knot",false)?1:0)];
    nodes[0]=d.Position+offset;
    for(int i=0; i<d.Nodes.Length; i++){
      nodes[i+1] = d.Nodes[i]+offset;
    }
    if(d.Bool("last_node_knot",false)){
      nodes[nodes.Length-1]=d.Nodes[d.Nodes.Length-1];
    }
    switch(type){
      case Types.linear:
        spline = new LinearSpline();
        spline.fromNodes(nodes);
        break;
      default:
        var lsp = new LinearSpline();
        spline = lsp;
        lsp.fromNodesAllRed(nodes);
        break;
    }
    Spline.splines[d.Attr("identifier","")] = spline;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    scene.Remove(this);
  }
}
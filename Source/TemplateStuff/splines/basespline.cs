


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
  public abstract Vector2 getPos(float t);
  const float finitedif=0.01f;
  public virtual Vector2 getPos(float t, out Vector2 derivative){
    Vector2 p1 = getPos(t);
    Vector2 p2 = getPos(t-finitedif);
    derivative = (p1-p2)/finitedif;
    return p1;
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
  Vector2 offset;
  public Vector2 pos;
  public Vector2 tangent;
  bool getderiv;
  bool keepMod;
  public SplineAccessor(Spline spline, Vector2 origin, bool needsTangent = false, bool keepMod=true, float t=0){
    this.spline=spline;
    this.t=t;
    getderiv = needsTangent;
    this.keepMod = keepMod;
    offset = origin-spline.getPos(t);
    set(t);
  }
  public void set(float newt){
    if(keepMod) this.t=Util.SafeMod(newt,numsegs);
    else this.t=newt;
    if(!getderiv) pos = spline.getPos(t)+offset;
    else pos = spline.getPos(t, out tangent)+offset;
  }
  public void setPos(float newt){
    if(keepMod) this.t=Util.SafeMod(newt,numsegs);
    else this.t=newt;
    pos = spline.getPos(t)+offset;
  }
  public bool towardsNext(float amount){
    if(t==0 && amount<0) t=numsegs;
    float target = amount>0?(float)Math.Floor(t+1):(float)Math.Ceiling(t-1);
    t=Calc.Approach(t,target,Math.Abs(amount));
    if(t==target){
      if(getderiv) spline.getPos(Util.SafeMod(t-0.0001f*MathF.Sign(amount),numsegs), out tangent);
      setPos(t);
      return true;
    }
    set(t);
    return false;
  }
  public bool towardsNextDist(float dist, float step=0.02f){
    float amount = spline.getDt(t,dist,step);
    return towardsNext(amount);
  }
  public Vector2 move(float amount){
    t+=amount;
    set(t);
    return pos;
  }
  public Vector2 moveDist(float dist, float step=0.02f){
    t+=spline.getDt(t,dist,step);
    set(t);
    return pos;
  }
  public int numsegs=>spline.segments;
}

[CustomEntity("auspicioushelper/Spline")]
public class SplineEntity:Entity{
  Spline spline;
  public enum Types {
    invalid,
    simpleLinear,
    compoundLinear,
    centripetalNormalized,
    centripetalDenormalized,
    uniformNormalized,
    uniformDenormalized,
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
  static Dictionary<Types,float> alphaDict = new(){
    {Types.centripetalDenormalized,0.5f},{Types.centripetalNormalized,0.5f}
  };
  static HashSet<Types> normalized = new(){Types.centripetalNormalized,Types.uniformNormalized};
  public static Spline GetSpline(EntityData dat, Types ctrType, Vector2 offset){
    if(dat.Enum<Types>("spline",Types.invalid)!=Types.invalid){
      ctrType = dat.Enum<Types>("spline",Types.invalid);
    }else if(!string.IsNullOrEmpty(dat.Attr("spline"))){
      if(Spline.splines.TryGetValue(dat.Attr("spline"), out var spline)) return spline;
    }
    switch(ctrType){
      case Types.simpleLinear:{
        LinearSpline l = new LinearSpline();
        l.fromNodesAllRed(entityInfoToNodes(dat.Position,dat.Nodes,offset,false));
        return l;}
      case Types.compoundLinear:{
        LinearSpline l = new LinearSpline();
        l.fromNodes(entityInfoToNodes(dat.Position,dat.Nodes,offset,dat.Bool("lastNodeIsKnot",true)));
        return l;}
      case Types.centripetalNormalized: case Types.uniformNormalized:
      case Types.centripetalDenormalized: case Types.uniformDenormalized:{
        float alpha = alphaDict.GetValueOrDefault(ctrType);
        Spline s = normalized.Contains(ctrType)?new CatmullNorm():new CatmullDenorm();
        s.fromNodes(entityInfoToNodes(dat.Position,dat.Nodes,offset,dat.Bool("lastNodeIsKnot",true)));
        return s;}
      default: throw new Exception("invalid spline type");
    }
  }
  public static Spline GetSpline(EntityData dat,Types ctrType)=>GetSpline(dat, ctrType, Vector2.Zero);
  public SplineEntity(EntityData d, Vector2 offset):base(d.Position+offset){
    spline = GetSpline(d,Types.invalid,offset);
    Spline.splines[d.Attr("identifier","")] = spline;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    scene.Remove(this);
  }
}
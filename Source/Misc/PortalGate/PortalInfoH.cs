


using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class PortalIntersectInfoH{
  public Actor a;
  public PortalOthersider m;
  public PortalGateH p;
  public bool ce; //true if real thing is on node side
  bool facesign = false;
  Vector2 pmul;
  public bool end;
  public bool rectify;
  public bool swapped = false;
  public PortalIntersectInfoH(bool end, PortalGateH p, Actor a){
    this.p=p;
    this.a=a;
    ce = end;
    pmul = p.flipped?new Vector2(-1,1):new Vector2(1,1);
    facesign = ce?p.n2dir:p.n1dir;
    //DebugConsole.Write("started");
  }
  public PortalOthersider addOthersider(){
    a.Scene.Add(m=new PortalOthersider(a.Position, this));
    m.Center=getOthersiderPos();
    m.Scene = a.Scene;
    return m;
  }
  public Vector2 getOthersiderPos(){
    return pmul*(a.Center-p.getpos(ce))+p.getpos(!ce);
  }
  public Vector2 calcspeed(Vector2 speed, bool newend){
    Vector2 rel = speed-p.getspeed(!newend);
    if(p.flipped) rel.X*=-1;
    rel+=p.getspeed(newend);
    return rel;
  }
  public void swap(){
    swapped = true;
    m.Center = getOthersiderPos();
    ce = !ce;
    DebugConsole.Write("swap "+a.Position.ToString()+" "+m.Position.ToString());
    Vector2 temp = a.Center;
    a.Center=m.Center;
    m.Center=temp;
    PortalGateH.evalEnt(a);
    facesign = ce?p.n2dir:p.n1dir;
    //PortalGateH.collideLim[m]=p.getSidedCollidelim(!ce);

    if(a is Player pl){
      pl.Speed = calcspeed(pl.Speed,ce);
      if(pl.StateMachine.state == Player.StDash && p.flipped) pl.DashDir.X*=-1;
      if(p.flipped)pl.LiftSpeed=new Vector2(-pl.LiftSpeed.X,pl.LiftSpeed.Y);
      Level l = pl.Scene as Level;
      if(!((IntRect)l.Bounds).CollidePoint(pl.Position)){
        if(l.Session.MapData.GetAt(pl.Position) is LevelData ld){
          l.NextTransitionDuration=0;
          l.NextLevel(pl.Position,Vector2.One);
        } else {
          l.EnforceBounds(pl);
        }
      }
    } else if(a is Glider g){
      g.Speed = calcspeed(g.Speed,ce);
    } else {
      //lifted striaght from chatgpt 先生 himself.
      Type type = a.GetType();
      FieldInfo field = type.GetField("Speed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (field != null && field.FieldType == typeof(Vector2)){
        DebugConsole.Write("Dynamically setting speed field");
          Vector2 speed = (Vector2)field.GetValue(a);
          field.SetValue(a, calcspeed(speed, ce));
          return;
      }
      PropertyInfo property = type.GetProperty("Speed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (property != null && property.CanWrite && property.PropertyType == typeof(Vector2)){
          DebugConsole.Write("Dynamically setting speed property");
          Vector2 speed = (Vector2)property.GetValue(a);
          property.SetValue(a, calcspeed(speed, ce));
          return;
      }
    }
  }
  public bool finish(){
    float center = a.CenterX;
    int signedface = Math.Sign(a.CenterX - p.getpos(ce).X)*(facesign?1:-1);
    m.Center=getOthersiderPos();
    if(signedface == -1)swap();
    end = Math.Sign((facesign?a.Left:a.Right)-p.getpos(ce).X)*(facesign?1:-1) == 1;
    if(end && a is Player && p.giveRCB && swapped){
      bool right = ce?p.n2dir:p.n1dir;
      RcbHelper.give(right,ce?p.npos.Y:p.Position.Y);
    }
    //if(end)DebugConsole.Write("ended");
    return end;
  }
  public bool applyDummyPush(Vector2 amount){
    if(!m.propegateMove) return false;
    bool hblock = amount.X==0?false:a.MoveHExact((int)amount.X);
    
    return true;
  }
  public bool getAbsoluteRects(Hitbox h, out FloatRect r1, out FloatRect r2){
    Vector2 ipos = ce?p.npos:p.Position;
    Vector2 opos = ce?p.Position:p.npos;
    Vector2 del = opos-ipos;
    bool inr=ce?p.n2dir:p.n1dir;
    bool outr=ce?p.n1dir:p.n2dir;
    float overlap;
    if(inr){
      overlap = Math.Max(0,ipos.X-h.AbsoluteLeft);
      r1=new FloatRect(h.AbsoluteLeft+overlap,h.AbsoluteTop,h.Width-overlap, h.height);
    } else {
      overlap = Math.Max(0,h.AbsoluteRight-ipos.X);
      r1=new FloatRect(h.AbsoluteLeft,h.AbsoluteTop,h.Width-overlap,h.height);
    }
    r2 = new FloatRect(outr?opos.X:opos.X-overlap,h.AbsoluteTop+del.Y,overlap,h.height);
    return overlap>0;
  }
  public int reinterpertPush(int moveH, Solid pusher){
    if(!rectify) return moveH;
    if(!(a.Collider is Hitbox h) || !(pusher.Collider is Hitbox o)){
      DebugConsole.Write("Should not happen; Actor or solid collider not hitbox!");
      return moveH;
    }else{
      getAbsoluteRects(a.Collider as Hitbox,out var r1,out var r2);
      float absl = o.AbsoluteLeft; float abst = o.AbsoluteTop;
      bool hits1 = r1.CollideExRect(absl, abst, o.width,o.height);
      bool hits2 = r2.CollideExRect(absl, abst, o.width,o.height);
      //DebugConsole.Write(hits1.ToString()+" "+hits2.ToString());
      if(hits1) return moveH;
      if(!hits2){
        DebugConsole.Write("really weird if things get here");
      }
      int nmoveH = moveH+(int)(moveH<0? a.Right-r2.x-r2.w:a.Left-r2.x);
      DebugConsole.Write("Rectified push "+moveH.ToString()+"--->"+nmoveH.ToString());
      return nmoveH;
    }
  }
  public bool checkLeaves(int voffset=0){
    float cpos = ce?p.npos.Y:p.Position.Y;
    return a.Top+voffset<cpos || a.Bottom+voffset>cpos+p.height;
  }
  public bool checkLeavesHorizontal(){
    bool inr=ce?p.n2dir:p.n1dir;
    Vector2 ipos = ce?p.npos:p.Position;
    return inr? ipos.X-a.Left<0 : a.Right-ipos.X<0;
  }
}
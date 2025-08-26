


using System;
using System.Collections.Generic;
using Celeste.Mod.auspicioushelper;
using Celeste.Editor;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/PortalGateH")]
[Tracked]
public class PortalGateH:Entity{
  private MTexture texture = GFX.Game["util/lightbeam"];
  public NoiseSamplerOS2_2DLoop ogen = new NoiseSamplerOS2_2DLoop(20, 70, 100);
  public static Dictionary<Entity, PortalIntersectInfoH> intersections = new Dictionary<Entity, PortalIntersectInfoH>();
  //public static Dictionary<Entity, Vector2> collideLim = new Dictionary<Entity, Vector2>();
  public class SurroundingInfoH {
    public PortalGateH left=null;
    public PortalGateH right=null;
    public Entity actor=null;
    public float leftl=float.NegativeInfinity;
    public float rightl=float.PositiveInfinity;
    public bool leftn;
    public bool rightn;
    public void setl(PortalGateH p, bool node){
      left = p; leftl = node?p.x2:p.x1; leftn = node;
    }
    public void setr(PortalGateH p, bool node){
      right = p; rightl = node?p.x2:p.x1; rightn = node;
    }
  }
  //public static Dictionary<Entity,SurroundingInfoH> portalInfos = new Dictionary<Entity, SurroundingInfoH>();
  public static SurroundingInfoH evalEnt(Entity a){
    SurroundingInfoH s = new SurroundingInfoH{actor=a};
    foreach(PortalGateH p in a.Scene.Tracker.GetEntities<PortalGateH>()){
      if(p.top1<=a.Top && p.bottom1>=a.Bottom){
        if(p.n1dir){
          if(a.Right>=p.x1 && s.leftl<p.x1) s.setl(p,false);
        } else {
          if(a.Left<=p.x1 && s.rightl>p.x1) s.setr(p,false);
        }
      }
      if(p.top2<=a.Top && p.bottom2>=a.Bottom){
        if(p.n2dir){
          if(a.Right>=p.x2 && s.leftl<p.x2) s.setl(p,true);
        } else {
          if(a.Left<=p.x2 && s.rightl>p.x2) s.setr(p,true);
        }
      }
    }
    // portalInfos[a]=s;
    // collideLim[a]=new Vector2(s.leftl,s.rightl);
    return s;
  }
  public void movePos(Vector2 amount){
    if(s1!=null)s1.Collidable=false;
    v1s[0]+=amount/Math.Max(Engine.DeltaTime,0.005f);
    moveFacePos(amount, false, ref Position);
    if(s1!=null) s1.Collidable=true;
  }
  public void moveNpos(Vector2 amount){
    if(s2!=null)s2.Collidable=false;
    v2s[0]+=amount/Math.Max(Engine.DeltaTime,0.005f);
    moveFacePos(amount, true, ref npos);
    if(s2!=null) s2.Collidable=true;
  }
  public void moveFacePos(Vector2 amount, bool node, ref Vector2 pos){
    Vector2 op=pos;
    Vector2 hp=pos+Vector2.UnitX*amount.X;
    Vector2 np = pos+amount;
    //DebugConsole.Write("\n Adding Speed "+amount.ToString()+" "+v1s[0].ToString());

    /* 
      Several cases to worry about:
      1. The portal going to encounter and begin intersection with an actor (Goto 2)
      2. The portal is intersecting an actor and is on its current side
        -We must moveH the fake and propagate any hit to the real
      3. The portal is intersecting an actor and is on its neglected side
        -if it is moving towards its face, we must march moveH the fake and propagate any hit to the real
      4. Handle any vertical splinching
      
      Split into vertical and horizontal phase for each entity; (1) is of no concern for vertical
    */
    int mdir = Math.Sign(amount.X);
    int ymdir = Math.Sign(amount.Y);
    bool ndir = node?n2dir:n1dir;
    // List<PortalIntersectInfoH> toremove = new();
    // foreach(var pair in intersections){
    //   Entity e = pair.Value.a;
    //   if(!e.Active || !(e.Collider is Hitbox h) || e.Scene == null){
    //     toremove.Add(pair.Value);
    //     continue;
    //   }
    // }
    foreach(Actor a in Scene.Tracker.GetEntities<Actor>()){
      if(!a.Active || !(a.Collider is Hitbox h)) continue;
      PortalIntersectInfoH info = null;
      if(intersections.TryGetValue(a,out info)){
        //DebugConsole.Write("Fetched intersection");
      } else{
        if(pos.Y<=a.Top && pos.Y+height>=a.Bottom){
          if((ndir && a.Left<=np.X && a.Left>=op.X)||(!ndir && a.Right>=np.X && a.Right<=op.X)){
            intersections[a]= info = new PortalIntersectInfoH(node, this,a);
            PortalOthersider mn = info.addOthersider();
            //DebugConsole.Write("Started intersection");
          }
        }
      }
      if(info == null || info.p!=this) continue;

      bool anotherHit;
      info.rectify = false;
      for(int i=0; i!=amount.X; i+=mdir){
        pos=op+Vector2.UnitX*i;
        Solid solid = a.CollideFirst<Solid>();
        if(solid!=null){
          solid.Collidable = false;
          if(info.ce != node){ //center is on other side; "slamming" case
            anotherHit = a.MoveHExact(-mdir*hmult,a.SquishCallback,solid);
            a.LiftSpeed = info.calcspeed(solid.LiftSpeed,true);
          } else { //center is on this side; "extruding" case
            anotherHit = a.MoveHExact(mdir,a.SquishCallback,solid);
          }
          solid.Collidable = true;
        }
      }
    
      for(int i=0; i!=amount.Y; i+=ymdir){
        pos=hp+Vector2.UnitY*i;
        Solid solid = a.CollideFirst<Solid>();
        if(solid!=null){
          solid.Collidable = false;
          if(info.ce != node){ //center is on other side; "slamming" case
            anotherHit = a.MoveVExact(-ymdir,a.SquishCallback,solid);
            a.LiftSpeed = info.calcspeed(solid.LiftSpeed,true);
          } else { //center is on this side; "extruding" case
            anotherHit = a.MoveVExact(ymdir,a.SquishCallback,solid);
          }
          solid.Collidable = true;
        }
      }
      info.rectify = true;
      if(info.finish()){
        intersections.Remove(a);
        //DebugConsole.Write("Finished intersection"+(a.Left<=op.X).ToString());
      }
    }
    pos=np;
  }

  public float height;
  public Vector2 npos;
  public float top1 {
    get=>Position.Y;
  }
  public float bottom1 {
    get=>Position.Y+height;
  }
  public float top2 {
    get=>npos.Y;
  }
  public float bottom2{
    get=>npos.Y+height;
  }
  public float x1{
    get=>Position.X;
  }
  public float x2{
    get=>npos.X;
  }
  public Vector2[] v1s = {Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero};
  public Vector2[] v2s = {Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero};
  public Solid s1=null;
  public Solid s2=null;
  public static Solid fakeSolid = new Solid(Vector2.Zero,0,0,false);
  public bool flipped;
  public int hmult;
  public bool n1dir;
  public bool n2dir;
  public bool contain=true;
  public List<uint> handles = new List<uint>();
  public Color color;
  public bool giveRCB;
  public Vector2 drawoffset1 =Vector2.Zero;
  public Vector2 drawoffset2 =Vector2.Zero;
  public bool instantCamera = false;
  
  public PortalGateH(EntityData d, Vector2 offset):base(d.Position+offset){
    portalHooks.hooks.enable();
    Depth=d.Int("depth",-9998);
    height = d.Height+0.999f;
    npos=d.Nodes[0]+offset;
    n1dir = d.Bool("right_facing_f0",false);
    n2dir = d.Bool("right_facing_f1",true);
    instantCamera = d.Bool("instant_camera",false);
    color = Util.hexToColor(d.Attr("color_hex","#FFFA"));
    //DebugConsole.Write(color.ToString()+" "+x1.ToString()+" "+x2.ToString());
    flipped = n1dir==n2dir;
    hmult = flipped?-1:1;
    
    for(int i=0; i<height; i+=2){
      handles.Add(ogen.getHandle());
      handles.Add(ogen.getHandle());
    }
    if(d.Bool("attached",false)){
      Add(new StaticMover(){
        OnMove = movePos,
        SolidChecker = (Solid solid)=>{
          bool c = solid.CollideRect(new Rectangle((int)Math.Floor(Position.X)-1, (int)Position.Y, 1, (int)height));
          bool d = solid.CollideRect(new Rectangle((int)Math.Floor(Position.X), (int)Position.Y, 1, (int)height));
          if(c==n1dir && d!=n1dir)s1=solid;
          return c!=d;
        },
        OnShake = (Vector2 amount)=>{
          drawoffset1 = amount;
        }
      });
      Add(new StaticMover(){
        OnMove = moveNpos,
        SolidChecker = (Solid solid)=>{
          bool c = solid.CollideRect(new Rectangle((int)Math.Floor(npos.X)-1, (int)npos.Y, 1, (int)height));
          bool d = solid.CollideRect(new Rectangle((int)Math.Floor(npos.X), (int)npos.Y, 1, (int)height));
          if(c==n2dir && d!=n2dir)s2=solid;
          return c!=d;
        },
        OnShake = (Vector2 amount)=>{
          drawoffset2=amount;
        }
      });
    }
    giveRCB = d.Bool("give_rcb",true);
    RcbHelper.hooks.enable();
  }
  
  public static bool BlockMoveHHook(On.Celeste.Platform.orig_MoveHExactCollideSolids orig, Platform p, int amount, bool thru, Action<Vector2, Vector2, Platform> onCollide){
    SurroundingInfoH s = evalEnt(p);
    PortalIntersectInfoH info=null;
    if(intersections.TryGetValue(p,out info)){
    } else if(p.Left+amount<=s.leftl) {
      intersections[p]=(info = new PortalIntersectInfoH(s.leftn, s.left,p));
      PortalOthersider mn = info.addOthersider();
    } else if(p.Right+amount>=s.rightl){
      intersections[p]=(info = new PortalIntersectInfoH(s.rightn, s.right, p));
      PortalOthersider mn = info.addOthersider();
    }
    bool res = orig(p,amount,thru,onCollide);
    if(info!=null && info.finish()) intersections.Remove(p);
    return res;
  }
  public static bool BlockMoveVHook(On.Celeste.Platform.orig_MoveVExactCollideSolids orig, Platform p, int amount, bool thru, Action<Vector2, Vector2, Platform> onCollide){
    //var s  = evalEnt(p);
    if(intersections.TryGetValue(p,out var info) && info.p.contain){
      int ctr=0;
      int dir = Math.Sign(amount);
      while(ctr!=amount){
        if(info.checkLeaves(ctr+dir)){
          if(!orig(p,ctr,thru,onCollide)){
            if(onCollide != null) onCollide.Invoke(dir*Vector2.UnitX,ctr*Vector2.UnitY,fakeSolid);
          }
          return true;
        }
        ctr+=dir;
      }
      return orig(p,ctr,thru,onCollide);
    } else {
      return orig(p,amount,thru,onCollide);
    }
  }
  public static bool ActorMoveHHook(On.Celeste.Actor.orig_MoveHExact orig, Actor a, int moveH, Collision onCollide, Solid pusher){
    SurroundingInfoH s = evalEnt(a);
    PortalIntersectInfoH info=null;
    if(intersections.TryGetValue(a,out info)){
    } else if(a.Left+moveH<=s.leftl) {
      intersections[a]=(info = new PortalIntersectInfoH(s.leftn, s.left,a));
      PortalOthersider mn = info.addOthersider();
      //collideLim[mn] = s.left.getSidedCollidelim(!s.leftn);
    } else if(a.Right+moveH>=s.rightl){
      intersections[a]=(info = new PortalIntersectInfoH(s.rightn, s.right, a));
      PortalOthersider mn = info.addOthersider();
      //collideLim[mn] = s.right.getSidedCollidelim(!s.rightn);
    }
    if(pusher!=null && info!=null){
      moveH = info.reinterpertPush(moveH,pusher);
    }
    bool val = orig(a,moveH,onCollide,pusher);
    if(info != null && info.finish()){
      intersections.Remove(a);
    } 
    return val;
  }
  public static void ActorNaiveMove(On.Celeste.Actor.orig_NaiveMove orig, Actor a, Vector2 amount){
    SurroundingInfoH s = evalEnt(a);
    PortalIntersectInfoH info = null;
    float moveH = amount.X;
    float moveV = amount.Y;
    if(intersections.TryGetValue(a,out info)){

    } else if(a.Left+moveH<=s.leftl) {
      intersections[a]=(info = new PortalIntersectInfoH(s.leftn, s.left,a));
      PortalOthersider mn = info.addOthersider();
    } else if(a.Right+moveH>=s.rightl){
      intersections[a]=(info = new PortalIntersectInfoH(s.rightn, s.right, a));
      PortalOthersider mn = info.addOthersider();
    }
    orig(a,amount);
    if(info!=null && info.finish()){
      intersections.Remove(a);
    }
  }
  public static bool ActorMoveVHook(On.Celeste.Actor.orig_MoveVExact orig, Actor a, int moveV, Collision onCollide, Solid pusher){
    //var s  = evalEnt(a);
    if(intersections.TryGetValue(a,out var info) && info.p.contain){
      int ctr=0;
      int dir = Math.Sign(moveV);
      while(ctr!=moveV){
        if(info.checkLeaves(ctr+dir)){
          if(!orig(a,ctr,onCollide,pusher)){
            if(onCollide != null) onCollide.Invoke(new CollisionData{
              Direction = Vector2.UnitY * dir,
              Moved = Vector2.UnitY * ctr,
              TargetPosition = a.Position+Vector2.UnitY*moveV,
              Hit = fakeSolid,
              Pusher = pusher
            });
          }
          return true;
        }
        ctr+=dir;
      }
      return orig(a,moveV,onCollide,pusher);
    } else {
      return orig(a,moveV,onCollide,pusher);
    }
  }
  public static void ActorUpdateHook(On.Celeste.Actor.orig_Update orig, Actor a){
    if(intersections.TryGetValue(a,out var info)){
    } else {
      SurroundingInfoH s = evalEnt(a);
      if(a.Left<s.leftl) {
        intersections[a]=(info = new PortalIntersectInfoH(s.leftn, s.left,a));
        PortalOthersider mn = info.addOthersider();
      } else if(a.Right>s.rightl){
        intersections[a]=(info = new PortalIntersectInfoH(s.rightn, s.right, a));
        PortalOthersider mn = info.addOthersider();
      }
    }
    orig(a);
  }
  public Vector2 getpos(bool node){
    return node?npos:Position;
  }
  public Vector2 getspeed(bool node){
    Vector2[] s=node?v2s:v1s;
    float mX=0;
    float mY=0;
    foreach(Vector2 v in s){
      if(Math.Abs(v.X)>Math.Abs(mX)) mX=v.X;
      if(Math.Abs(v.Y)>Math.Abs(mY)) mY=v.Y;
    }
    return new Vector2(mX,mY);
  }
  public Vector2 getSidedCollidelim(bool node){
    Vector2 b = new Vector2(float.NegativeInfinity,float.PositiveInfinity);
    float x = node?x2:x1;
    if(node?n2dir:n1dir){
      b.X=x;
    } else {
      b.Y=x;
    }
    return b;
  }


  public override void Render(){
    base.Render();
    ogen.update(Engine.RawDeltaTime/3);
    Vector2 offset = new Vector2(0f, 0.5f);
    float wrec1 = (n1dir?1f:-1f) / (float)texture.Width;
    float wrec2 = (n2dir?1f:-1f) / (float)texture.Width;
    for(int i=4; i<height-4; i+=4){
      float alpha = Math.Min(1,Math.Max(0,ogen.sample(handles[i]))+0.2f);
      if(alpha<0) continue;
      float length = ogen.sample(handles[i+1])*10+20;
      texture.Draw(Position+new Vector2(0,i)+drawoffset1, offset, color*alpha, new Vector2(wrec1 * length, 8), 0);
      texture.Draw(npos+new Vector2(0,i)+drawoffset2, offset, color*alpha, new Vector2(wrec2 * length, 8), 0);
    }
    
  }
  public override void Update(){
    base.Update();
    //DebugConsole.Write(v1s[0].ToString());
    if(Engine.DeltaTime!=0){
      for(int i=v1s.Length-1; i>0; i--){
        v1s[i]=v1s[i-1];
        v2s[i]=v2s[i-1];
      }
      v1s[0]=Vector2.Zero;
      v2s[0]=Vector2.Zero;
    }
    //DebugConsole.Write((getspeed(false)-getspeed(true)).ToString()+" = "+getspeed(false));
  }
}
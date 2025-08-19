


using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class PortalOthersider:Entity{
  public PortalIntersectInfoH info;
  public bool propegateMove=true;
  public int mulMoveH = 1;
  

  public PortalOthersider(Vector2 pos, PortalIntersectInfoH h):base(pos){
    Entity copying = h.a;
    Depth=copying.Depth;
    Collidable=false;
    Collider chit = copying.Collider;
    Collider = new Hitbox(chit.Width,chit.Height,chit.Position.X,chit.Position.Y);
    info=h;
    if(h.p.flipped) mulMoveH=-1;
  }
  public override void Render(){
    base.Render();
    Vector2 unrendpos = Position;
    Center = info.getOthersiderPos();
    if(info.end)return;
    var delta = -info.a.Position+Position;
    if(info.a is Actor){
      Vector2 mulMove = new Vector2(mulMoveH,1);
      if(info.a is Player p){
        PlayerHair h = p.Get<PlayerHair>();
        if(info.p.flipped) p.Facing = (Facings)(0 - p.Facing);
        var oldpos = p.Position;
        p.Position+=delta;
        for(int i=0; i<h.Sprite.HairCount; i++){
          h.Nodes[i]=(h.Nodes[i]-oldpos)*mulMove+p.Position;
        }
        try{
          p.Render();
        }catch(Exception ex){
          DebugConsole.Write("Error in rendering otherside player");
          DebugConsole.Write(ex.ToString());
        }
        for(int i=0; i<h.Sprite.HairCount; i++){
          h.Nodes[i]=(h.Nodes[i]-p.Position)*mulMove+oldpos;
        }
        if(info.p.flipped) p.Facing = (Facings)(0 - p.Facing);
        p.Position=oldpos;
        return;
      }

      foreach(Component c in info.a.Components){
        if(c is Sprite s){
          var oldpos = s.RenderPosition;
          s.RenderPosition = oldpos+delta;
          s.Scale.X*=mulMoveH;
          s.Render();
          s.Scale.X*=mulMoveH;
          s.RenderPosition = oldpos;
        }
      }
    } else {
      Vector2 pos = info.a.Position;
      info.a.Position = this.Position;
      info.a.Render();
      info.a.Position = pos;
    }
    Position=unrendpos;
  }
  public override void Update(){
    base.Update();
    //DebugConsole.Write(info.getOthersiderPos().ToString());
    if(info.end){
      RemoveSelf();
      return;
    } 
    if(info.checkLeaves() || info.checkLeavesHorizontal()){
      info.end = true;
      PortalGateH.intersections.Remove(info.a);
    }
  }
  /*public int tryMoveH(int moveH, Collision onCollide = null, Solid pusher = null){
    propegateMove = false;
    float oldX = X;
    bool fail = MoveHExact(moveH*mulMoveH, onCollide, pusher);
    propegateMove = true;
    return fail?(int)Math.Round(X-oldX)*mulMoveH:moveH;
  }
  public int tryMoveV(int moveV, Collision onCollide=null, Solid pusher=null){
    propegateMove = false;
    float oldY = Y;
    bool fail = MoveVExact(moveV, onCollide, pusher);
    propegateMove = true;
    return fail?(int)Math.Round(Y-oldY):moveV;
  }*/
}



using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

public class MaddiesStuff{
  public class SidewaysJt:ChildMarker,ISimpleWrapper{
    public Entity wrapped {get;set;}
    public Vector2 toffset {get;set;}
    Template ITemplateChild.parent {
      get=>base.parent;
      set=>base.parent = value;
    }
    public SidewaysJt(Entity w, Template parent):base(parent){
      MaddiesIop.hooks.enable();
      wrapped = w;
      allowLeftToRight = MaddiesIop.side.get(wrapped);
      wrapped.Add(this);
    }
    bool allowLeftToRight;
    bool PlayerIsRiding(Player p){
      return  p.StateMachine.State==Player.StClimb && 
              p.CollideCheckOutside(wrapped,p.Position+Vector2.UnitX*(allowLeftToRight?-1:1)) &&
              p.Facing == (allowLeftToRight?Facings.Left:Facings.Right);
    }
    bool ITemplateChild.hasPlayerRider(){
      return UpdateHook.cachedPlayer is {} p && !p.Dead && PlayerIsRiding(p);         
    }
    public void relposTo(Vector2 nloc, Vector2 liftspeed){
      int curx = (int) MathF.Round(wrapped.Position.X);
      int newx = (int) MathF.Round(nloc.X+toffset.X);
      Player p = UpdateHook.cachedPlayer;
      if(p==null || p.Dead || !PlayerIsRiding(p)) p=null;
      if(p!=null) p.LiftSpeed=liftspeed;
      if(curx!=newx){
        int delta = newx-curx;
        wrapped.Position.X=newx;
        p?.MoveH(delta);
        if(allowLeftToRight == delta>0 && wrapped.Scene is {} scene){
          float pos = allowLeftToRight?wrapped.Right:wrapped.Left;
          foreach(Actor a in wrapped.CollideAll<Actor>()){
            a.LiftSpeed = liftspeed;
            a.MoveH(pos-(allowLeftToRight?a.Left:a.Right));
          }
        }
      }
      float newy = MathF.Round(nloc.Y+toffset.Y);
      if(newy!=wrapped.Position.Y)p?.MoveV(newy-wrapped.Position.Y);
      wrapped.Position.Y = newy;
    }
  }
  public static void setup(){
    EntityParser.clarify(["MaxHelpingHand/SidewaysJumpThru","MaxHelpingHand/AttachedSidewaysJumpThru"], EntityParser.Types.unwrapped, (l,d,o,e)=>{
      if(Level.EntityLoaders.TryGetValue("MaxHelpingHand/SidewaysJumpThru",out var orig)) 
        EntityParser.currentParent.addEnt(new SidewaysJt(orig(l,d,o,e),EntityParser.currentParent));
      return null;
    });
  }
}
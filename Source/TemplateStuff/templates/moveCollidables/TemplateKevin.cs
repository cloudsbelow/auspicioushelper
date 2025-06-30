// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Microsoft.Xna.Framework;
// using Monocle;
// namespace Celeste.Mod.auspicioushelper;

// public class TemplateKevin:TemplateMoveCollidable{
//   public TemplateKevin(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
//   Vector2 movedir;
//   enum Axis{vertical, horizontal}
//   Stack<Tuple<Axis, float>> moves = new(); 
//   bool going;
//   float maxspeed;
//   float acceleration;
//   float locktime;
//   int leniency;
//   bool locked;
//   public TemplateKevin(EntityData d, Vector2 offset, int depthoffset)
//   :base(d,offset+d.Position,depthoffset){
//     OnDashCollide = (p,d)=>{
//       return DashCollisionResults.NormalCollision;
//     };
//   }
//   Coroutine current;
//   IEnumerator GoSequence(Vector2 dir){
//     float speed = 0;
//     locked = true;
//     going = true;
//     shake(locktime);
//     yield return locktime;
//     locked = false;
//     while(true){
//       speed = Calc.Approach(speed, maxspeed, Engine.DeltaTime*acceleration);
//       var q = getq(movedir.Abs()*speed+leniency*Vector2.One);
//       ownLiftspeed = movedir*speed;
//       bool hit = false;
//       if(movedir.Y!=0) hit = MoveVCollide(q,speed*movedir.Y*Engine.DeltaTime,leniency,movedir*speed);
//       else hit = MoveHCollide(q,speed*movedir.X*Engine.DeltaTime,leniency,movedir*speed);
//       if(hit){
//         shake(0.4f);
//         yield return 0.4f;
//         going = false;
//         Add(current = new Coroutine(ReturnSequence()));
//         yield break;
//       }
//       yield return null;
//     }
//   }
//   IEnumerator ReturnSequence(){
//     while(true){

      
//       if(moves.Count == 0) yield break;
//     }
//   }
// }
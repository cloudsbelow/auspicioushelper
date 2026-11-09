


using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;


[Tracked]
[CustomEntity("auspicioushelper/TemplateParallax")]
public class TemplateParallax:Template{
  ChannelState.Vec2Ch fac;
  Vector2 camOrigin, offset;
  protected override Vector2 virtLoc=>Position+offset;
  public TemplateParallax(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateParallax(EntityData d, Vector2 offset, int depthoffset):base(d,d.Position+offset,depthoffset){
    fac = d.ChannelVecOrScalar("factor",1);
    camOrigin = d.Nodes?.Length>0? d.Nodes[0]-d.Position : Vector2.Zero;
    Depth = -10000000;
    AddTag(Tags.TransitionUpdate);
    ResetEvents.Hooks<TemplateParallax>.enable();
  }
  public override void Update() {
    base.Update();
    Camera c = (Scene as Level).Camera;
    Vector2 camCenter = new((c.Left+c.Right)/2, (c.Top+c.Bottom)/2);
    Vector2 delta = camCenter - (Position+camOrigin);
    offset = (Vector2.One-fac) * delta;
    childRelposSafe();
    Depth = -10000000; //just needs to be after player always
  }
}
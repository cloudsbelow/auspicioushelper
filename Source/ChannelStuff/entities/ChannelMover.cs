using System;
using Celeste.Mod.auspicioushelper;
using Celeste.Mod.auspicioushelper.iop;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[Tracked]
[CustomEntity("auspicioushelper/ChannelMover")]
public class ChannelMover:Solid, ICustomMatRender{
  public Vector2 p0;
  public Vector2 p1;
  public Vector2 loc => prog*p1+(1-prog)*p0;
  public float width;
  public float height;
  public float relspd;
  public float asym;
  public float prog;
  public string channel {get; set;}
  public float dir;
  public ChannelMover(EntityData data, Vector2 offset):base(data.Position,data.Width, data.Height, data.Bool("safe",false)){
    width = data.Width;
    height = data.Height;
    p0 = data.Position+offset;
    p1 = data.Nodes[0]+offset;
    channel = data.Attr("channel","");
    relspd = 1/data.Float("move_time",1);
    asym = data.Float("asymmetry",1f);
  }
  public void setChVal(int val){
    dir = (val&1)==1?1:-1*asym;
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(ChannelMaterialsA.layerA is {} layera && layera.enabled){
      OverrideVisualComponent.Get(this).AddToOverride(new(layera,-100,true));
    }
    ChannelTracker ct = new ChannelTracker(channel, setChVal).AddTo(this);
    dir = (ct.value &1)==1?1:-1*asym;
    Position = dir==1?p1:p0;
    prog = dir == 1?1:0;
  }
  public override void Update(){
    base.Update();
    float lprog = prog;
    prog = System.Math.Clamp(prog+dir*relspd*Engine.DeltaTime,0,1);
    if(lprog != prog){
      MoveTo(loc, dir*relspd*(p1-p0)+(tcomp?.getParentLiftspeed()??Vector2.Zero));
    }
  }
  public override void Render(){
    base.Render();
    Draw.Rect(Position, width, height, Color.Red);
  }
  void ICustomMatRender.MatRender(){
    Draw.SpriteBatch.Draw(Draw.Pixel.Texture.Texture_Safe,new Rectangle((int) Position.X, (int) Position.Y,(int) width, (int) height), Draw.Pixel.ClipRect, new Color(1,0,0,255));
  }

  TemplateIop.TemplateChildComponent tcomp = null;
  public TemplateIop.TemplateChildComponent makeComp(){
    Vector2 offset0=Vector2.Zero; 
    Vector2 offset1=Vector2.Zero;
    //This block doesn't appear or disappear so we don't need to change the default
    //ChangeStatus. Also, it doesn't make any child entities so there's no need to change
    //the default AddSelf
    tcomp = new(this){
      RepositionCB = (Vector2 nloc, Vector2 liftspeed)=>{
        p0 = offset0+nloc;
        p1 = offset1+nloc;
        MoveTo(loc, (Math.Round(prog)!=prog?dir*relspd*(p1-p0):Vector2.Zero)+liftspeed);
      },
      SetOffsetCB = (Vector2 ppos)=>{
        offset0 = p0-ppos;
        offset1 = p1-ppos;
      }
    };
    OnDashCollide = tcomp.RegisterDashhit;
    return tcomp;
  }
}
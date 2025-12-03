

using System;
using System.Text.RegularExpressions;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper;
[CustomEntity("auspicioushelper/TemplateGluable")]
[MapenterEv(nameof(Search))]
public class TemplateGluable:Template{
  string lookingFor;
  Entity gluedto;
  int smearamount;
  Vector2[] pastLiftspeed;
  bool averageSmear;
  public override Vector2 gatheredLiftspeed => ownLiftspeed;
  void evalLiftspeed(bool precess = true){
    float mX=0;
    float mY=0;
    if(!averageSmear)foreach(Vector2 v in pastLiftspeed){
      if(MathF.Abs(v.X)>MathF.Abs(mX)) mX=v.X;
      if(MathF.Abs(v.Y)>MathF.Abs(mY)) mY=v.Y;
    } else foreach(Vector2 v in pastLiftspeed){
      mX+=v.X/smearamount; mY+=v.Y/smearamount;
    }
    ownLiftspeed = new Vector2(mX,mY);
    if(!precess) return; 
    for(int i=smearamount-1; i>=1; i--){
      pastLiftspeed[i]=pastLiftspeed[i-1];
    }
    pastLiftspeed[0]=Vector2.Zero;
  }
  public TemplateGluable(EntityData d, Vector2 offset):this(d,offset,d.Int("depthoffset",0)){}
  public TemplateGluable(EntityData d, Vector2 offset, int depthoffset)
  :base(d,d.Position+offset,depthoffset){
    lookingFor = d.Attr("glue_to_identifier");
    smearamount = d.Int("liftspeed_smear",4);
    pastLiftspeed = new Vector2[smearamount];
    averageSmear = d.Bool("smear_average",false);
  }
  static Regex matchReg = new(@"^\s*\d+\s*(?:[\/,]\s*\d+\s*)*$",RegexOptions.Compiled);
  static void Search(EntityData d){
    if(d.Bool("can_be_ID_path",false)){
      string str = d.Attr("glue_to_identifier");
      if(matchReg.Match(str).Success) Finder.watch(str,(e)=>FoundEntity.addIdent(e,str));
    }
  }
  bool added = false;
  public void make(Entity e){
    Depth = e.Depth-1;
    Position = e.Position;
    added=true;
    remake();
  }
  public override void addTo(Scene scene){
    setTemplate(scene:scene);
    scene.Add(this);
  }
  public override void Awake(Scene scene){
    base.Awake(scene);
    if(!added){
      gluedto = FoundEntity.find(lookingFor)?.Entity;
      DebugConsole.Write("found",gluedto);
      if(gluedto != null && gluedto.Scene!=null) make(gluedto);
    }
    
  }
  public override void Update(){
    base.Update();
    if(!added){
      gluedto = FoundEntity.find(lookingFor)?.Entity;
      if(gluedto != null && gluedto.Scene!=null) make(gluedto);
      return;
    }
    if(gluedto.Scene == null){
      gluedto = null;
      destroyChildren(true);
      added=false;
      return;
    } 
    if(gluedto.Position!=Position){
      var move = gluedto.Position-Position;
      pastLiftspeed[0]+=move/Math.Max(Engine.DeltaTime,0.005f);
      evalLiftspeed();
      Position = gluedto.Position;
      childRelposSafe();
    }
    evalLiftspeed(true);
  }
}
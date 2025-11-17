


using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.auspicioushelper.Wrappers;

[Tracked]
public class CassetteW:CassetteBlock, ISimpleEnt{
  public Template parent {get;set;}
  Vector2 ppos;
  public Vector2 toffset {get;set;}
  public CassetteW(EntityData d, Vector2 o, EntityID id):base(d,o.Round(),id){
    hooks.enable();
  }
  void setPosition(Vector2 ls){
    MoveTo((ppos+toffset+Vector2.UnitY*(2-blockHeight)).Round(), (parent?.gatheredLiftspeed??Vector2.Zero)+ls);
  }
  void sposPreadded(Vector2 ls){
    MoveTo((ppos+toffset+Vector2.UnitY*(2-blockHeight)).Round(), ls);
  }
  void ITemplateChild.setOffset(Vector2 l){
    ppos = l;
    toffset = Position-ppos;
  }
  void ITemplateChild.relposTo(Vector2 v, Vector2 ls){
    ppos=v;
    sposPreadded(ls);
  }
  TemplateDisappearer.vcaTracker vca = new();
  void ITemplateChild.parentChangeStat(int vis, int col, int act){
    vca.Align(vis,col,act);
    rectifyCol();
    Active = vca.Active;
    Visible = vca.Visible;
  }
  bool ownCollidable=true;
  void rectifyCol(){
    if(ownCollidable && vca.Collidable == Collidable) return;
    Collidable = ownCollidable && vca.Collidable;
    if(Collidable){
      EnableStaticMovers();
    } else {
      DisableStaticMovers();
    }
  }
  public override void Update() {
    Components.Update();
    Activated = lis.Activated;
    if (groupLeader && Activated && !ownCollidable){
      bool flag = false;
      foreach (CassetteBlock item in group){
        if (item.BlockedCheck()){
          flag = true;
          break;
        }
      }
      if (!flag){
        foreach (CassetteBlock item2 in group) if(item2 is CassetteW w){
          w.ownCollidable = true;
          w.rectifyCol();
          item2.blockHeight=2;
          setPosition(-60*Vector2.UnitY);
        }
        wiggler.Start();
      }
    }
    else if (!Activated && ownCollidable){
      blockHeight = 0;
      ownCollidable = false;
      rectifyCol();
      setPosition(60*Vector2.UnitY);
    }
    //DebugConsole.Write("cassbl",Collidable,Visible,ownCollidable);
    UpdateVisualState();
  }
  void findInGroupGood(CassetteW block){
    foreach (CassetteW entity in base.Scene.Tracker.GetEntities<CassetteW>()){
      if (entity == this || entity == block || entity.Index != Index || entity.parent!=parent) continue;
      if((
        entity.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) || 
        entity.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))
      ) && !group.Contains(entity)){
        group.Add(entity);
        findInGroupGood(entity);
        entity.group = group;
      }
    }
  }
  public bool CheckForSameGood(float x, float y){
    foreach (CassetteW entity in base.Scene.Tracker.GetEntities<CassetteW>()){
      if (entity.Index == Index && entity.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8)) && entity.parent == parent){
        return true;
      }
    }
    return false;
  }
  CassetteListener lis;
  public override void Awake(Scene scene) {
    base.Awake(scene);
    Add(lis=new CassetteListener(Index, Tempo){
      OnWillActivate = ()=>{
        blockHeight = 1;
        setPosition(Vector2.Zero);
        UpdateVisualState();
      },
      OnWillDeactivate = ()=>{
        blockHeight = 1;
        setPosition(60*Vector2.UnitY);
        UpdateVisualState();
      },
      OnStart = (bool activated)=>{
        Activated = (ownCollidable = activated);
        UpdateVisualState();
        if (activated){
          rectifyCol();
          return;
        }
        blockHeight = 0;
        setPosition(Vector2.Zero);
        rectifyCol();
      }
    });
  }
  static void Hook(On.Celeste.CassetteBlock.orig_FindInGroup orig, CassetteBlock s,CassetteBlock o){
    if(s is CassetteW w){
      w.findInGroupGood(w);
    }else orig(s,o);
  }
  static bool Hook(On.Celeste.CassetteBlock.orig_CheckForSame orig, CassetteBlock s,float x, float y){
    if(s is CassetteW w){
      return w.CheckForSameGood(x,y);
    } else return orig(s,x,y);
  }
  static HookManager hooks = new(()=>{
    On.Celeste.CassetteBlock.FindInGroup+=Hook;
    On.Celeste.CassetteBlock.CheckForSame+=Hook;
  }, ()=>{
    On.Celeste.CassetteBlock.FindInGroup-=Hook;
    On.Celeste.CassetteBlock.CheckForSame-=Hook;
  },auspicioushelperModule.OnEnterMap);
}

[Tracked]
[CustomEntity("auspicioushelper/CassetteFixerThing")]
public class CassetteBlockFixer:Entity{
  class WillToggleLock:IDisposable{
    static int count;
    public static bool locked=>count>0;
    public WillToggleLock(){count++;}
    void IDisposable.Dispose()=>count--;
  }
  [Tracked]
  class OrigHeightComp:Component{
    public int h;
    public bool toUse=false;
    public OrigHeightComp(int orig):base(false,false){
      h=orig;
    }
    public override void EntityAdded(Scene scene) {
      base.EntityAdded(scene);
      if(scene.Tracker.GetEntity<CassetteBlockFixer>() is {}) toUse=true;
    }
  }
  public CassetteBlockFixer():base(Vector2.Zero){
    hooks.enable();
  }
  public override void Awake(Scene scene) {
    base.Awake(scene);
    foreach(OrigHeightComp h in scene.Tracker.GetComponents<OrigHeightComp>()) h.toUse=true;
  }
  static void Hook(On.Celeste.CassetteBlock.orig_WillToggle orig, CassetteBlock b){
    using(new WillToggleLock()) orig(b);
  }
  static void Hook(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock b, int shift){
    if(b.Get<OrigHeightComp>() is not {} oh || !oh.toUse){
      orig(b,shift);
      return;
    }
    if(WillToggleLock.locked) b.blockHeight=1;
    else b.blockHeight = b.Activated?2:0;
    b.MoveToY(oh.h+2-b.blockHeight);
  }
  static void Hook(On.Celeste.CassetteBlock.orig_ctor_Vector2_EntityID_float_float_int_float orig, CassetteBlock s, Vector2 o, EntityID id, float a, float b, int c, float d){
    orig(s,o,id,a,b,c,d);
    if(s.GetType()==typeof(CassetteBlock)){
      s.Add(new OrigHeightComp((int)Math.Round(s.Position.Y)));
    }
  }
  static HookManager hooks = new(()=>{
    On.Celeste.CassetteBlock.ShiftSize+=Hook;
    On.Celeste.CassetteBlock.WillToggle+=Hook;
    On.Celeste.CassetteBlock.ctor_Vector2_EntityID_float_float_int_float+=Hook;
  },()=>{
    On.Celeste.CassetteBlock.ShiftSize-=Hook;
    On.Celeste.CassetteBlock.WillToggle-=Hook;
    On.Celeste.CassetteBlock.ctor_Vector2_EntityID_float_float_int_float-=Hook;
  },auspicioushelperModule.OnEnterMap);
}




using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TrackedCassette")]
public class TrackedCassette:Cassette{
  public bool dontUnlockBside;
  public bool useRawMessage;
  public bool alwaysShow;
  public string message="";
  public string id;
  public string flag;
  public int messageLineWidth = 900;
  public TrackedCassette(EntityData d, Vector2 pos):base(d,pos){
    dontUnlockBside = d.Bool("dont_unlock_bside",false);
    useRawMessage = d.Bool("use_raw_message", true);
    alwaysShow = d.Bool("always_show",false);
    message = d.Attr("message", "");
    id = d.Attr("identifier", "");
    messageLineWidth = d.Int("line_width",900);
    flag = d.Attr("flag","");
    hooks.enable();
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(auspicioushelperModule.Session.collectedTrackedCassettes.Contains(id)){
      scene.Remove(this);
    }
  }


  private static bool CheckGhost(Cassette c){
    if(c is TrackedCassette d){
      return auspicioushelperModule.SaveData.collectedTrackedCassettes.Contains(d.id);
    }
    return true;
  }
  private static void IL_AddedHook(ILContext il){
    var cursor = new ILCursor(il);
    if(!cursor.TryGotoNext(MoveType.Before, instr=>instr.MatchStfld<Cassette>("IsGhost"))){
      DebugConsole.Write("Failed adding hook for TrackedCassette::Added");
      return;
    }
    cursor.Emit(OpCodes.Ldarg_0);
    cursor.EmitDelegate(CheckGhost);
    cursor.Emit(OpCodes.And);
  }

  private static bool interceptNextSave;
  private static void registerCassetteHook(On.Celeste.SaveData.orig_RegisterCassette orig, SaveData s, AreaKey a){
    if(interceptNextSave){
      interceptNextSave = false;
      return;
    }
    orig(s,a);
  }
  private static string interceptNextMessage = "";
  private static void CassetteMessageAdded(On.Celeste.Cassette.UnlockedBSide.orig_Added orig, Entity self, Scene s){
    orig(self, s);
    if(!string.IsNullOrEmpty(interceptNextMessage)){
      if(self is Cassette.UnlockedBSide m){
        m.text = interceptNextMessage;
        interceptNextMessage = "";
      } else {
        DebugConsole.Write("funny error");
      }
    }
  }
  private static IEnumerator collectRoutineHook(On.Celeste.Cassette.orig_CollectRoutine orig, Cassette self, Player player){
    bool priorSessionstate = self.SceneAs<Level>().Session.Cassette;
    if(self is TrackedCassette c){
      if(c.dontUnlockBside){
        interceptNextSave = true;
      }
      if(!string.IsNullOrEmpty(c.message)){
        if(c.useRawMessage == true){
          interceptNextMessage = ActiveFont.FontSize.AutoNewline(c.message, c.messageLineWidth);
        } else {
          interceptNextMessage = ActiveFont.FontSize.AutoNewline(Dialog.Clean(c.message), c.messageLineWidth);
        }
      }
      auspicioushelperModule.SaveData.collectedTrackedCassettes.Add(c.id);
      auspicioushelperModule.Session.collectedTrackedCassettes.Add(c.id);
      if(!string.IsNullOrEmpty(c.flag)){
        self.SceneAs<Level>().Session.SetFlag(c.flag,true);
      }
    }
    yield return new SwapImmediately(orig(self, player));
    if(self is TrackedCassette){
      self.SceneAs<Level>().Session.Cassette = priorSessionstate;
    }
  }
  public static HookManager hooks = new HookManager(()=>{
    IL.Celeste.Cassette.Added+=IL_AddedHook;
    On.Celeste.Cassette.CollectRoutine+=collectRoutineHook;
    On.Celeste.SaveData.RegisterCassette+=registerCassetteHook;
    On.Celeste.Cassette.UnlockedBSide.Added += CassetteMessageAdded;
  },void()=>{
    IL.Celeste.Cassette.Added-=IL_AddedHook;
    On.Celeste.Cassette.CollectRoutine-=collectRoutineHook;
    On.Celeste.SaveData.RegisterCassette-=registerCassetteHook;
    On.Celeste.Cassette.UnlockedBSide.Added-=CassetteMessageAdded;
  }, auspicioushelperModule.OnEnterMap);
}
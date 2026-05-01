

using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.ModInterop;

namespace Celeste.Mod.auspicioushelper;

[ModImportName("GravityHelper")]
public class GelperIop{
  public static Func<string, int> GravityTypeToInt;
  public static Action<Actor,int,float> SetActorGravity;
  public static Func<Actor,bool> IsActorInverted;
  public static Action BeginOverride;
  public static Action EndOverride;
  public static Func<bool> IsPlayerInverted;
  public static bool PlayerFlipped=>IsPlayerInverted is {} f? f():false;
  public static Vector2 downVec(Actor a)=>IsActorInverted is { } f && f(a)? -Vector2.UnitY:Vector2.UnitY;
  public ref struct MoveOverride:IDisposable{
    public MoveOverride(){
      if(BeginOverride is {} f) f();
    }
    void IDisposable.Dispose() {
      if(EndOverride is {} f) f();
    }
  }


  public static void TryFlip(Actor a){
    if(SetActorGravity!=null) SetActorGravity(a,2,1);
  }
  public static bool IsFlipped(Actor a)=>(IsActorInverted is {} fn)?fn(a):false;
  public static Type gravityComponentType;
  public static FieldInfo OnChangeVisuals;
  internal static Func<Action<bool, int>,object> wrapFunc;
  [OnLoad]
  static void Load(){
    typeof(GelperIop).ModInterop();
    if(BeginOverride != null){
      gravityComponentType = Util.getModdedType("GravityHelper","Celeste.Mod.GravityHelper.Components.GravityComponent");
      OnChangeVisuals = gravityComponentType.GetField("UpdateVisuals");
      
      Type actionType = OnChangeVisuals.FieldType;
      Type gravChangeType  = actionType.GetGenericArguments()[0];
      PropertyInfo changed = gravChangeType.GetProperty("Changed");
      FieldInfo dir = gravChangeType.GetField("NewValue");

      var infunc = Expression.Parameter(typeof(Action<bool,int>));
      var gravChange = Expression.Parameter(gravChangeType);
      var changed_ = Expression.Property(gravChange, changed);
      var ntype = Expression.Field(gravChange, dir);
      var tstr = Expression.Convert(ntype, typeof(int));
      var invoke = Expression.Invoke(infunc, [changed_, tstr]);

      var lambda = Expression.Lambda(actionType, invoke, [gravChange]);
      var outType = typeof(Func<,>).MakeGenericType(typeof(Action<bool,int>), actionType);
      var thing = Expression.Lambda(outType, lambda, infunc).Compile();

      wrapFunc = (Func<Action<bool, int>,object>) thing;
    }
  }
}
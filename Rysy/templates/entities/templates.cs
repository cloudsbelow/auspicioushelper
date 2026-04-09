



using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Rysy;
using Rysy.Graphics;
using Rysy.LuaSupport;

namespace auspicioushelper.Rysy;

public class Template:LonnEntity {
  public virtual string BaseTemplateSprite=>"tblk";
  public const string SpriteDirPath = "loenn/auspicioushelper/template/";
  static Dictionary<string,ColoredSpriteTemplate> cachedSpriteTempaltes = new();
  public static ConditionalWeakTable<Room,Dictionary<Vector2,Template>> emptyTemplates = new();
  public override void OnChanged(EntityDataChangeCtx changed){
    emptyTemplates.Remove(Room);
    base.OnChanged(changed);
  }
  public override IEnumerable<ISprite> GetSprites(){
    string path = SpriteDirPath+BaseTemplateSprite;
    if(!cachedSpriteTempaltes.TryGetValue(path, out var st)){
      st = SpriteTemplate.FromTexture(path,-13000).Centered().CreateColoredTemplate(Color.White);
      cachedSpriteTempaltes.Add(path, st);
    }
    return st.Create(Pos);
  }
}
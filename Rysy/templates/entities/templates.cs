



using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Rysy;
using Rysy.Graphics;
using Rysy.LuaSupport;
using Rysy.Shared;

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
    //auspicioushelperModule.TryLoad();
    string path = SpriteDirPath+BaseTemplateSprite;
    if(!cachedSpriteTempaltes.TryGetValue(path, out var st)){
      st = SpriteTemplate.FromTexture(path,-13000).Centered().CreateColoredTemplate(Color.White);
      cachedSpriteTempaltes.Add(path, st);
    }
    IEnumerable<ISprite> sprites = [st.Create(Pos)];
    string tstr = Attr("template");
    if(string.IsNullOrWhiteSpace(tstr)) return sprites;
    TemplateFiller filler = TemplateFiller.getFiller(tstr,this);
    ausModule.Log(LogLevel.Info, "got template", tstr, filler);
    if(filler==null) return sprites;
    sprites = sprites.Concat(filler.renderAt(Int2.Round(Pos)));
    return sprites;
  }
}
public class NodedTemplate:Template{
  
}
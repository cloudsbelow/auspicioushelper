


using Microsoft.Xna.Framework;
using Rysy;
using Rysy.Graphics;

namespace auspicioushelper.Rysy.TemplateEntities;
[CustomEntity("auspicioushelper/TemplateBehaviorChain",["auspicioushelper"])]
public class TemplateBehaviorChain:Template{
  static ColoredSpriteTemplate filledBracket = SpriteTemplate.FromTexture(SpriteDirPath+"tgroupnode",-13000).Centered().CreateColoredTemplate(Color.White);
  static ColoredSpriteTemplate empty = SpriteTemplate.FromTexture(SpriteDirPath+"group_error",-13001).Centered().CreateColoredTemplate(Color.White);
  static ColoredSpriteTemplate emptyBracket = SpriteTemplate.FromTexture(SpriteDirPath+"tgroupnode",-13000).Centered().CreateColoredTemplate(new(1,0.25f,0.25f));
  public override string BaseTemplateSprite => "tgroup";
  public override IEnumerable<ISprite> GetSprites() {
    return base.GetSprites();
  }
  List<ISprite> cachedSprites = null;
  public override void OnChanged(EntityDataChangeCtx changed) {
    base.OnChanged(changed);
    cachedSprites = null;
  } 
  public override IEnumerable<ISprite> GetAllNodeSprites(){
    if(!emptyTemplates.TryGetValue(Room, out var ena)){
      emptyTemplates.Add(Room, ena = new());
      foreach(var t in Room.Entities.OfType<Template>()){
        if(t is TemplateBehaviorChain tbc) tbc.cachedSprites = null; 
        if(string.IsNullOrWhiteSpace(t.Attr("template","")) && !ena.TryAdd(t.Pos,t)){
          //some error? todo: return some graphic
        }
      }
    }
    if(cachedSprites==null) FillCache(ena);
    return cachedSprites;
  }
  void FillCache(Dictionary<Vector2,Template> ena){
    cachedSprites = new();
    if(Nodes is {} nodes) foreach(var n in nodes){
      if(ena.ContainsKey(n.Pos)) cachedSprites.Add(filledBracket.Create(n.Pos-Vector2.UnitY*3));
      else{
        cachedSprites.Add(empty.Create(n.Pos));
        cachedSprites.Add(emptyBracket.Create(n.Pos-Vector2.UnitY*3));
      } 
    }
  }
}
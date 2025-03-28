


using Celeste.Mods.auspicioushelper;
using Monocle;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mods.auspicioushelper;

public interface IMaterialObject{
  public void registerMaterials();
  public void renderMaterial(MaterialLayer l, SpriteBatch sb, Camera c);
}
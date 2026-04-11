

using Rysy;

namespace auspicioushelper.Rysy.TemplateEntities;

[CustomEntity("auspicioushelper/TemplateBlock", ["auspicioushelper"])]
public class TempalteBlock:Template{
  public override string BaseTemplateSprite => "tblk";
}

[CustomEntity("auspicioushelper/TemplateCassetteBlock", ["auspicioushelper"])]
public class TempalteCassetteBlock:Template{
  public override string BaseTemplateSprite => "tcass";
}

[CustomEntity("auspicioushelper/TemplateDashhitModifier", ["auspicioushelper"])]
public class TemplateDashhitModifier:Template{
  public override string BaseTemplateSprite => "tdash";
}

[CustomEntity("auspicioushelper/TemplateEntityModifier", ["auspicioushelper"])]
public class TemplateEntityModifier:Template{
  public override string BaseTemplateSprite => "tentmod";
}

[CustomEntity("auspicioushelper/TemplateFakewall", ["auspicioushelper"])]
public class TemplateFakewall:Template{
  public override string BaseTemplateSprite => "tfake";
}

[CustomEntity("auspicioushelper/TemplateFallingblock", ["auspicioushelper"])]
public class TemplateFallingblock:Template{
  public override string BaseTemplateSprite => "tfall";
}

[CustomEntity("auspicioushelper/TemplateIceblock", ["auspicioushelper"])]
public class TemplateIceblock:Template{
  public override string BaseTemplateSprite => "tcore";
}

[CustomEntity("auspicioushelper/TemplateKevin", ["auspicioushelper"])]
public class TemplateKevin:Template{
  public override string BaseTemplateSprite => "tkevin";
}

[CustomEntity("auspicioushelper/TemplateCloud", ["auspicioushelper"])]
public class TemplateCloud:Template{
  public override string BaseTemplateSprite => "tcloud";
}

[CustomEntity("auspicioushelper/TemplateMoonblock", ["auspicioushelper"])]
public class TemplateMoonblock:Template{
  public override string BaseTemplateSprite => "tmoon";
}

[CustomEntity("auspicioushelper/TemplateTemplate", ["auspicioushelper"])]
public class TempalteTemplate:Template{
  public override string BaseTemplateSprite => "tsub";
}

[CustomEntity("auspicioushelper/TemplateTriggerModifier", ["auspicioushelper"])]
public class TemplateTriggerModifier:Template{
  public override string BaseTemplateSprite => "ttrig";
}

[CustomEntity("auspicioushelper/TemplateResetter", ["auspicioushelper"])]
public class TemplateResetter:Template{
  public override string BaseTemplateSprite => "treset";
}

[CustomEntity("auspicioushelper/MaterialTemplate", ["auspicioushelper"])]
public class MaterialTemplate:Template{
  public override string BaseTemplateSprite => "tmat";
}

[CustomEntity("auspicioushelper/TemplateMoveblock", ["auspicioushelper"])]
public class TemplateArrowblock:Template{
  public override string BaseTemplateSprite => "tmat";
}

[CustomEntity("auspicioushelper/TemplateStaticmover", ["auspicioushelper"])]
public class TemplateStaticmover:Template{
  public override string BaseTemplateSprite => "tmat";
}


[CustomEntity("auspicioushelper/TemplateChannelmover",["auspicioushelper"])]
public class TemplateChannelmover:NodedTemplate{
  public override string BaseTemplateSprite => "tchan";
}

[CustomEntity("auspicioushelper/TemplateBelt",["auspicioushelper"])]
public class TemplateBelt:NodedTemplate{
  public override string BaseTemplateSprite => "tconv";
}

[CustomEntity("auspicioushelper/TemplateGluable",["auspicioushelper"])]
public class TemplateGluable:NodedTemplate{
  public override string BaseTemplateSprite => "tglue";
}

[CustomEntity("auspicioushelper/TemplateSwapblock",["auspicioushelper"])]
public class TemplateSwapblock:NodedTemplate{
  public override string BaseTemplateSprite => "tswap";
}

[CustomEntity("auspicioushelper/TemplateZipmover",["auspicioushelper"])]
public class TemplateZipmover:NodedTemplate{
  public override string BaseTemplateSprite => "tzip";
}
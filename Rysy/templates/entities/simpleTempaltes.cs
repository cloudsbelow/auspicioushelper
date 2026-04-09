

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
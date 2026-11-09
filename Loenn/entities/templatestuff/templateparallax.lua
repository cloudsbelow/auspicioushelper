local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateParallax")
entity.depth = -13000
entity.nodeLimits = {1,1}
entity.nodeLineRenderType = "fan"
entity.nodeVisibility = "always"


entity.placements = {
  {
    name = "Template Parallax",
    data = {
      template = "",
      depthoffset=0,
      displaceInto=true,
      alwaysForcepos=true,
      factor=0.5,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldOrder = {
  "x","y", "template","depthoffset"
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },
    }
end

entity.selection = aelperLib.template_selection
entity.texture = "loenn/auspicioushelper/template/tstat_legacy"
entity.nodeTexture = "loenn/auspicioushelper/template/tmat"

return entity
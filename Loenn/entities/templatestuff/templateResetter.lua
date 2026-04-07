local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateResetter")
entity.depth = -13000


entity.placements = {
  {
    name = "Template Resetter",
    data = {
      template = "",
      depthoffset=0,
      resetChannel="",
      destroyChannel="",
      particles=true,
      --resetKeepsOld=false,
      resetOnTrigger=false,
      startWith=true,
      blockedByPlayer=false,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldOrder = {
  "x","y", "template","depthoffset","resetChannel","destroyChannel"
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },
    }
end

entity.selection = aelperLib.template_selection
entity.texture = "loenn/auspicioushelper/template/treset"

return entity
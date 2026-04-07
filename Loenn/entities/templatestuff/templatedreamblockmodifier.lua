local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateDreamblockModifier")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Dreamblock Modifier",
    data = {
      template = "",
      depthoffset=0,
      triggerOnEnter=true,
      triggerOnLeave=true,
      normalChannel="",
      customVisualGroup = "",
      reverse=false,
      conserve=false,
      useVisuals=true,
      allowTransition=false,
      tryDashhit=true,
      sendDashhit=false,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldOrder = {
  "x","y", "template","depthoffset", "normalChannel","customVisualGroup","reverse","conserve","triggerOnEnter","triggerOnLeave"
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tdream")

return entity
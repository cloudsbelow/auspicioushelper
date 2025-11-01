local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateFakewall")
entity.depth = -13000

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=0,
      freeze = false,
      dontOnTransitionInto = false,
      disappear_depth = -13000,
      fade_speed = 1,
      persistent = true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end
entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tfake")

return entity
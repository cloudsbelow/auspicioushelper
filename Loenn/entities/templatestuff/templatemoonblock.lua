local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateMoonblock")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Moonblock",
    data = {
      template = "",
      depthoffset=0,
      drift_frequency=1,
      drift_amplitude=4,
      sink_amount=12,
      sink_speed=1,
      dash_influence=8,
      startphase=0,
      useCustomStartphase=false,
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
entity.draw = aelperLib.get_entity_draw("tmoon")

return entity
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
      depthoffset=5,
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

function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tmoon")

return entity
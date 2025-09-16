local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateIceblock")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Iceblock",
    data = {
      template = "",
      depthoffset=5,
      sinkTime=1,
      sinkDist=12,
      respawnTime=1.6,
      triggerable=true,
      ridingTriggers=true,
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
entity.draw = aelperLib.get_entity_draw("tcore")

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateGluable")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Gluable",
    data = {
      template = "",
      depthoffset=5,
      channel = "",
      glue_to_identifier ="",
      liftspeed_smear = 4,
      smear_average = false,
      
      _loenn_display_template = true,
    }
  }
}

function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tstat")

return entity
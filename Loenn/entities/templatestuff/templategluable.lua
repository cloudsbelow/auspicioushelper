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
      depthoffset=0,
      channel = "",
      glue_to_identifier ="",
      can_be_ID_path = true,
      liftspeed_smear = 4,
      smear_average = false,
      onlyX = false,
      onlyY = false,
      
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
entity.draw = aelperLib.get_entity_draw("tglue")

return entity
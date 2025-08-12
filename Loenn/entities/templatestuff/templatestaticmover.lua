local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateStaticmover")
entity.depth = -13000

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=5,
      channel = "",
      liftspeed_smear = 4,
      smear_average = false,
      ridingTrigger = true,
      EnableUnrooted = false,
      conveyRiding = false,
      
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
entity.draw = aelperLib.get_entity_draw("tstat")

return entity
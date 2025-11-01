local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateKevin")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Kevin",
    data = {
      template = "",
      depthoffset=0,
      maxspeed=240,
      acceleration=500,
      returnSpeed=60,
      leniency=4,
      left=true,
      right=true,
      top=false,
      bottom=false,
      returning=true,
      ImpactSfx="event:/game/general/fallblock_impact",
      StartSfx="event:/new_content/game/10_farewell/fusebox_hit_1",
      hitJumpthrus=true,
      throughDashblocks=true,
      
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
entity.draw = aelperLib.get_entity_draw("tkevin")

return entity
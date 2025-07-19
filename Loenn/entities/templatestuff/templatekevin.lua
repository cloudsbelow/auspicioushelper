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
      depthoffset=5,
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
      
      _loenn_display_template = true,
    }
  }
}
function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tkevin")

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateFallingblock")
entity.depth = -13000

local directions = {"down","up","left","right"}

entity.placements = {
  {
    name = "main",
    data = {
      template = "",
      depthoffset=5,
      direction="down",
      reverseChannel="",
      triggerChannel="",
      gravity = 500,
      max_speed = 130,
      impact_sfx = "event:/game/general/fallblock_impact",
      shake_sfx = "event:/game/general/fallblock_shake",
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = {
  direction = {
    options = directions,
    editable=false
  },
  impact_sfx = {options = {"event:/game/general/fallblock_impact"}},
  shake_sfx = {options = {"event:/game/general/fallblock_shake"}},
}

function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tfall")

return entity
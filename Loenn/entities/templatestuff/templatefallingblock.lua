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
      set_trigger_channel=false,
      hitJumpthrus=true,
      triggeredByRiding=true,
      throughDashblocks=true,
      customFallTiming="0.25,0.1",
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
  direction = {
    options = directions,
    editable=false
  },
  impact_sfx = {options = {"event:/game/general/fallblock_impact"}},
  shake_sfx = {options = {"event:/game/general/fallblock_shake"}},
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tfall")

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}
local easings = {"Linear","SineIn","SineOut","SineInOut","QuadIn","QuadOut","CubeIn","CubeOut","Smoothstep","QuartIn","QuartOut","QuintIn","QuintOut"}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateRotator")
entity.depth = -13000

entity.placements = {
  {
    name = "Template Rotator",
    data = {
      template = "",
      depthoffset=0,
      moveTimes="1",
      moveDegrees="180",
      pauseAmounts="0",
      initialDelay="0",
      radius="40",
      manual="",
      easing = "Linear",
      shakeTimes = "",
      count = 2,
      offsets = "",
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldOrder = {
  "x","y", "template","depthoffset", "moveTimes","moveDegrees", "pauseAmounts","initialDelay","radius","manual","easing","shakeTimes", "count","offsets"
}
entity.fieldInformation = function(entity)
    return {
        template = {
            options = aelperLib.get_template_options(entity)
        },
        easing = {options = easings, editable=false}
    }
end

entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tconv")

return entity
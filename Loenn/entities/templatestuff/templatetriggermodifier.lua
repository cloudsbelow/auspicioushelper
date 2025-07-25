local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateTriggerModifier")
entity.depth = -13000
entity.fieldInformation = {
    advancedTouchOptions={
        fieldType="list",
        elementDefault="jump",
        elementOptions = {
            fieldType = "string",
            options={
                "collideV", 
                "collideH", 
                "jump", 
                "climbjump", 
                "walljump", 
                "wallbounce", 
                "super", 
                "grounded", 
                "climbing"
            }
        }
    }
}

entity.placements = {
  {
    name = "Template Trigger Modifier",
    data = {
      template = "",
      depthoffset=5,
      advancedTouchOptions = "",
      triggerOnTouch = false,
      channel = "",
      setChannel = "",
      propagateRiding = true,
      propagateInside = true,
      propagateShake = true,
      propagateDashHit = true,
      propagateTrigger = true,
      hideTrigger = false,
      blockTrigger = false,
      delay = -1,
      blockFilter = "",
      log=false,
      
      _loenn_display_template = true,
    }
  }
}
function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("ttrig")

return entity
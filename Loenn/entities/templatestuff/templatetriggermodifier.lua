local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateTriggerModifier")
entity.depth = -13000

entity.fieldInformation = function(entity)
    return {
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
                "climbing",
                "dashH",
                "dashV",
            }
        }
    },
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end

entity.placements = {
  {
    name = "Template Trigger Modifier",
    data = {
      template = "",
      depthoffset=0,
      advancedTouchOptions = "",
      triggerOnTouch = false,
      channel = "",
      setChannel = "",
      skipChannel = "",
      propagateRiding = true,
      propagateInside = true,
      propagateShake = true,
      propagateDashHit = true,
      propagateTrigger = true,
      hideTrigger = false,
      blockTrigger = false,
      seekersTrigger = false,
      holdablesTrigger = false,
      useAdvancedSetch = false,
      delay = -1,
      blockFilter = "",
      log=false,
      
      _loenn_display_template = true,
    }
  }
}
entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("ttrig")

return entity
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
                "FishExplosion",
                "SeekerExplosion",
                "bumper",
                "SeekerSlam",
                "HoldableHit",
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
      channel = "",
      setChannel = "",
      skipChannel = "",
      propagateRiding = true,
      propagateTrigger = true,
      hideTrigger = false,
      delay = -1,
      blockFilter = "",
      neverTriggerOnAwake = false,
      log=false,
      collideWith="",
      
      _loenn_display_template = true,
    }
  }
}
entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("ttrig")
entity.fieldOrder = {
    "x", "y", "template", "depthoffset", "channel", "setChannel",
    "advancedTouchOptions", "blockFilter", "delay", "skipChannel"

}
return entity
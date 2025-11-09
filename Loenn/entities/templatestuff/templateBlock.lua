local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local aelperLib = require("mods").requireFromPlugin("libraries.aelper_lib")

local entity = {}

entity.name = aelperLib.register_template_name("auspicioushelper/TemplateBlock")
entity.depth = -13000

local sfxs = {
  "event:/game/general/wall_break_dirt",
  "event:/game/general/wall_break_ice",
  "event:/game/general/wall_break_wood",
  "event:/game/general/wall_break_stone"
}

entity.placements = {
  {
    name = "Template Dashblock",
    data = {
      template = "",
      depthoffset=0,
      only_redbubble_or_summit_launch = false,
      persistent = false,
      canbreak = true,
      breaksfx = "event:/game/general/wall_break_stone",
      triggerable = false,
      triggerOnBreak = true,
      breakableByBlocks = true,
      
      _loenn_display_template = true,
    }
  }, {
    name = "Template Block (Triggerable)",
    data = {
      template = "",
      depthoffset=0,
      visible = true,
      collidable = true,
      active = true,
      only_redbubble_or_summit_launch = false,
      persistent = false,
      canbreak = false,
      propagateRiding = true,
      propagateShaking = true,
      breaksfx = "event:/game/general/wall_break_stone",
      exitBlockBehavior = false,
      triggerable = true,
      triggerOnBreak = false,
      breakableByBlocks = true,
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = function(entity)
    return {
        breaksfx ={
            options = sfxs,
        },
        template = {
            options = aelperLib.get_template_options(entity)
        }
    }
end
entity.selection = aelperLib.template_selection
entity.draw = aelperLib.get_entity_draw("tblk")

return entity
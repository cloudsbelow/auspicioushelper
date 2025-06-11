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
    name = "Template Block",
    data = {
      template = "",
      depthoffset=5,
      visible = true,
      collidable = true,
      active = true,
      only_redbubble_or_summit_launch = false,
      persistent = false,
      canbreak = true,
      propagateRiding = true,
      propagateShaking = true,
      breaksfx = "event:/game/general/wall_break_stone",
      
      _loenn_display_template = true,
    }
  }
}
entity.fieldInformation = {
  breaksfx ={
    options = sfxs,
  },
}
function entity.rectangle(room, entity)
    return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end
entity.draw = aelperLib.get_entity_draw("tblk")

return entity
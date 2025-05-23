local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/TemplateBlock"
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
      breaksfx = "event:/game/general/wall_break_stone"
    }
  }
}
entity.fieldInformation = {
  breaksfx ={
    options = sfxs,
  }
}
entity.texture = "loenn/auspicioushelper/template/tblk"

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x-6, entity.y-6, 12, 12)
end
--entity.fillColor = {1,0.3,0.3}

return entity
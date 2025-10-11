local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/triggerparenttrigger"
entity.depth = 2000

entity.placements = {
  {
    name = "Trigger Parent Trigger",
    data = {
      onEnter=true,
      onLeave=false,
    }

  }
}
return entity

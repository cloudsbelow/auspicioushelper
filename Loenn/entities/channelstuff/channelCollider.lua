local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/FinderCollider"
entity.depth = 2000

entity.placements = {
  {
    name = "channel collider swapper",
    data = {
      path = "0",
      channel = "",
      collider = "rect:[-8,-8,16,16]"
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/marker"

return entity
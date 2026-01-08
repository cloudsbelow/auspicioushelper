local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/CollisionCounter"
entity.depth = 2000

entity.placements = {
  {
    name = "channel collision counter",
    data = {
      groupA = "",
      groupB = "",
      onlyCollidableA = true,
      onlyCollidableB = true,
      channel = "numCollisions"
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/marker"

return entity
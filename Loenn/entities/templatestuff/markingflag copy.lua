local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/FinderDepth"
entity.depth = 2000

entity.placements = {
  {
    name = "id based depth thing",
    data = {
      path = "0",
      depth = 1,
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/marker"

return entity
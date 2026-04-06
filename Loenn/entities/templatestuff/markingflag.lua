local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/EntityMarkingFlag"
entity.depth = 2000

entity.placements = {
  {
    name = "main",
    data = {
      path = "",
      identifier = ""
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/marker"

return entity
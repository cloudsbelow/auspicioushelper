local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/EvilPackedTemplateRoom"
entity.depth = 2000

entity.placements = {
  {
    name = "Evil packed template rooms (evil)",
    data = {
      EncodedRooms = ""
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/evilrooms"

return entity



local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/spinner"
entity.depth = 2000

entity.placements = {
  {
    name = "Recolorable spinner",
    data = {
      customColor = "ffffff",
      makeFiller = true,
    }
  }
}

entity.texture = "danger/crystal/fg_white00"

return entity
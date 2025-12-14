local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/MaterialApplier"
entity.depth = 2000

entity.placements = {
  {
    name = "Material Applier",
    data = {
      can_be_ID_path = true,
      identifier = "fg",
      materialLayer = "",
      dontNormalRender = true,
      toggleChannel = "",
      priority = -1,
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/matadd"

return entity
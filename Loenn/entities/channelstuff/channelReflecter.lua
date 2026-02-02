


local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelReflector"
entity.depth = 2000

entity.placements = {
  {
    name = "channel reflector",
    data = {
      path = "player",
      channel = "",
      logAccessible = false,
      valueIfNull = 0,
      access = "Top",
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/marker"

return entity
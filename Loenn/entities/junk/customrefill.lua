


local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/CustomRefill"
entity.depth = 2000

entity.placements = {
  {
    name = "(Another) custom refill",
    data = {
      respawnTimer = 2.5,
      oneUse = false,
      twoDash = false,
      triggering=false,
      useOnPickup = true,
      useOnRelease = true,
      numRefresh = -1,
    }
  }
}

entity.texture = "objects/refill/idle03"

return entity
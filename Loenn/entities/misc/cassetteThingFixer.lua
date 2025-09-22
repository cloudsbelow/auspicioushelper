local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/CassetteFixerThing"
entity.depth = -100000

entity.placements = {
  {
    name = "Cassette block fixing thing idk",
    data = {
    }
  }
}

entity.texture = "/loenn/auspicioushelper/cassettemanager_simple"

return entity
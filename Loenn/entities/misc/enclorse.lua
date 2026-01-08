
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/enclorse"
entity.depth = -100000

entity.placements = {
  {
    name = "enclorse",
    data = {
      channel="enclosed_horse"
    }
  }
}

entity.texture = "/objects/auspicioushelper/horse/n1"

return entity
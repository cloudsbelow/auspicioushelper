local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/lavasandwichAligner"
entity.depth = 2000

entity.placements = {
  {
    name = "main",
    data = { }
  }
}

entity.texture = "loenn/auspicioushelper/sandwich_aligner"

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x-12, entity.y-12, 24,24)
end

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/CassetteGate"
entity.depth = -9000

entity.placements = {
  {
    name = "main",
    data = {
      width = 8,
      height= 8,
      horizontal = false
    }
  }
}

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end
entity.fillColor = {0.8, 0.8, 0.8, 1}
entity.borderColor = {1,1,1,1}

return entity
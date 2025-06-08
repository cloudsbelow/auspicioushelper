local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/Anti0fZone"
entity.depth = -11000
entity.nodeLimits = {0,1}

entity.placements = {
  {
    name = "main",
    data = {
      width = 8,
      height = 8,
      holdables=false,
      player_colliders = true,
      always_walljumpcheck = false,
      triggers = false,
      solids = false,
      cover_whole_room=false
    }
  }
}
function entity.rectangle(room, entity)
  return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end
entity.fillColor = {1,0.6,0.2,0.2}
entity.borderColor = {0.7,0.4,0.1,0.7}
entity.fieldOrder = {
    "x", "y", "width", "height",
    "player_colliders",
    "holdables",
    "triggers",
    "solids",
    "always_walljumpcheck",
    "cover_whole_room"
}

return entity
local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ConnectedContainer"
entity.depth = 2000

entity.placements = {
  {
    name = "Connected Container (Template)",
    data = {
        width = 8,
        height = 8,
        filterEntities="",
        filterDecals="",
        getEntities=true,
        getDecals=false,
    }
  }
}

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end
entity.fillColor = {0,0.5,1,0.15}
entity.borderColor = {0,0.5,1,0.5}

return entity
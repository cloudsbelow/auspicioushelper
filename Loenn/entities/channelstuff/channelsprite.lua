local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelSprite"
entity.depth = 2000

edge_types = {"loop","clamp","hide"}
entity.placements = {
  {
    name = "Channel Sprite",
    data = {
      channel = "",
      attached = false,
      edge_type = "loop",
      xml_spritename = "auspicioushelper_example1",
      cases=3,
      offsetX=0,
      offsetY=0,
      depth=2
    }
  }
}
entity.fieldInformation = {
  edge_type = {
    options = edge_types
  },
  cases = {
    fieldType="integer"
  },
  offsetX = {
    fieldType="integer"
  },
  offsetY = {
    fieldType="integer"
  },
  depth = {
    fieldType="integer"
  }
}
entity.texture = "loenn/auspicioushelper/controllers/sprite"

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x, entity.y, 8, 8)
end

return entity
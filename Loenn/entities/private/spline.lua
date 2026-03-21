

local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/Spline"
entity.depth = 2000
entity.nodeLimits = {1,-1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "main",
    data = {
      identifier = "",
      spline_type = "basic",
      last_node_knot = false
    }
  }
}
local types = {"linear","basic"}
entity.fieldInformation = {
  spline_type ={
    options = types
  },
}

entity.color = {137/255, 242/255, 124/255}
function entity.rectangle(room, entity)
  return utils.rectangle(entity.x-4, entity.y-4, 8, 8)
end

return entity
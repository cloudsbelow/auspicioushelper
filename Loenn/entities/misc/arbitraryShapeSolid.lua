local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ArbitrarySolid"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

local rotations = {0,90,180,270}
entity.placements = {
  {
    name = "Arbitrary shape solid",
    data = {
      safe=false,
      image = "decals/9-core/fossil_a",
      CustomColliderPath = "",
      flipH=false,
      flipV=false,
      color="fff",
      rotation=0,
      depth=-100,
    }
  }
}
entity.fieldInformation = {
    rotation = {
      options = rotations,
      editable = false,
    }
  }
function entity.texture(room, entity)
  return entity.image
end

return entity
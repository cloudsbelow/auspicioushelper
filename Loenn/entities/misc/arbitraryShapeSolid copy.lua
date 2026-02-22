local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ArbitraryDie"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Arbitrary shape kill area",
    data = {
      safe=false,
      image = "decals/9-core/fossil_a",
      CustomColliderPath = "",
      scaleX=1,
      scaleY=1,
      color="fff",
      rotation=0,
      depth=-100,
      alphaCutoff=0.5,
    }
  }
}

function entity.texture(room, entity)
  return entity.image
end
function entity.scale(room, entity)
  return {
    entity.scaleX or (entity.flipH and -1) or 1,
    entity.scaleY or (entity.flipV and -1) or 1
  }
end
function entity.rotation(room, entity)
  return math.rad(entity.rotation or 0) -- flip horizontally
end
function entity.depth(room,entity)
  return entity.depth or -100
end
function entity.flip(room, entity, horizontal, vertical)
  if vertical then
    entity.scaleY = -(entity.scaleY or 1)
  end
  if horizontal then
    entity.scaleX = -(entity.scaleX or 1)
  end
  return true
end

function entity.rotate(room, entity, direction)
  if direction ~= 0 then
      entity.rotation = ((entity.rotation or 0) + direction * 90) % 360
  end
  return direction ~= 0
end

return entity
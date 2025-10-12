local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ArbitraryDie"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

local rotations = {0,90,180,270}
entity.placements = {
  {
    name = "Arbitrary shape kill area",
    data = {
      safe=false,
      image = "decals/9-core/fossil_a",
      CustomColliderPath = "",
      flipH=false,
      flipV=false,
      color="fff",
      rotation=0,
      depth=-100,
      alphaCutoff=0.5,
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
function entity.scale(room, entity)
  return {entity.flipH and -1 or 1,entity.flipV and -1 or 1} -- flip horizontally
end
function entity.rotation(room, entity)
  return math.rad(entity.rotation or 0) -- flip horizontally
end
function entity.depth(room,entity)
  return entity.depth or -100
end
function entity.flip(room, entity, horizontal, vertical)
  if vertical then
    entity.flipV = not entity.flipV
  end
  if horizontal then
    entity.flipH = not entity.flipH
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
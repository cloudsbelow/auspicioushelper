local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/DecalRecolor"
entity.depth = -11000
entity.nodeLimits = {0,1}

local scopeSizes = {"wholeMap","wholeRoom","areaOnly"}

entity.placements = {
  {
    name = "Decal recolor controller",
    data = {
      width = 8,
      height = 8,
      scope = "wholeMap",
      texture = "",
      recolor = "",
    }
  }
}
entity.fieldInformation = {
  scope = {
    options = scopeSizes,
    editable = false,
  }
}
function entity.rectangle(room, entity)
  return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end
entity.fillColor = {1,0.6,0.2,0.2}
entity.borderColor = {0.7,0.4,0.1,0.7}

return entity
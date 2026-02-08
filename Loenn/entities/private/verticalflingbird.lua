local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local vflingbird = {}

vflingbird.name = "auspicioushelper/VerticalFlingBird"
vflingbird.depth = 2000
vflingbird.nodeLimits = {0,-1}
vflingbird.nodeLineRenderType = "line"

vflingbird.placements = {
  {
    name = "main",
    data = {
      --waiting=false
    }
  }
}

vflingbird.texture = "characters/bird/Hover04"
vflingbird.rotation = -math.pi/2

return vflingbird
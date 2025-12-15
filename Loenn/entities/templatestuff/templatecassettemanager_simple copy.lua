local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/TemplateCassetteColor"
entity.depth = -100000

entity.placements = {
  {
    name = "Template Cassette Colorer",
    data = {
      tintActive=true,
      simpleStyle=false,
      activeDepth=-10,
      channels="",
      fgSaturation = 0.5,
      color="#f80",

      inactiveDepth=8999,
      patternAngle=0,
      patternScale=4,
      patternOffset=0,
      patternWeight=0.5,
      borderColor="",
      innerColor="",
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/cassettelayer"

return entity
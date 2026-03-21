local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/TemplateCassetteManager"
entity.depth = -100000

entity.placements = {
  {
    name = "Template Cassette Manager",
    data = {
      materials = "",
      timings = "",
      onactivate = "",
      ondeactivate = "",
      channel = "",
      beatsPerMeasure = 4,
      beatsPerMinute = 90,
      offset = 0,
      useChannel = false,
      trysync = true,
      correct = true,
    }
  }
}

entity.texture = "/loenn/auspicioushelper/cassettemanager"

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/CustomDreamlayer"
entity.depth = 2000

entity.placements = {
  {
    name = "Dream visual settings",
    data = {
      color_0="#e46666",
      color_1="#d5f",
      color_2="#35c050",
      color_3="#04f",
      color_4="#ec0",
      color_5="#4df",
      depth = -12000,
      identifier = "ident",
      density = "1",
      contentAlpha = 0.15,
      edgeColor = "#fff",
      insideColor = "#000"
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/dreamlayer"

return entity
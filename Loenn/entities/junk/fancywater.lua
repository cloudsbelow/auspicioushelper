local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local xnaColors = require("consts.xna_colors")

local entity = {}

entity.name = "auspicioushelper/water"
entity.depth = -9999

entity.placements = {
  {
    name = "connected water",
    data = {
      width = 8,
      height=8,
      tempalteDrag=1,
    }
  }
}

entity.fillColor = {xnaColors.LightBlue[1] * 0.3, xnaColors.LightBlue[2] * 0.3, xnaColors.LightBlue[3] * 0.3, 0.5}
entity.borderColor = {xnaColors.LightBlue[1]*0.8,xnaColors.LightBlue[2] * 0.8, xnaColors.LightBlue[3] * 0.8, 0.8}

return entity
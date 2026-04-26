


local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}
local colors = {"danger/crystal>_blue", "danger/crystal>_red", "danger/crystal>_purple", "danger/crystal>_rainbow"}
entity.name = "auspicioushelper/spinner"
entity.depth = 2000

entity.placements = {
  {
    name = "main",
    data = {
      customColor = "ffffff",
      depth = -8500,
      makeFiller = true,
      numDebris = 4,
      dreamThru = false,
      neverClip = false,
      color = "danger/crystal>_rainbow",
      fancy = "",
      border = "000",
    }
  }
}
entity.fieldInformation = {
  color ={
    options = colors,
    editable=true
  },
}


entity.texture = "danger/crystal/fg_white00"

return entity



local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}
local colors = {"Blue", "Red", "Purple", "Rainbow"}
entity.name = "auspicioushelper/spinner"
entity.depth = 2000

entity.placements = {
  {
    name = "Recolorable spinner",
    data = {
      customColor = "ffffff",
      depth = -8500,
      makeFiller = true,
      numDebris = 4,
      dreamThru = false,
      neverClip = false,
      color = "Rainbow",
      fancy = "",
      border = "000",
    }
  }
}
entity.fieldInformation = {
  color ={
    options = colors,
    editable=false
  },
}


entity.texture = "danger/crystal/fg_white00"

return entity
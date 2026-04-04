


local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelApproacher"
entity.depth = 2000

entity.placements = {
  {
    name = "channel approach controller",
    data = {
      towardsChannel = "(1+x*3)",
      amount = "1",
      outChannel = "",
      useDt = true
    }
  }
}

entity.texture = "loenn/auspicioushelper/controllers/approachcontroller"

return entity
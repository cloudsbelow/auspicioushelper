local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelToFlag"
entity.depth = 2000

entity.placements = {
  {
    name = "Channel Flag Setter",
    data = {
      channel = "",
      flag = ""
    }
  }
}
entity.texture = "loenn/auspicioushelper/controllers/channelflag"

return entity
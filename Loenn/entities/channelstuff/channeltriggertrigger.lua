local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelTriggerTrigger"
entity.depth = 2000
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Activator (on channel)",
    data = {
      channel = "",
      onlyIfChannel = "",
      delay = -1,
      everyFrame = false,
    }
  }
}
entity.texture = "loenn/auspicioushelper/controllers/channelflag"

return entity
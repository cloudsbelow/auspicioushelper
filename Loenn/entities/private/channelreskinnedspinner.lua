local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelReskinnedSpinner"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Channel Reskinned Spinner",
    data = {
      mini=false
    }
  }
}
function entity.texture(room, entity)
  return entity.mini and "objects/auspicioushelper/channelcrystal/crystalminifg00" or "objects/auspicioushelper/channelcrystal/crystalfg00"
end

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local channelspritechain = {}

channelspritechain.name = "auspicioushelper/SpriteAnimChain"
channelspritechain.depth = 2000
channelspritechain.nodeLimits = {3,-1}
channelspritechain.nodeLineRenderType = "line"

channelspritechain.placements = {
  {
    name = "Channel VFX spritechain",
    data = {
      depth = 0,
      seconds_per_node=1.8,
      addfreq=0.5,
      stack_ends=false,
      tangent_freq=1.0,
      tangent_magnitude=16.,
      atlas_directory="particles/starfield/",
      loop=false
    }
  }
}
-- channelspritechain.fieldInformation = {
--   channel = {
--     depth="integer"
--   }
-- }

function channelspritechain.rectangle(room, entity)
  return utils.rectangle(entity.x-4, entity.y-4, 8, 8)
end

return channelspritechain
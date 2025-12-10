local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ConditionalStrawb"
entity.depth = 2000
entity.nodeLimits = {0,-1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Conditional Strawberry",
    data = {
      strawberry_id = "",
      appear_channel = "",
      appear_roomenter_only = true,

      fly_channel="",
      nodeSelectorChannel = "",

      deathless=false,
      flyOnDashNormal=false,
      flyOnDashFollower=false,
      persist_on_death=false,
      sprites="auspicioushelper_conditionalstrawb"
    }
  }
}

entity.texture = "objects/auspicioushelper/conditionalstrawb/silverwinged/wings00"

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end

return entity
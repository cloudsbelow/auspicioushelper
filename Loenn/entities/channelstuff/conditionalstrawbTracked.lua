local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ConditionalStrawbTracked"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

entity.placements = {
  {
    name = "Conditional Strawberry (Tracked)",
    data = {
      strawberry_id = "",
      appear_on_ch=false,
      appear_roomenter_only=true,
      appear_channel= "",
      appear_chvalue= 0,

      fly_on_ch = false,
      fly_channel="",
      fly_value=1,

      deathless=false,
      winged=false,
      wingedfollower=false,
      persist_on_death=false,
      sprites="auspicioushelper_conditionalstrawb"
    }
  }
}
entity.fieldInformation = {
  fly_value = {
    fieldType="integer"
  },
  appear_chvalue = {
    fieldType="integer"
  }
}
entity.texture = "objects/auspicioushelper/conditionalstrawb/silverwinged/wings00"

function entity.rectangle(room, entity)
  return utils.rectangle(entity.x-8, entity.y-8, 16, 16)
end

return entity
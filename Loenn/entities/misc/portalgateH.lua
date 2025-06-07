local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/PortalGateH"
entity.depth = 2000
entity.nodeLimits = {1,1}
entity.nodeLineRenderType = "line"
entity.nodeVisibility = "always"

entity.placements = {
  {
    name = "main",
    data = {
      height = 8,
      right_facing_f0=false,
      right_facing_f1=true,
      color_hex="ffffffaa",
      attached=false,
      giveRCB = true,
    }
  }
}
entity.fieldInformation = {
    color_hex = { fieldType = "color", useAlpha = true }
}

entity.texture = "loenn/auspicioushelper/portal"
function entity.scale(room, entity)
    return {entity.right_facing_f0 and 1 or -1,entity.height}    
end
entity.justification = {0,0}
function entity.color(room, entity)
    local parsed, r, g, b, a = utils.parseHexColor(entity.color_hex)
    return parsed and {r, g, b, a} or {1,1,1}
end

entity.nodeTexture = entity.texture
entity.nodeScale = function(room, entity)
    return {entity.right_facing_f1 and 1 or -1,entity.height}    
end
entity.nodeJustification = entity.justification 
entity.nodeColor = entity.color

return entity
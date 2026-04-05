local fakeTilesHelper = require("helpers.fake_tiles")
local drawableSprite = require("structs.drawable_sprite")

local entity = {}

entity.name = "auspicioushelper/ConnectedBlocks"
entity.depth = -10001

function entity.placements()
    return {
        name = "main",
        data = {
            tiletype = fakeTilesHelper.getPlacementMaterial(),
            width = 8,
            height = 8
        }
    }
end


local orig = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

entity.sprite = function(room, entity, node)
    if entity.tiletype == '\n' then
        local data = {}

        data.x = entity.x
        data.y = entity.y
        data.scaleX = entity.width
        data.scaleY = entity.height
        data.justificationX = 0
        data.justificationY = 0
        data.color = {0.2,0.6,0.4,0.6}
        return drawableSprite.fromInternalTexture("1x1-tinting-pixel", data)
    else orig(room,entity,node) end 
    return orig(room,entity,node)
end

--entity.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype", "tilesBg")
entity.fieldInformation = function(entity)
    local tiles = {["#Empty (connecting)"]="\n"}
    for k,v in pairs(fakeTilesHelper.getTilesOptions()) do
        tiles[k]=v
    end
    return {
        tiletype = {
            options = tiles,
            editable = false
        }
    }
end

return entity
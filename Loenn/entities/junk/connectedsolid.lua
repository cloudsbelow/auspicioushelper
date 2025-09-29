local fakeTilesHelper = require("helpers.fake_tiles")

local entity = {}

entity.name = "auspicioushelper/ConnectedBlocks"
entity.depth = 0

function entity.placements()
    return {
        name = "connected tiles (template)",
        data = {
            tiletype = fakeTilesHelper.getPlacementMaterial(),
            width = 8,
            height = 8
        }
    }
end

entity.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
entity.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

return entity
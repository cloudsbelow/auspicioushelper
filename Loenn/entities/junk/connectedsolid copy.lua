local fakeTilesHelper = require("helpers.fake_tiles")

local entity = {}

entity.name = "auspicioushelper/ConnectedBlocksBg"
entity.depth = 10000

function entity.placements()
    return {
        name = "main",
        data = {
            tiletype = fakeTilesHelper.getPlacementMaterial("3","tilesBg"),
            width = 8,
            height = 8
        }
    }
end

entity.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false, "tilesBg")
entity.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype", "tilesBg")

return entity
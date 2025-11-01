


local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/CampfireRespawn" 
entity.depth = 2000

entity.placements = {
  {
    name = "Campfire Respawn Controller",
    data = {
      duckTime=2,
      channel="",
      disableNormal=true,
    }
  }
}
entity.texture = "objects/campfire/dream04"
entity.color = {225/255, 186/255, 255/255}

return entity
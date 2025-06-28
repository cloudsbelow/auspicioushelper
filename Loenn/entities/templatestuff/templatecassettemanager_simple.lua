local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/TemplateCassetteManagerSimple"
entity.depth = -100000

entity.placements = {
  {
    name = "Template Cassette Manager (simple)",
    data = {
      channel_1="",
      channel_2="",
      channel_3="",
      channel_4="",
      simple_style = false,
      visual_only = false,
      tintActive = false,
      translucent = false,
    }
  }
}

entity.texture = "/loenn/auspicioushelper/cassettemanager_simple"

return entity
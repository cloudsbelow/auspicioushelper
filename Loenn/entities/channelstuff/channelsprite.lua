local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelSprite"
entity.depth = 2000
entity.nodeLimits = {0,1}
entity.nodeLineRenderType = "line"

edge_types = {"loop","clamp","hide"}
entity.placements = {
  {
    name = "Channel Sprite",
    data = {
      channel = "",
      attached = false,
      edge_type = "loop",
      xml_spritename = "auspicioushelper_example1",
      cases=3,
      depth=2,
      materialIdentifiers="",
    }
  }
}
entity.fieldInformation = {
  edge_type = {
    options = edge_types,
    editable=false
  },
  cases = {
    fieldType="integer"
  },
  depth = {
    fieldType="integer"
  }
}
entity.texture = "loenn/auspicioushelper/controllers/sprite"

return entity
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local entity = {}

entity.name = "auspicioushelper/ChannelMathTrigger"
entity.depth = 2000

local multiOptions = {"ReplacePrevious", "BlockIfActive", "AttachedMultiple", "DetatchedMultiple"}
local activOptions = {"onEnter","onLeave","onInside"}
entity.placements = {
  {
    name = "Channel Math Controller",
    data = {
      compiled_operations = "",
      run_immediately = false,
      multi_type = "Block",
      activation_cond = "onEnter",
      debug = false,
      run_when_awake=false,
    }
  }
}
entity.fieldInformation = {
    compiled_operations = {
        options = {"https://cloudsbelow.neocities.org/celestestuff/visualmathcompiler"}
    },
    multi_type = {
      options = multiOptions,
      editable = false,
    },
    activation_cond = {
      options = activOptions,
      editable = false,
    }
}

entity.texture = "loenn/auspicioushelper/controllers/math"

return entity
